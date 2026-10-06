using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float speed = 5f;
    public float gravity = -30f;
    public float jumpHeight = 3.5f;

    [Header("VR")]
    [Tooltip("Velocidad al caminar con el joystick en VR. Más baja que en PC: en visor, ir rápido marea.")]
    public float velocidadVR = 2.5f;
    [Tooltip("Grados que gira el jugador por cada toque del stick derecho (si el control tiene uno; si no, se gira con la cabeza).")]
    public float anguloGiroPorPaso = 45f;
    [Tooltip("Qué tanto hay que empujar el stick derecho para girar (0 a 1).")]
    public float umbralGiro = 0.6f;
    [Tooltip("Oscurece suavemente los bordes de la vista al moverse, caer o saltar (anti-mareo). Se ajusta en el componente VinetaConfort que aparece en el Player al jugar.")]
    public bool vinetaConfort = true;

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;

    private Transform cabezaVR;
    private Transform origenVR;
    private bool giroListo = true;

    // Pared INVISIBLE detrás de un punto (sin objeto físico): el jugador no puede retroceder
    // más allá de ella. Solo bloquea una franja de 'ancho' metros y hasta 'fondo' metros hacia atrás,
    // así nunca afecta pasillos lejanos. No afecta al robot ni a la línea del piso.
    private bool limiteActivo;
    private Vector3 limitePunto, limiteAdelante, limiteDerecha;
    private float limiteAncho, limiteFondo;

    /// <summary>Activa la pared invisible: 'punto' es donde está, 'adelante' hacia dónde SÍ se puede ir.</summary>
    public void ActivarLimiteTrasero(Vector3 punto, Vector3 adelante, float ancho = 8f, float fondo = 6f)
    {
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.0001f) return;
        limiteAdelante = adelante.normalized;
        limiteDerecha = Vector3.Cross(Vector3.up, limiteAdelante);
        limitePunto = punto;
        limiteAncho = ancho;
        limiteFondo = fondo;
        limiteActivo = true;
    }

    public void DesactivarLimiteTrasero() => limiteActivo = false;

    private void AplicarLimiteTrasero()
    {
        if (!limiteActivo || controller == null || !controller.enabled) return;
        Vector3 desfase = transform.position - limitePunto;
        float atras = Vector3.Dot(desfase, limiteAdelante);      // < 0 = pasó la pared hacia atrás
        float lado = Vector3.Dot(desfase, limiteDerecha);
        if (atras < 0f && atras > -limiteFondo && Mathf.Abs(lado) < limiteAncho * 0.5f)
        {
            controller.Move(limiteAdelante * (-atras));            // lo devolvemos justo a la pared
            AvisarQueNoHayVueltaAtras();
        }
    }

    [Tooltip("Lo que dice el robot si intentas volver atrás después del diálogo.")]
    public string textoRobotNoVolver = "¡Por ahí no hay salida! Sígueme por la línea.";
    private float proximoAvisoLimite;

    /// <summary>Aviso arriba ("NO HAY VUELTA ATRÁS") + el robot te llama. Como mucho uno cada 5 s.</summary>
    private void AvisarQueNoHayVueltaAtras()
    {
        if (Time.time < proximoAvisoLimite) return;
        proximoAvisoLimite = Time.time + 5f;

        AvisoSistema.Mostrar(AvisoSistema.Tipo.SinRetorno, "", 0f, 0f);
        if (NubeDialogoBot.Instancia != null) NubeDialogoBot.Instancia.Mostrar(textoRobotNoVolver, 3.5f);
    }

    public bool EsVR => cabezaVR != null;
    public Transform CabezaVR => cabezaVR;
    /// <summary>Movimiento de este frame: XZ = joystick, Y = caída/salto (lo usa VinetaConfort).</summary>
    public Vector3 VelocidadActual { get; private set; }

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>Lo llama RigVR al armar el jugador VR: 'cabeza' es la cámara y 'origen' su padre en el piso.</summary>
    public void ConfigurarVR(Transform cabeza, Transform origen)
    {
        cabezaVR = cabeza;
        origenVR = origen;

        if (vinetaConfort && cabeza != null && GetComponent<VinetaConfort>() == null)
            gameObject.AddComponent<VinetaConfort>().Configurar(this, cabeza);
    }

    void Update()
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        Vector3 move;
        if (EsVR)
        {
            CentrarCapsulaBajoCabeza();
            GirarPorPasos();

            // En VR "adelante" es hacia donde mira la cabeza, no hacia donde apunta el cuerpo.
            Vector2 stick = EntradaVR.Mover;
            Vector3 adelante = Vector3.ProjectOnPlane(cabezaVR.forward, Vector3.up).normalized;
            Vector3 derecha = Vector3.ProjectOnPlane(cabezaVR.right, Vector3.up).normalized;
            move = (derecha * stick.x + adelante * stick.y) * velocidadVR;

            // En VR el PlayerInput está apagado (RigVR), así que el salto se lee aquí.
            if (EntradaVR.SaltarPresionado()) Saltar();
        }
        else
        {
            move = (transform.right * moveInput.x + transform.forward * moveInput.y) * speed;
        }
        controller.Move(move * Time.deltaTime);

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        AplicarLimiteTrasero();

        VelocidadActual = new Vector3(move.x, velocity.y, move.z);
    }

    /// <summary>
    /// Si el jugador camina físicamente por su pieza, la cabeza se aleja de la cápsula.
    /// Movemos la cápsula (chocando con las paredes) hasta quedar bajo la cabeza y
    /// devolvemos el origen del tracking lo mismo, así la vista no salta.
    /// </summary>
    private void CentrarCapsulaBajoCabeza()
    {
        Vector3 desfase = cabezaVR.position - transform.position;
        desfase.y = 0f;
        if (desfase.sqrMagnitude < 0.0001f) return;

        Vector3 antes = transform.position;
        controller.Move(desfase);
        Vector3 recorrido = transform.position - antes;
        recorrido.y = 0f;
        origenVR.position -= recorrido;
    }

    private void GirarPorPasos()
    {
        float x = EntradaVR.Girar.x;

        if (giroListo && Mathf.Abs(x) >= umbralGiro)
        {
            // Giramos alrededor de la cabeza (no del centro de la cápsula) para que el
            // jugador sienta que gira sobre sí mismo.
            Vector3 cabezaAntes = cabezaVR.position;
            transform.Rotate(0f, Mathf.Sign(x) * anguloGiroPorPaso, 0f);
            origenVR.position += cabezaAntes - cabezaVR.position;
            giroListo = false;
        }
        else if (Mathf.Abs(x) < umbralGiro * 0.5f)
        {
            giroListo = true;
        }
    }

    /// <summary>
    /// Pone al jugador con los pies en 'pies' y anula la velocidad de caída (si no, al
    /// reaparecer seguiría cayendo rápido y podría atravesar el piso).
    /// </summary>
    public void TeletransportarPies(Vector3 pies)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        float desdePies = controller != null
            ? -(controller.center.y - controller.height * 0.5f) * transform.lossyScale.y
            : 1f;

        bool estabaActivo = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false; // si no, el CharacterController ignora el cambio de posición
        transform.position = pies + Vector3.up * (desdePies + 0.1f);
        if (controller != null) controller.enabled = estabaActivo;

        velocity = Vector3.zero;
    }

    /// <summary>
    /// Mueve al jugador a 'posicion' (la de su Transform) mirando hacia 'rotacionY' grados, y
    /// anula la velocidad de caída. Lo usa ControladorActo1 al terminar el diálogo.
    /// </summary>
    public void TeletransportarA(Vector3 posicion, float rotacionY)
    {
        if (controller == null) controller = GetComponent<CharacterController>();

        bool estabaActivo = controller != null && controller.enabled;
        if (controller != null) controller.enabled = false;
        transform.SetPositionAndRotation(posicion, Quaternion.Euler(0f, rotacionY, 0f));
        if (controller != null) controller.enabled = estabaActivo;

        velocity = Vector3.zero;
    }

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
        if (!enabled) return; // congelado (menú, diálogo...): el botón no debe dejar un salto pendiente
        Saltar();
    }

    private void Saltar()
    {
        if (controller.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            Debug.Log("> Impulso vertical generado en el Datacenter.");
        }
    }
}

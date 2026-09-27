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

    private CharacterController controller;
    private Vector2 moveInput;
    private Vector3 velocity;

    private Transform cabezaVR;
    private Transform origenVR;
    private bool giroListo = true;

    public bool EsVR => cabezaVR != null;
    public Transform CabezaVR => cabezaVR;

    void Start()
    {
        controller = GetComponent<CharacterController>();
    }

    /// <summary>Lo llama RigVR al armar el jugador VR: 'cabeza' es la cámara y 'origen' su padre en el piso.</summary>
    public void ConfigurarVR(Transform cabeza, Transform origen)
    {
        cabezaVR = cabeza;
        origenVR = origen;
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

    public void OnMove(InputValue value)
    {
        moveInput = value.Get<Vector2>();
    }

    public void OnJump(InputValue value)
    {
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

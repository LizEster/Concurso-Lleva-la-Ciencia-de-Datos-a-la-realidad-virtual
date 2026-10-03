using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Terminal interactuable (el modelo con animación que se repite, importado de Sketchfab).
/// El aviso "[E] Abrir" / "[E] Cerrar" permanece OCULTO y sin función hasta que el
/// jugador termina todo el diálogo del robot y elige "Preparada/o." (ver
/// ControladorActo1.DialogoTerminado). A partir de ahí queda siempre visible sobre la
/// terminal (no depende de la cercanía, sólo la interacción sí). La PRIMERA vez que se presiona
/// A en el control (o E) estando cerca: se reproduce la animación de la terminal (que deja de
/// repetirse sola) -> aparece una ADVERTENCIA (el piso se cae, hay tiempo limitado para
/// responder, todo gasta agua) con la opción "Acepto el desafío", que se elige con la
/// mirada + un botón -> se muestra "Abriendo puerta..." y se abre la puerta (en vertical, con
/// PuertaDataCenter). Las veces siguientes, A simplemente abre o cierra la puerta
/// directamente. Los textos del Inspector que digan "[E]" se muestran como "[A]".
/// </summary>
public class TerminalInteractiva : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    [Tooltip("El controlador del diálogo del robot (Acto 1). Mientras el jugador no termine TODO el diálogo y elija 'Preparada/o.', el aviso [E] Abrir se queda oculto y la tecla no hace nada. Si se deja vacío, la terminal funciona siempre (sin esperar diálogo).")]
    public ControladorActo1 controladorActo1;

    [Header("Detección de cercanía")]
    [Tooltip("Distancia máxima a la que el jugador tiene que estar de la terminal para poder interactuar (abrir/cerrar). El aviso [E] se ve siempre; esto sólo controla cuándo funciona la tecla.")]
    public float distanciaDeActivacion = 2.5f;

    [Header("Aviso flotante sobre la terminal")]
    [Tooltip("Punto donde aparece el aviso '[E] Abrir'. Si lo dejas vacío, se usa la posición de este mismo objeto más 'Altura Aviso'.")]
    public Transform puntoAviso;
    public float alturaAviso = 1.6f;
    [Tooltip("Texto del aviso cuando la puerta está cerrada.")]
    public string textoAvisoAbrir = "[A] Abrir";
    [Tooltip("Texto del aviso cuando la puerta ya está abierta.")]
    public string textoAvisoCerrar = "[A] Cerrar";
    [Tooltip("Tamaño de fuente en unidades de mundo (no píxeles): pruébalo y ajústalo a ojo.")]
    public float tamanoFuenteAviso = 4f;
    public Color colorAviso = new Color(0f, 1f, 1f, 1f);

    [Header("Animación de la terminal")]
    [Tooltip("El Animator del modelo de la terminal (el que trae la animación que hoy se repite sola).")]
    public Animator animatorTerminal;
    [Tooltip("Cuánto dura la animación de apertura, en segundos. Ajusta este número para que calce con el clip real (mira su duración en la pestaña Animation de Unity).")]
    public float duracionAnimacionApertura = 3f;
    [Range(0f, 1f)]
    [Tooltip("En qué punto del clip (0 a 1) se ve la terminal ABIERTA. Para encontrarlo: Window > Animation > Animation, arrastra el cabezal hasta que se vea abierta, y divide el número de frame por el total de frames (Duración del clip x FPS).")]
    public float normalizadoAbierta = 0f;
    [Range(0f, 1f)]
    [Tooltip("En qué punto del clip (0 a 1) se ve la terminal CERRADA. Mismo truco: frame actual / frames totales.")]
    public float normalizadoCerrada = 0.45f;

    [Header("Referencias del jugador")]
    [Tooltip("El PlayerMovement del jugador: se desactiva mientras se lee la advertencia, para que no se aleje caminando. Si se deja vacío se busca solo.")]
    public PlayerMovement movimientoJugador;

    [Header("Advertencia antes de abrir la puerta")]
    [TextArea(6, 14)]
    [Tooltip("Mensaje que muestra la terminal antes de abrir la puerta. Acepta rich text de TextMeshPro (<b>, <color>, <align>...).")]
    public string textoAdvertencia =
        "<align=center><b><color=#FF5555>ADVERTENCIA</color></b></align>\n\n" +
        "Al abrirse la puerta comienza el puente de datos:\n\n" +
        "- Cada baldosa tiene una pregunta y un <b>tiempo limitado</b> para responderla.\n" +
        "- Si el tiempo se acaba, <b>el piso se cae contigo encima</b>.\n" +
        "- Cada caída, cada error y cada respuesta de la IA <b>gasta agua real</b> del sistema.\n\n" +
        "<align=center>¿Aceptas el desafío con consciencia?</align>";
    [Tooltip("Texto de la opción para aceptar (se elige apuntándola con la mirada y apretando un botón).")]
    public string textoAceptar = "Acepto el desafío";
    [TextArea(1, 2)]
    public string textoAbriendoPuerta = "<align=center>Abriendo puerta...</align>";
    [Tooltip("Tamaño de letra del panel de advertencia (usa la misma fuente que el texto de la terminal del HUD).")]
    public float tamanoFuenteAdvertencia = 48f;
    [Tooltip("Segundos que se queda escrito 'Abriendo puerta...' antes de que la puerta empiece a moverse.")]
    public float esperaAntesDeAbrir = 1.2f;

    [Header("Puerta")]
    [Tooltip("La puerta que se abre en vertical al aceptar el desafío.")]
    public PuertaDataCenter puerta;

    [Header("Diálogos del robot guía")]
    [TextArea(1, 2)]
    [Tooltip("Lo que dice el robot (en su burbuja) la primera vez que el jugador se acerca a la terminal, antes de haberla usado.")]
    public string textoRobotAlAcercarse = "¡Abre la puerta para poder escapar!";
    [Tooltip("Segundos que se muestra el aviso del robot.")]
    public float duracionAvisoRobot = 4f;
    [Tooltip("Arrastra aquí el mismo AudioSource que usa el bot para su bip de diálogo (puede ser el mismo que en ControladorActo1).")]
    public AudioSource fuenteAudioBip;
    public AudioClip clipBip;

    private Transform jugador;
    private bool procesando = false;
    private bool terminalYaUsada = false;
    private bool puertaAbierta = false;
    private bool jugadorEnRango = false;
    private bool advertenciaAbierta = false;
    private bool avisoAcercarseDicho = false;

    private GameObject avisoGO;
    private TextMeshPro etiquetaAviso;

    void Start()
    {
        jugador = GameObject.FindGameObjectWithTag("Player")?.transform;

        // Si no arrastraste el ControladorActo1 a mano en el Inspector, lo buscamos solos
        // en la escena: así el bloqueo del diálogo funciona igual aunque se te olvide conectarlo.
        if (controladorActo1 == null)
        {
            controladorActo1 = FindFirstObjectByType<ControladorActo1>();
        }

        CrearAvisoFlotante();

        // La animación importada, reproducida hacia adelante, muestra la terminal
        // "abierta" al principio (t=0) y "cerrándose" hacia el final (t=1). Para que
        // al interactuar se vea como que la terminal se ENCIENDE/ABRE, la dejamos
        // pausada en el último frame (t=1, "cerrada") y luego la reproducimos hacia
        // atrás en SecuenciaApertura().
        if (animatorTerminal != null)
        {
            AnimatorStateInfo estadoActual = animatorTerminal.GetCurrentAnimatorStateInfo(0);
            animatorTerminal.Play(estadoActual.fullPathHash, 0, normalizadoCerrada);
            animatorTerminal.speed = 0f;
            animatorTerminal.Update(0f);
        }
    }

    void Update()
    {
        // Mientras se lee la advertencia, la terminal no hace nada más (se acepta con la mirada).
        if (advertenciaAbierta) return;

        if (!DialogoListo())
        {
            // Todavía no terminaste de hablar con el robot (o no elegiste "Preparada/o."):
            // el aviso [E] Abrir se mantiene oculto y la tecla no hace nada.
            if (avisoGO != null && avisoGO.activeSelf) avisoGO.SetActive(false);
            return;
        }

        if (avisoGO != null && !avisoGO.activeSelf) avisoGO.SetActive(true);

        if (procesando) return;

        ActualizarCercania();
        if (!jugadorEnRango) return;

        if (!EntradaVR.InteractuarPresionado()) return;
        if (PunteroMirada.FrameUltimaEleccion == Time.frameCount) return; // ese botón ya eligió una opción

        if (!terminalYaUsada)
        {
            // Primera vez: reproduce la animación y muestra la advertencia.
            StartCoroutine(SecuenciaApertura());
        }
        else if (!puertaAbierta)
        {
            // Ya se usó una vez: las siguientes veces sólo abre/cierra la puerta,
            // sin repetir la animación ni la advertencia.
            AbrirDirecto();
        }
        else
        {
            CerrarDirecto();
        }
    }

    /// <summary>Mismo truco que en ControladorActo1: pitch al azar para que suene "distorsionado".</summary>
    private void ReproducirBip()
    {
        if (fuenteAudioBip == null || clipBip == null) return;
        fuenteAudioBip.pitch = Random.Range(0.6f, 1.3f);
        fuenteAudioBip.PlayOneShot(clipBip);
    }

    private void AbrirDirecto()
    {
        if (puerta != null) puerta.AbrirPuerta();
        puertaAbierta = true;
        ActualizarTextoAviso();
    }

    private void CerrarDirecto()
    {
        if (puerta != null) puerta.CerrarPuerta();
        puertaAbierta = false;
        ActualizarTextoAviso();
    }

    private void ActualizarTextoAviso()
    {
        if (etiquetaAviso == null) return;
        etiquetaAviso.text = EntradaVR.TraducirTeclas(puertaAbierta ? textoAvisoCerrar : textoAvisoAbrir);
    }

    /// <summary>True cuando ya se puede mostrar/usar el aviso [E] Abrir: o no hay
    /// controlador de diálogo asignado, o el jugador ya terminó todo el diálogo y
    /// eligió "Preparada/o.".</summary>
    private bool DialogoListo()
    {
        return controladorActo1 == null || controladorActo1.DialogoTerminado;
    }

    private void ActualizarCercania()
    {
        if (jugador == null)
        {
            jugador = GameObject.FindGameObjectWithTag("Player")?.transform;
            if (jugador == null) return;
        }

        float distancia = Vector3.Distance(jugador.position, transform.position);
        bool estabaEnRangoAntes = jugadorEnRango;
        jugadorEnRango = distancia <= distanciaDeActivacion;
        // El aviso ya no se oculta según la distancia: se queda siempre visible
        // (ver CrearAvisoFlotante()). Esto sólo controla si la tecla E funciona.

        // La primera vez que el jugador entra en rango (y todavía no usó la terminal),
        // el robot avisa que hay que abrir la puerta para escapar.
        if (jugadorEnRango && !estabaEnRangoAntes && !terminalYaUsada && !avisoAcercarseDicho)
        {
            avisoAcercarseDicho = true;
            ReproducirBip();
            if (NubeDialogoBot.Instancia != null)
            {
                NubeDialogoBot.Instancia.Mostrar(textoRobotAlAcercarse, duracionAvisoRobot);
            }
        }
    }

    /// <summary>Texto 3D (no UI) flotando sobre la terminal, orientado siempre hacia la cámara.</summary>
    private void CrearAvisoFlotante()
    {
        // OJO: a propósito NO lo dejamos como hijo del objeto de la terminal. Los
        // modelos importados (sobre todo desde Sketchfab) suelen traer una escala muy
        // distinta a 1 en su Transform; si el texto fuera hijo, heredaría esa escala y
        // podría verse gigante, microscópico o directamente invisible. En vez de eso,
        // seguimos su posición a mano cada frame en ActualizarPosicionAviso().
        GameObject textoGO = new GameObject("AvisoAbrirTerminal");

        etiquetaAviso = textoGO.AddComponent<TextMeshPro>();
        etiquetaAviso.text = EntradaVR.TraducirTeclas(textoAvisoAbrir);
        etiquetaAviso.fontSize = tamanoFuenteAviso;
        etiquetaAviso.color = colorAviso;
        etiquetaAviso.alignment = TextAlignmentOptions.Center;

        avisoGO = textoGO;
        ActualizarPosicionAviso();
        avisoGO.SetActive(DialogoListo()); // oculto hasta terminar el diálogo del robot
    }

    private void ActualizarPosicionAviso()
    {
        if (avisoGO == null) return;

        avisoGO.transform.position = puntoAviso != null
            ? puntoAviso.position
            : transform.position + Vector3.up * alturaAviso;
    }

    /// <summary>
    /// Mantiene el aviso flotante mirando siempre hacia la cámara del jugador, igual que
    /// hace NubeDialogoBot con la burbuja del robot guía.
    /// </summary>
    void LateUpdate()
    {
        if (avisoGO == null || !avisoGO.activeSelf) return;

        ActualizarPosicionAviso();

        if (Camera.main == null) return;

        Vector3 haciaElAviso = avisoGO.transform.position - Camera.main.transform.position;
        haciaElAviso.y = 0f;

        if (haciaElAviso.sqrMagnitude > 0.0001f)
        {
            avisoGO.transform.rotation = Quaternion.LookRotation(haciaElAviso.normalized, Vector3.up);
        }
    }

    private IEnumerator SecuenciaApertura()
    {
        procesando = true;

        // IMPORTANTE: Animator.speed NO admite valores negativos en tiempo de juego
        // normal (Unity tira "Animator.speed can only be negative when Animator
        // recorder is enabled"), así que no se puede simplemente poner speed = -1
        // para reproducir en reversa. En vez de eso, "arrastramos" el tiempo de la
        // animación a mano, cuadro a cuadro, de 1 (cerrada) hacia 0 (abierta).
        if (animatorTerminal != null)
        {
            yield return ReproducirAnimacionEnReversa();
        }
        else
        {
            yield return new WaitForSeconds(duracionAnimacionApertura);
        }

        MostrarAdvertencia();
    }

    private IEnumerator ReproducirAnimacionEnReversa()
    {
        AnimatorStateInfo estado = animatorTerminal.GetCurrentAnimatorStateInfo(0);
        int hashEstado = estado.fullPathHash;

        float transcurrido = 0f;
        while (transcurrido < duracionAnimacionApertura)
        {
            transcurrido += Time.deltaTime;
            float t = Mathf.Lerp(normalizadoCerrada, normalizadoAbierta, transcurrido / duracionAnimacionApertura);
            animatorTerminal.Play(hashEstado, 0, t);
            animatorTerminal.Update(0f);
            yield return null;
        }

        // Nos aseguramos de terminar exactamente en la pose "abierta".
        animatorTerminal.Play(hashEstado, 0, normalizadoAbierta);
        animatorTerminal.Update(0f);
    }

    /// <summary>
    /// Muestra la advertencia en un panel flotante delante del jugador, con la opción
    /// "Acepto el desafío" que se elige con la mirada + un botón (PanelOpcionesMirada). Mientras
    /// tanto el jugador no puede caminar, pero sí mirar alrededor.
    /// </summary>
    private void MostrarAdvertencia()
    {
        if (movimientoJugador == null && jugador != null) movimientoJugador = jugador.GetComponent<PlayerMovement>();
        if (movimientoJugador != null) movimientoJugador.enabled = false;

        advertenciaAbierta = true;
        PanelOpcionesMirada.Mostrar(textoAdvertencia, new[] { textoAceptar }, FuenteTerminal(),
            tamanoFuenteAdvertencia, _ => AceptarDesafio());
    }

    private void AceptarDesafio()
    {
        if (!advertenciaAbierta) return;
        advertenciaAbierta = false;

        ReproducirBip();
        PanelOpcionesMirada.Mostrar(textoAbriendoPuerta, null, FuenteTerminal(), tamanoFuenteAdvertencia, null);
        StartCoroutine(SecuenciaFinalApertura());
    }

    /// <summary>La misma fuente que usa el texto de la terminal del HUD (UIManager).</summary>
    private TMP_FontAsset FuenteTerminal()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null &&
            GameManager.Instance.uiManager.textoTerminal != null)
        {
            return GameManager.Instance.uiManager.textoTerminal.font;
        }
        return null;
    }

    private IEnumerator SecuenciaFinalApertura()
    {
        yield return new WaitForSeconds(esperaAntesDeAbrir);

        PanelOpcionesMirada.Ocultar();
        if (movimientoJugador != null) movimientoJugador.enabled = true;

        if (puerta != null)
        {
            puerta.AbrirPuerta();
        }

        puertaAbierta = true;
        terminalYaUsada = true;
        procesando = false;
        ActualizarTextoAviso();
    }
}

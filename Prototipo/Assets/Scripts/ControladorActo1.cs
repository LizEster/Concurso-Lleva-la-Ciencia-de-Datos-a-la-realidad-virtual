using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
public class ControladorActo1 : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public UIManager uiManager;
    [Tooltip("Opcional: una puerta con PuertaDataCenter que quieras abrir al terminar el diálogo (además de la barrera).")]
    public PuertaDataCenter puertaInicio;
    [Tooltip("El robot guía: se pone rojo/enfermo al arrancar y se cura al terminar el diálogo.")]
    public BotGuia botGuia;
    [Tooltip("Objeto sólido (un Cube con Box Collider, sin 'Is Trigger') que bloquea el paso hacia el puente/túnel. Se desactiva al elegir 'Preparada/o.'.")]
    public GameObject barreraSalida;

    [Header("Texto inicial (pantalla negra / HUD)")]
    [TextArea(2, 3)]
    public string textoPantallaInicial = "Parece que te has perdido…";
    [Tooltip("Segundos que se muestra el texto inicial antes de que el robot empiece a hablar.")]
    public float duracionTextoInicial = 5f;

    [Header("Bip de atención")]
    [Tooltip("Arrastra aquí un AudioSource (puede vivir en el propio robot, o en cualquier objeto).")]
    public AudioSource fuenteAudioBip;
    public AudioClip clipBip;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad, por cada letra escrita, de que suene un bip extra de 'glitch' además del bip inicial de cada frase.")]
    public float probabilidadBipExtra = 0.04f;

    [Header("Opciones del jugador")]
    [Tooltip("Tamaño de letra de las opciones que se eligen mirándolas (usan la misma fuente que el texto de la terminal).")]
    public float tamanoFuenteOpciones = 52f;

    [Header("Después del diálogo: negro y reubicación")]
    [Tooltip("Dónde aparece el jugador cuando termina el diálogo y todo se pone negro. Son los MISMOS números que muestra el Inspector del Player (relativos a su padre, 'Nivel'). Si asignas 'Punto Tras Dialogo', se usa ese objeto en vez de estos números.")]
    public Vector3 posicionTrasDialogo = new Vector3(-0.18f, -12.48f, -46.39f);
    [Tooltip("Hacia dónde mira el jugador al aparecer (rotación Y en grados, como en el Inspector del Player).")]
    public float rotacionYTrasDialogo = -540.1f;
    [Tooltip("Opcional: un objeto vacío en la escena que marque dónde aparece el jugador (usa su posición y su rotación Y).")]
    public Transform puntoTrasDialogo;
    public float duracionFundidoANegro = 1f;
    public float segundosEnNegro = 0.6f;
    public float duracionFundidoDesdeNegro = 1.5f;

    [Header("Cierre del diálogo")]
    [TextArea(1, 2)]
    public string textoFinalTrasDialogo = "Sígueme...";

    // ------------------------------------------------------------------
    // GUION DEL ROBOT: cada nodo es una línea que dice el robot, mostrada
    // SOLO en su burbuja. Las opciones del jugador (las líneas "JUGADOR:")
    // NUNCA entran a la burbuja: aparecen aparte, en un panel flotante
    // (PanelOpcionesMirada) y se eligen con la mirada + un botón.
    // ------------------------------------------------------------------

    [System.Serializable]
    public class OpcionDialogo
    {
        public string textoOpcion;
        public int nodoSiguiente; // -1 = termina el diálogo
    }

    [System.Serializable]
    public class NodoDialogoRobot
    {
        [TextArea(2, 6)] public string textoRobot;
        public OpcionDialogo[] opciones;
    }

    public List<NodoDialogoRobot> guionRobot = new List<NodoDialogoRobot>()
    {
        new NodoDialogoRobot
        {
            textoRobot = "¡Al fin alguien aquí! No sé cómo entraste, pero has caído dentro de un servidor de procesamiento. Por favor ayúdame y te ayudaré a salir.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "¿Qué es este lugar?", nodoSiguiente = 1 },
                new OpcionDialogo { textoOpcion = "¿Qué tengo que hacer?", nodoSiguiente = 1 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Estoy enfermo y debilitado… Soy un agente de Large Language Model, el tipo de Inteligencia Artificial que lee miles de textos para responder como haría un humano. Mi trabajo es procesar datos, pero el paso hacia la salida está roto y mi sistema está por colapsar.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "¿Por qué estás enfermo?", nodoSiguiente = 2 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Me he quedado sin agua. Los servidores de Inteligencia Artificial viven de miles de litros de agua real para enfriarse y no sobrecalentarse. Como mis reservas están en las últimas, no puedo pensar. Necesito que tú repares el puente de salida asociando los conceptos manualmente.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "Entendido, ¿cómo cruzo?", nodoSiguiente = 3 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Piensa como una Inteligencia Artificial (IA): calcula la relación lógica entre palabras. Si una te cuesta mucho, puedes pedir \"Responder con IA\", pero ten cuidado. Delegarme la respuesta forzará el procesador y gastará gran parte de mis reservas de agua. Cada fallo también gasta agua, y si la reserva llega a 0%, colapsamos. Sé consciente de tus decisiones. ¿Preparada/o?",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "Preparada/o.", nodoSiguiente = -1 },
            }
        },
    };

    private int nodoActual = 0;
    private bool esperandoOpcion = false;
    private bool dialogoTerminado = false;
    private PlayerMovement movimientoJugador;

    /// <summary>
    /// True recién cuando el jugador terminó de leer TODO el diálogo del robot y eligió
    /// "Preparada/o.". Otros scripts (como TerminalInteractiva) lo consultan para no
    /// activarse antes de tiempo.
    /// </summary>
    public bool DialogoTerminado => dialogoTerminado;

    void Start()
    {
        // La barrera bloquea desde el minuto uno, hasta que se elija "Preparada/o.".
        if (barreraSalida != null) barreraSalida.SetActive(true);

        // El jugador no se puede mover desde el menú hasta terminar TODO el diálogo
        // (mirar alrededor sí, para apuntar a las opciones).
        movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        CongelarJugador(true);

        // El robot se queda quieto (esperando para hablar) hasta que termine el diálogo.
        if (botGuia != null) botGuia.enPausa = true;

        StartCoroutine(SecuenciaCompleta());
    }

    private IEnumerator SecuenciaCompleta()
    {
        // Por si NubeDialogoBot arranca mostrando su texto de ejemplo antes de que
        // nosotros tomemos el control del diálogo.
        if (NubeDialogoBot.Instancia != null)
        {
            NubeDialogoBot.Instancia.Ocultar();
        }

        // Primero el menú principal, y después su intro (negro + "Parece que te has
        // perdido…" + el juego apareciendo de a poco). Recién ahí habla el robot.
        // 'textoPantallaInicial' y 'duracionTextoInicial' los usa MenuPrincipal para esa intro.
        yield return null; // deja que MenuPrincipal alcance a crearse
        while (MenuPrincipal.Bloqueando) yield return null;

        // Por si algo lo soltó en el camino (el menú, otro script...).
        CongelarJugador(true);

        // El robot aparece de frente, delante de donde está mirando el jugador.
        if (botGuia != null)
        {
            botGuia.enPausa = true;
            botGuia.ColocarFrenteAlJugador();
        }

        MostrarNodo(0);
    }

    private void CongelarJugador(bool congelar)
    {
        if (movimientoJugador == null) movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        if (movimientoJugador != null) movimientoJugador.enabled = !congelar;
    }

    private void MostrarNodo(int indice)
    {
        nodoActual = indice;
        esperandoOpcion = false;

        PanelOpcionesMirada.Ocultar(); // limpia las opciones del nodo anterior

        StartCoroutine(EscribirEnNube(guionRobot[indice].textoRobot, AlTerminarDeEscribir));
    }

    /// <summary>
    /// Escribe el texto letra por letra directamente en la burbuja del robot (NubeDialogoBot
    /// no tiene animación propia). Suena un bip al empezar la frase, y ocasionalmente algún
    /// bip extra "glitcheado" en medio, como si al robot se le distorsionara la voz al hablar.
    /// </summary>
    private IEnumerator EscribirEnNube(string mensaje, System.Action alTerminar)
    {
        if (NubeDialogoBot.Instancia == null)
        {
            // Sin burbuja no hay dónde escribir, pero el diálogo no se puede quedar pegado.
            alTerminar?.Invoke();
            yield break;
        }

        ReproducirBip();
        yield return new WaitForSeconds(0.35f); // el bip suena antes de que empiece el texto

        string acumulado = "";
        foreach (char letra in mensaje)
        {
            acumulado += letra;
            NubeDialogoBot.Instancia.Mostrar(acumulado);

            if (Random.value < probabilidadBipExtra)
            {
                ReproducirBip();
            }

            yield return new WaitForSeconds(0.028f);
        }

        alTerminar?.Invoke();
    }

    private void ReproducirBip()
    {
        if (fuenteAudioBip == null || clipBip == null) return;
        // El pitch variable (más bajo/errático de lo normal) es lo que da la sensación
        // de que el sonido viene distorsionado, como un robot enfermo hablando mal.
        fuenteAudioBip.pitch = Random.Range(0.6f, 1.3f);
        fuenteAudioBip.PlayOneShot(clipBip);
    }

    private void AlTerminarDeEscribir()
    {
        OpcionDialogo[] opciones = guionRobot[nodoActual].opciones;

        if (opciones == null || opciones.Length == 0)
        {
            TerminarDialogo();
            return;
        }

        esperandoOpcion = true;

        // Las opciones del jugador se muestran APARTE, en un panel flotante delante de la
        // mirada (nunca dentro de la burbuja del robot), y se eligen con la mirada + un botón.
        string[] textos = new string[opciones.Length];
        for (int i = 0; i < opciones.Length; i++)
        {
            textos[i] = opciones[i].textoOpcion;
        }

        // Un poco más abajo de lo normal, para no tapar al robot ni su burbuja.
        TMP_FontAsset fuente = uiManager != null && uiManager.textoTerminal != null ? uiManager.textoTerminal.font : null;
        PanelOpcionesMirada.Mostrar(null, textos, fuente, tamanoFuenteOpciones, ElegirOpcion, 0.45f);
    }

    private void ElegirOpcion(int indice)
    {
        if (!esperandoOpcion || dialogoTerminado) return;
        esperandoOpcion = false;
        int siguiente = guionRobot[nodoActual].opciones[indice].nodoSiguiente;

        if (siguiente < 0 || siguiente >= guionRobot.Count)
        {
            TerminarDialogo();
        }
        else
        {
            MostrarNodo(siguiente);
        }
    }

    private void TerminarDialogo()
    {
        dialogoTerminado = true;

        PanelOpcionesMirada.Ocultar(); // ya no hay nada que elegir
        StartCoroutine(TransicionTrasDialogo());
    }

    /// <summary>
    /// Al elegir "Preparada/o.": todo se pone negro, el jugador y el robot aparecen en el
    /// punto de salida, vuelve la imagen, el robot dice "Sígueme..." y recién ahí el jugador
    /// se puede mover y el robot empieza a guiarlo.
    /// </summary>
    private IEnumerator TransicionTrasDialogo()
    {
        if (botGuia != null) botGuia.Curarse();
        if (NubeDialogoBot.Instancia != null) NubeDialogoBot.Instancia.Ocultar();

        yield return VeloNegro.Fundir(0f, 1f, duracionFundidoANegro);

        // --- Todo esto pasa en negro ---
        if (barreraSalida != null) barreraSalida.SetActive(false);
        if (puertaInicio != null) puertaInicio.AbrirPuerta();

        if (movimientoJugador == null) movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        if (movimientoJugador != null)
        {
            Vector3 destino;
            float rotacionY;
            if (puntoTrasDialogo != null)
            {
                destino = puntoTrasDialogo.position;
                rotacionY = puntoTrasDialogo.eulerAngles.y;
            }
            else
            {
                // Los números del Inspector son locales al padre del Player ('Nivel', que está
                // desplazado 24,66 m en Y): se pasan a coordenadas de mundo.
                Transform padre = movimientoJugador.transform.parent;
                destino = padre != null ? padre.TransformPoint(posicionTrasDialogo) : posicionTrasDialogo;
                rotacionY = rotacionYTrasDialogo + (padre != null ? padre.eulerAngles.y : 0f);
            }
            movimientoJugador.TeletransportarA(destino, rotacionY);
        }

        yield return null; // un frame para que la cámara (y la cabeza en el visor) ya estén en el lugar nuevo

        if (botGuia != null) botGuia.ColocarFrenteAlJugador();

        yield return new WaitForSeconds(segundosEnNegro);
        yield return VeloNegro.Fundir(1f, 0f, duracionFundidoDesdeNegro);

        // El robot dice su última frase (con bip, igual que las demás).
        yield return EscribirEnNube(textoFinalTrasDialogo, null);

        CongelarJugador(false);
        if (botGuia != null) botGuia.enPausa = false;

        // Recién ahora que el jugador se puede mover aparece la línea del piso.
        if (SenializacionRuta.Instancia != null) SenializacionRuta.Instancia.MostrarLinea(true);
    }
}

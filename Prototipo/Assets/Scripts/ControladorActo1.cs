using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
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

    [Header("Cierre del diálogo")]
    [TextArea(1, 2)]
    public string textoFinalTrasDialogo = "Sígueme...";

    [Header("Botón: Saltar diálogo")]
    [Tooltip("Si está marcado, aparece un botón en pantalla que salta todo el diálogo del robot de una sola vez.")]
    public bool mostrarBotonSaltar = true;
    [Tooltip("Texto que se muestra dentro del botón.")]
    public string textoBotonSaltar = "Saltar diálogo »";
    public Color colorFondoBotonSaltar = new Color(1f, 1f, 1f, 0.15f);
    public Color colorTextoBotonSaltar = Color.white;
    public int tamanoFuenteBotonSaltar = 22;

    // ------------------------------------------------------------------
    // GUION DEL ROBOT: cada nodo es una línea que dice el robot, mostrada
    // SOLO en su burbuja. Las opciones del jugador (las líneas "JUGADOR:")
    // NUNCA entran a la burbuja: se muestran aparte, en el HUD de pantalla.
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
    private bool introEnCurso = true;
    private bool dialogoTerminado = false;
    private GameObject botonSaltarGO;

    void Start()
    {
        // La barrera bloquea desde el minuto uno, hasta que se elija "Preparada/o.".
        if (barreraSalida != null) barreraSalida.SetActive(true);

        if (mostrarBotonSaltar)
        {
            CrearBotonSaltar();
        }

        // Mientras dura el diálogo el jugador no necesita mirar alrededor: liberamos el
        // cursor para que pueda hacer clic en "Saltar diálogo". Se vuelve a bloquear en
        // cuanto el diálogo termina, ya sea normal o saltado (ver LiberarSalida()).
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        StartCoroutine(SecuenciaCompleta());
    }

    /// <summary>
    /// Crea, colgado del mismo Canvas que usa UIManager, un botón que al presionarlo
    /// (una sola vez) se destruye y salta TODO el diálogo del robot de golpe.
    /// </summary>
    private void CrearBotonSaltar()
    {
        RectTransform lienzo = uiManager != null ? uiManager.canvasRectTransform : null;
        if (lienzo == null)
        {
            Debug.LogWarning("ControladorActo1: falta asignar 'Canvas Rect Transform' en el UIManager para poder crear el botón de saltar diálogo.");
            return;
        }

        GameObject botonGO = new GameObject("BotonSaltarDialogo");
        botonGO.transform.SetParent(lienzo, false);

        RectTransform rectBoton = botonGO.AddComponent<RectTransform>();
        rectBoton.anchorMin = new Vector2(1f, 1f);
        rectBoton.anchorMax = new Vector2(1f, 1f);
        rectBoton.pivot = new Vector2(1f, 1f);
        rectBoton.anchoredPosition = new Vector2(-24f, -24f);
        rectBoton.sizeDelta = new Vector2(240f, 56f);

        Image fondoBoton = botonGO.AddComponent<Image>();
        fondoBoton.color = colorFondoBotonSaltar;

        Button boton = botonGO.AddComponent<Button>();
        boton.targetGraphic = fondoBoton;
        boton.onClick.AddListener(SaltarDialogo);

        GameObject textoGO = new GameObject("Texto");
        textoGO.transform.SetParent(botonGO.transform, false);
        RectTransform rectTexto = textoGO.AddComponent<RectTransform>();
        rectTexto.anchorMin = Vector2.zero;
        rectTexto.anchorMax = Vector2.one;
        rectTexto.offsetMin = Vector2.zero;
        rectTexto.offsetMax = Vector2.zero;

        TextMeshProUGUI etiquetaBoton = textoGO.AddComponent<TextMeshProUGUI>();
        etiquetaBoton.text = textoBotonSaltar;
        etiquetaBoton.fontSize = tamanoFuenteBotonSaltar;
        etiquetaBoton.color = colorTextoBotonSaltar;
        etiquetaBoton.alignment = TextAlignmentOptions.Center;

        botonSaltarGO = botonGO;
    }

    /// <summary>
    /// Se llama al presionar el botón "Saltar diálogo". Corta cualquier corrutina de
    /// diálogo en curso (texto inicial, escritura letra por letra, etc.), limpia toda
    /// la UI del diálogo y deja el nivel exactamente como si el jugador hubiese
    /// terminado la conversación por las buenas.
    /// </summary>
    public void SaltarDialogo()
    {
        if (dialogoTerminado) return;

        StopAllCoroutines();

        introEnCurso = false;
        esperandoOpcion = false;
        dialogoTerminado = true;

        if (uiManager != null)
        {
            uiManager.MostrarOpciones("");
            uiManager.MostrarMensajeTerminal("");
        }

        if (NubeDialogoBot.Instancia != null)
        {
            NubeDialogoBot.Instancia.Ocultar();
        }

        if (botonSaltarGO != null)
        {
            Destroy(botonSaltarGO);
            botonSaltarGO = null;
        }

        LiberarSalida();
    }

    private IEnumerator SecuenciaCompleta()
    {
        // Por si NubeDialogoBot arranca mostrando su texto de ejemplo antes de que
        // nosotros tomemos el control del diálogo.
        if (NubeDialogoBot.Instancia != null)
        {
            NubeDialogoBot.Instancia.Ocultar();
        }

        // Texto inicial en el HUD normal, sin pedir ninguna tecla: aparece solo y se
        // borra solo después de 'duracionTextoInicial' segundos.
        if (uiManager != null)
        {
            uiManager.MostrarMensajeTerminal(textoPantallaInicial);
        }

        yield return new WaitForSeconds(duracionTextoInicial);

        if (uiManager != null)
        {
            uiManager.MostrarMensajeTerminal("");
        }

        introEnCurso = false;
        MostrarNodo(0);
    }

    void Update()
    {
        if (introEnCurso || dialogoTerminado || !esperandoOpcion) return;

        var teclado = UnityEngine.InputSystem.Keyboard.current;
        if (teclado == null) return;

        OpcionDialogo[] opciones = guionRobot[nodoActual].opciones;

        for (int i = 0; i < opciones.Length; i++)
        {
            var tecla = ObtenerTeclaNumero(i, teclado);
            if (tecla != null && tecla.wasPressedThisFrame)
            {
                ElegirOpcion(i);
                return;
            }
        }
    }

    private UnityEngine.InputSystem.Controls.KeyControl ObtenerTeclaNumero(int indice, UnityEngine.InputSystem.Keyboard teclado)
    {
        switch (indice)
        {
            case 0: return teclado.digit1Key;
            case 1: return teclado.digit2Key;
            case 2: return teclado.digit3Key;
            default: return null;
        }
    }

    private void MostrarNodo(int indice)
    {
        nodoActual = indice;
        esperandoOpcion = false;

        if (uiManager != null)
        {
            uiManager.MostrarOpciones(""); // limpia las opciones del nodo anterior
        }

        StartCoroutine(EscribirEnNube(guionRobot[indice].textoRobot, AlTerminarDeEscribir));
    }

    /// <summary>
    /// Escribe el texto letra por letra directamente en la burbuja del robot (NubeDialogoBot
    /// no tiene animación propia). Suena un bip al empezar la frase, y ocasionalmente algún
    /// bip extra "glitcheado" en medio, como si al robot se le distorsionara la voz al hablar.
    /// </summary>
    private IEnumerator EscribirEnNube(string mensaje, System.Action alTerminar)
    {
        if (NubeDialogoBot.Instancia == null) yield break;

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

        // Las opciones del jugador se muestran APARTE, en el HUD (uiManager), nunca
        // dentro de la burbuja del robot.
        string textoOpciones = "";
        for (int i = 0; i < opciones.Length; i++)
        {
            textoOpciones += $"[{i + 1}] {opciones[i].textoOpcion}\n";
        }

        if (uiManager != null)
        {
            uiManager.MostrarOpciones(textoOpciones);
        }
    }

    private void ElegirOpcion(int indice)
    {
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

        if (uiManager != null)
        {
            uiManager.MostrarOpciones(""); // ya no hay nada que elegir
        }

        if (botonSaltarGO != null)
        {
            Destroy(botonSaltarGO);
            botonSaltarGO = null;
        }

        // El robot dice su última frase (con bip, igual que las demás) y el jugador queda
        // libre para avanzar.
        StartCoroutine(EscribirEnNube(textoFinalTrasDialogo, null));

        LiberarSalida();
    }

    /// <summary>
    /// Acciones de "fin del diálogo": cura al robot, quita la barrera de salida, abre la
    /// puerta y vuelve a bloquear el cursor para que el jugador retome el control normal
    /// de la cámara (MouseLook). La usan tanto el final normal del diálogo como saltarlo.
    /// </summary>
    private void LiberarSalida()
    {
        if (botGuia != null)
        {
            botGuia.Curarse();
        }

        if (barreraSalida != null)
        {
            barreraSalida.SetActive(false);
        }

        if (puertaInicio != null)
        {
            puertaInicio.AbrirPuerta();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Componentes de Texto UI")]
    public TextMeshProUGUI textoMétricas; // Mantenemos tu nombre original con tilde
    public TextMeshProUGUI textoTerminal;

    [Header("Pantallas Finales (Acto III)")]
    public GameObject panelFinal;
    public TextMeshProUGUI textoFinalExplicativo; // Mantenemos tu nombre original

    [Header("Efecto Terminal Retro")]
    [Tooltip("Arrastra aquí el componente AudioSource que reproducirá el clic")]
    public AudioSource fuenteAudioTeclado;
    public float velocidadEscritura = 0.04f; // Tiempo en segundos entre cada letra

    [Header("Transición al final1 (Acto III éxito)")]
    [Tooltip("Arrastra aquí el RectTransform de tu Canvas principal. Si el panel final no existe todavía en la escena, se construye solo la primera vez, colgado de este Canvas.")]
    public RectTransform canvasRectTransform;
    [Tooltip("Nombre EXACTO de la escena a la que se transiciona al presionar 'Continuar'")]
    public string escenaSiguiente = "final1";
    public Color colorFondoPanel = new Color(0f, 0f, 0f, 0.85f);
    public Color colorTextoBoton = Color.white;
    public Color colorFondoBoton = new Color(1f, 1f, 1f, 0.15f);
    public int tamanoFuenteTexto = 24;
    public int tamanoFuenteBoton = 26;
    public string textoBotonContinuar = "Presiona [E] para continuar";

    private GameObject avisoContinuar;
    private bool esperandoTeclaE = false;
    private Coroutine corrutinaEscritura;

    void Update()
    {
        if (esperandoTeclaE && Input.GetKeyDown(KeyCode.E))
        {
            esperandoTeclaE = false;
            ContinuarHaciaSiguienteEscena();
        }
    }

    void Start()
    {
        // ACTO I: Al aparecer en la sala negra, las métricas del visor inician apagadas
        if (textoMétricas != null)
        {
            textoMétricas.gameObject.SetActive(false);
        }

        // Mensaje inicial del juego en la terminal con el nuevo efecto dinámico
        MostrarMensajeTerminal("Salón principal de Ciencia de Datos. Elige sobre qué deseas aprender.");
    }

    // Actualiza el indicador en tiempo real
    public void ActualizarMétricas(float refri, float agua)
    {
        if (textoMétricas != null)
        {
            textoMétricas.text = $"\nReservas de refrigeración: {refri:0}%\nAgua evaporada: {agua:0.2} L";
        }
    }

    // Activa el HUD visual cuando el jugador cae al datacenter (Acto II)
    public void ActivarVisorDataCenter()
    {
        if (textoMétricas != null)
        {
            textoMétricas.gameObject.SetActive(true);
        }
    }

    // NUEVO: Cambia el texto usando la animación letra por letra
    public void MostrarMensajeTerminal(string mensaje)
    {
        if (textoTerminal != null)
        {
            if (corrutinaEscritura != null)
            {
                StopCoroutine(corrutinaEscritura);
            }

            if (string.IsNullOrEmpty(mensaje))
            {
                textoTerminal.text = "";
            }
            else
            {
                corrutinaEscritura = StartCoroutine(EscribirTextoLetraPorLetra(mensaje));
            }
        }
    }

    // NUEVO: Corrutina que genera la animación y reproduce el sonido por cada carácter
    private IEnumerator EscribirTextoLetraPorLetra(string textoCompleto)
    {
        textoTerminal.text = "";

        // 1. ENCIENDE EL AUDIO AL INICIO: Como el sonido ya es un teclado escribiendo,
        // lo reproducimos de fondo UNA SOLA VEZ al arrancar la frase.
        if (fuenteAudioTeclado != null && fuenteAudioTeclado.clip != null)
        {
            fuenteAudioTeclado.volume = 0.25f; // Lo dejamos sutil y de fondo
            fuenteAudioTeclado.Play();
        }

        foreach (char letra in textoCompleto.ToCharArray())
        {
            textoTerminal.text += letra;
            yield return new WaitForSeconds(velocidadEscritura);
        }

        // 2. APAGA EL AUDIO AL TERMINAR: En cuanto se termina de escribir la última letra,
        // silenciamos el teclado ambiental para dar paso al silencio dramático.
        if (fuenteAudioTeclado != null)
        {
            fuenteAudioTeclado.Stop();
        }
    }

    // Despliega las conclusiones del Acto III según el rendimiento térmico
    public void MostrarPantallaFinal(bool exito, float refriRestante, float aguaTotal)
    {
        if (panelFinal == null)
        {
            ConstruirPantallaFinal();
        }

        if (panelFinal != null)
        {
            panelFinal.SetActive(true);
        }

        if (textoFinalExplicativo == null) return;

        if (exito)
        {
            textoFinalExplicativo.text = $"> OUTPUT GENERADO CON ÉXITO.\n" +
                $"Estado de refrigeración restante: {refriRestante:0}%\n\n" +
                $"Incluso siendo eficiente y usando tu propia deducción la mayor parte del tiempo, tu consulta evaporó {aguaTotal:0.2} litros de agua real. " +
                $"Mantener esta red viva tiene un costo físico inevitable.\n\n" +
                $"¿Sabías que entrenar grandes modelos de lenguaje en la vida real evapora cientos de miles de litros de agua?\n\n" +
                $"La Ciencia de Datos es una herramienta poderosa, pero cada vez que presionas 'Enter', el planeta paga una parte del precio.\n\n" +
                $"Úsala con conciencia.";
        }
        else
        {
            textoFinalExplicativo.text = $"<color=red>> ERROR 508: Límite de recursos excedido. Colapso térmico inminente.</color>\n\n" +
                $"Ya sea delegando todas las decisiones a la IA mediante el cálculo automático, o forzando al sistema a procesar errores continuos por falta de análisis, la sed del algoritmo secó las reservas por completo.\n\n" +
                $"Cada cálculo, cada error y cada prompt dentro de un LLM consume agua real de nuestro planeta para evitar que los componentes ardan.\n\n" +
                $"Entender cómo funciona esta tecnología no es solo técnica, es supervivencia.\n\n" +
                $"La próxima vez, calcula mejor tu huella.";
        }

        // El aviso de "Presiona [E] para continuar" (que hace el flash blanco y carga la
        // siguiente escena) solo tiene sentido en el final bueno.
        if (avisoContinuar != null)
        {
            avisoContinuar.SetActive(exito);
        }
        esperandoTeclaE = exito;
    }

    // Crea en tiempo real el panel del final (fondo + texto + botón "Continuar"),
    // colgado del Canvas principal. Solo se ejecuta si nadie lo dejó armado a mano
    // en el Inspector (panelFinal seguía sin asignar).
    private void ConstruirPantallaFinal()
    {
        if (canvasRectTransform == null)
        {
            Debug.LogWarning("UIManager: falta asignar 'canvasRectTransform' para poder construir la pantalla final. Arrastra tu Canvas en el Inspector.");
            return;
        }

        // --- Panel de fondo ---
        GameObject panelGO = new GameObject("PanelFinal");
        panelGO.transform.SetParent(canvasRectTransform, false);
        RectTransform panelRT = panelGO.AddComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        Image panelImg = panelGO.AddComponent<Image>();
        panelImg.color = colorFondoPanel;
        panelFinal = panelGO;
        panelFinal.SetActive(false);

        // --- Texto explicativo ---
        GameObject textoGO = new GameObject("TextoFinalExplicativo");
        textoGO.transform.SetParent(panelRT, false);
        RectTransform textoRT = textoGO.AddComponent<RectTransform>();
        textoRT.anchorMin = new Vector2(0.1f, 0.2f);
        textoRT.anchorMax = new Vector2(0.9f, 0.85f);
        textoRT.offsetMin = Vector2.zero;
        textoRT.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = textoGO.AddComponent<TextMeshProUGUI>();
        txt.fontSize = tamanoFuenteTexto;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.enableWordWrapping = true;
        textoFinalExplicativo = txt;

        // --- Aviso "Presiona [E] para continuar" (ya no es un botón clicable) ---
        GameObject avisoGO = new GameObject("AvisoContinuar");
        avisoGO.transform.SetParent(panelRT, false);
        RectTransform avisoRT = avisoGO.AddComponent<RectTransform>();
        avisoRT.anchorMin = new Vector2(0.5f, 0.06f);
        avisoRT.anchorMax = new Vector2(0.5f, 0.06f);
        avisoRT.pivot = new Vector2(0.5f, 0.5f);
        avisoRT.sizeDelta = new Vector2(320, 60);
        Image avisoImg = avisoGO.AddComponent<Image>();
        avisoImg.color = colorFondoBoton;
        avisoContinuar = avisoGO;

        GameObject avisoTextoGO = new GameObject("Texto");
        avisoTextoGO.transform.SetParent(avisoRT, false);
        RectTransform avisoTextoRT = avisoTextoGO.AddComponent<RectTransform>();
        avisoTextoRT.anchorMin = Vector2.zero;
        avisoTextoRT.anchorMax = Vector2.one;
        avisoTextoRT.offsetMin = Vector2.zero;
        avisoTextoRT.offsetMax = Vector2.zero;
        TextMeshProUGUI avisoTxt = avisoTextoGO.AddComponent<TextMeshProUGUI>();
        avisoTxt.text = textoBotonContinuar;
        avisoTxt.fontSize = tamanoFuenteBoton;
        avisoTxt.color = colorTextoBoton;
        avisoTxt.alignment = TextAlignmentOptions.Center;

        avisoContinuar.SetActive(false); // se activa solo en el final exitoso
    }

    // Se llama al presionar "Continuar" en el final exitoso: hace el flash blanco
    // y carga la escena siguiente (por defecto, "final1").
    public void ContinuarHaciaSiguienteEscena()
    {
        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.TransicionarAEscena(escenaSiguiente);
        }
        else
        {
            Debug.LogWarning("UIManager: no se encontró ScreenFader.Instance, cargando la escena directo sin transición.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(escenaSiguiente);
        }
    }
}

using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú principal futurista que aparece al entrar al nivel, con el mundo tapado de negro:
///   JUGAR    -> pantalla "Cómo jugar" (con VOLVER al inicio) -> COMENZAR -> negro + "Parece que te has perdido…"
///               -> el juego aparece de a poco -> empieza el diálogo del robot.
///   CRÉDITOS -> nombres del equipo -> VOLVER.
/// Todo se elige apuntando con la mirada y apretando A, B, X o Y (PunteroMirada).
/// Se crea solo en la escena que tiene ControladorActo1: no hay que ponerlo en la escena.
/// Mientras está activo, 'Bloqueando' es true y ControladorActo1 espera.
/// </summary>
public class MenuPrincipal : MonoBehaviour
{
    // ----------------------- TEXTOS (edítalos aquí) -----------------------
    private const string Titulo = "SED ALGORÍTMICA";
    private const string Subtitulo = "Lleva la Ciencia de Datos a la Realidad Virtual";
    private static readonly string[] NombresCreditos = { "Liz", "Mora", "Caro", "Cris" };
    private const string TextoInstrucciones =
        "Apunta con la <color=#33E6FF>mirada</color> a lo que quieras elegir\n" +
        "y aprieta <color=#33E6FF>cualquier botón</color> del control\n<b>(X, A, B o Y)</b>.\n\n" +
        "Camina con el <color=#33E6FF>joystick</color>.";
    private const string TextoIntroRespaldo = "Parece que te has perdido…";
    // ----------------------------------------------------------------------

    /// <summary>True mientras el menú o la intro tapan el juego (ControladorActo1 espera).</summary>
    public static bool Bloqueando { get; private set; }

    private const float DistanciaMenu = 2f;
    private const float MetrosPorPx = 0.0015f;
    private const float AnguloParaRecentrar = 65f;

    private static readonly Color Cian = new Color(0.2f, 0.9f, 1f, 1f);
    private static readonly Color CianSuave = new Color(0.2f, 0.9f, 1f, 0.35f);
    private static readonly Color FondoPanel = new Color(0.01f, 0.03f, 0.06f, 0.94f);
    private static readonly Color BotonNormal = new Color(0.02f, 0.14f, 0.2f, 0.95f);
    private static readonly Color BotonMirada = new Color(0.1f, 0.65f, 0.85f, 1f);
    private static readonly Color TextoBotonMirada = new Color(0.02f, 0.05f, 0.08f, 1f);

    private TMP_FontAsset fuente;
    private Material materialUI;
    private Material materialTexto;
    private PlayerMovement movimiento;

    private Transform velo;
    private Image imagenVelo;
    private RectTransform menu;
    private GameObject pantallaInicio, pantallaCreditos, pantallaInstrucciones;
    private TextMeshProUGUI titulo;
    private RectTransform lineaEscaneo;

    private RectTransform canvasIntro;
    private CanvasGroup grupoIntro;
    private bool comenzando;

    // ------------------------------------------------------------------
    // CREACIÓN AUTOMÁTICA
    // ------------------------------------------------------------------

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AlIniciar()
    {
        SceneManager.sceneLoaded += (escena, modo) => Crear();
        Crear();
    }

    private static void Crear()
    {
        if (UnityEngine.Object.FindAnyObjectByType<ControladorActo1>() == null)
        {
            Bloqueando = false;
            return;
        }

        Bloqueando = true;
        new GameObject("[MenuPrincipal]").AddComponent<MenuPrincipal>();
    }

    void Start()
    {
        fuente = FuenteDelJuego();

        // Todo el menú se dibuja encima del mundo (aunque haya paredes delante).
        materialUI = new Material(Canvas.GetDefaultCanvasMaterial());
        materialUI.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        materialTexto = new Material(fuente.material);
        materialTexto.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);

        movimiento = UnityEngine.Object.FindAnyObjectByType<PlayerMovement>();
        if (movimiento != null) movimiento.enabled = false;

        CrearVelo();
        CrearMenu();
        MostrarPantalla(pantallaInicio);
        ColocarMenu();
    }

    void OnEnable() { Application.onBeforeRender += SeguirCabeza; }
    void OnDisable() { Application.onBeforeRender -= SeguirCabeza; }

    void Update()
    {
        if (menu != null && menu.gameObject.activeSelf)
        {
            // Título que "respira" y línea de escaneo que baja por el panel.
            if (titulo != null)
            {
                Color c = titulo.color;
                c.a = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f);
                titulo.color = c;
            }
            if (lineaEscaneo != null)
            {
                float alto = menu.sizeDelta.y;
                float y = Mathf.Repeat(Time.unscaledTime * 160f, alto);
                lineaEscaneo.anchoredPosition = new Vector2(0f, alto * 0.5f - y);
            }

            // Si te das vuelta, el menú vuelve a ponerse delante.
            Transform cabeza = PunteroMirada.Cabeza();
            if (cabeza != null)
            {
                Vector3 haciaMenu = Vector3.ProjectOnPlane(menu.position - cabeza.position, Vector3.up);
                Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
                if (adelante.sqrMagnitude > 0.001f && Vector3.Angle(haciaMenu, adelante) > AnguloParaRecentrar) ColocarMenu();
            }
        }
    }

    void LateUpdate()
    {
        SeguirCabeza();
    }

    /// <summary>El velo negro (y el texto de la intro) van pegados a los ojos.</summary>
    private void SeguirCabeza()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;

        if (velo != null) velo.SetPositionAndRotation(cabeza.position + cabeza.forward * 0.5f, cabeza.rotation);
        if (canvasIntro != null) canvasIntro.SetPositionAndRotation(cabeza.position + cabeza.forward * 1.6f, cabeza.rotation);
    }

    // ------------------------------------------------------------------
    // NAVEGACIÓN
    // ------------------------------------------------------------------

    private void MostrarPantalla(GameObject pantalla)
    {
        pantallaInicio.SetActive(pantalla == pantallaInicio);
        pantallaCreditos.SetActive(pantalla == pantallaCreditos);
        pantallaInstrucciones.SetActive(pantalla == pantallaInstrucciones);
    }

    private void AlJugar() => MostrarPantalla(pantallaInstrucciones);
    private void AlCreditos() => MostrarPantalla(pantallaCreditos);
    private void AlVolver() => MostrarPantalla(pantallaInicio);

    private void AlComenzar()
    {
        if (comenzando) return;
        comenzando = true;
        StartCoroutine(Intro());
    }

    /// <summary>Negro -> "Parece que te has perdido…" -> el juego aparece de a poco.</summary>
    private IEnumerator Intro()
    {
        menu.gameObject.SetActive(false);

        ControladorActo1 acto1 = UnityEngine.Object.FindAnyObjectByType<ControladorActo1>();
        string texto = acto1 != null && !string.IsNullOrEmpty(acto1.textoPantallaInicial) ? acto1.textoPantallaInicial : TextoIntroRespaldo;
        float duracionTexto = acto1 != null ? Mathf.Max(2f, acto1.duracionTextoInicial) : 5f;

        CrearTextoIntro(texto);

        yield return new WaitForSeconds(0.6f);                 // un momento en negro total
        yield return Fundir(t => grupoIntro.alpha = t, 1.2f);  // aparece el texto
        yield return new WaitForSeconds(duracionTexto - 1.2f);

        // El juego aparece de a poco mientras el texto se va.
        const float duracionAparicion = 3f;
        float tiempo = 0f;
        while (tiempo < duracionAparicion)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.Clamp01(tiempo / duracionAparicion);
            imagenVelo.color = new Color(0f, 0f, 0f, 1f - Mathf.SmoothStep(0f, 1f, t));
            grupoIntro.alpha = 1f - Mathf.Clamp01(t * 2f);
            yield return null;
        }

        // El jugador sigue quieto: ControladorActo1 lo suelta recién al terminar el diálogo.
        Bloqueando = false;
        Destroy(gameObject);
    }

    private static IEnumerator Fundir(Action<float> aplicar, float duracion)
    {
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            aplicar(Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }
        aplicar(1f);
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN DE LA UI
    // ------------------------------------------------------------------

    private void CrearVelo()
    {
        RectTransform rt = CrearCanvas("VeloNegro", 1400, new Vector2(100f, 100f), 0.1f); // 10 m de ancho a 50 cm: tapa toda la vista
        rt.SetParent(transform, false);
        imagenVelo = CrearImagen(rt, "Negro", Vector2.zero, Vector2.zero, Color.black);
        Estirar(imagenVelo.rectTransform);
        velo = rt;
    }

    private void CrearMenu()
    {
        menu = CrearCanvas("Menu", 1500, new Vector2(1000f, 720f), MetrosPorPx);
        menu.SetParent(transform, false);

        // Panel con borde cian y esquinas marcadas.
        Image fondo = CrearImagen(menu, "Fondo", Vector2.zero, Vector2.zero, FondoPanel);
        Estirar(fondo.rectTransform);
        CrearBorde(menu, 1000f, 720f, 3f, CianSuave);
        CrearEsquinas(menu, 1000f, 720f, 70f, 8f, Cian);

        // Línea de escaneo que recorre el panel.
        lineaEscaneo = CrearImagen(menu, "Escaneo", Vector2.zero, new Vector2(1000f, 4f), new Color(0.2f, 0.9f, 1f, 0.12f)).rectTransform;

        // --- Inicio ---
        pantallaInicio = CrearPantalla("Inicio");
        RectTransform inicio = (RectTransform)pantallaInicio.transform;
        titulo = CrearTexto(inicio, Titulo, new Vector2(0f, 220f), new Vector2(940f, 130f), 96f, Cian, FontStyles.Bold);
        titulo.characterSpacing = 6f;
        CrearTexto(inicio, Subtitulo, new Vector2(0f, 135f), new Vector2(900f, 50f), 30f, new Color(1f, 1f, 1f, 0.7f), FontStyles.Normal);
        CrearImagen(inicio, "Separador", new Vector2(0f, 95f), new Vector2(620f, 3f), CianSuave);
        CrearBoton(inicio, "JUGAR", new Vector2(0f, 0f), AlJugar);
        CrearBoton(inicio, "CRÉDITOS", new Vector2(0f, -140f), AlCreditos);
        CrearTexto(inicio, "Apunta con la mirada y aprieta X, A, B o Y", new Vector2(0f, -290f), new Vector2(900f, 40f), 26f, new Color(0.2f, 0.9f, 1f, 0.6f), FontStyles.Italic);

        // --- Créditos ---
        pantallaCreditos = CrearPantalla("Creditos");
        RectTransform creditos = (RectTransform)pantallaCreditos.transform;
        CrearTexto(creditos, "CRÉDITOS", new Vector2(0f, 250f), new Vector2(900f, 100f), 72f, Cian, FontStyles.Bold).characterSpacing = 6f;
        CrearTexto(creditos, "Desarrollado por", new Vector2(0f, 170f), new Vector2(900f, 50f), 30f, new Color(1f, 1f, 1f, 0.6f), FontStyles.Normal);
        CrearTexto(creditos, string.Join("\n", NombresCreditos), new Vector2(0f, 20f), new Vector2(900f, 260f), 52f, Color.white, FontStyles.Normal);
        CrearBoton(creditos, "VOLVER", new Vector2(0f, -250f), AlVolver);

        // --- Cómo jugar ---
        pantallaInstrucciones = CrearPantalla("Instrucciones");
        RectTransform instrucciones = (RectTransform)pantallaInstrucciones.transform;
        CrearTexto(instrucciones, "CÓMO JUGAR", new Vector2(0f, 250f), new Vector2(900f, 100f), 72f, Cian, FontStyles.Bold).characterSpacing = 6f;
        CrearTexto(instrucciones, TextoInstrucciones, new Vector2(0f, 40f), new Vector2(880f, 300f), 38f, Color.white, FontStyles.Normal);
        CrearBoton(instrucciones, "COMENZAR", new Vector2(0f, -170f), AlComenzar);
        CrearBoton(instrucciones, "VOLVER", new Vector2(0f, -290f), AlVolver);
    }

    private void CrearTextoIntro(string texto)
    {
        canvasIntro = CrearCanvas("TextoIntro", 1500, new Vector2(1200f, 300f), MetrosPorPx);
        canvasIntro.SetParent(transform, false);
        grupoIntro = canvasIntro.gameObject.AddComponent<CanvasGroup>();
        grupoIntro.alpha = 0f;
        TextMeshProUGUI txt = CrearTexto(canvasIntro, texto, Vector2.zero, new Vector2(1150f, 280f), 64f, Color.white, FontStyles.Normal);
        txt.enableWordWrapping = true;
        SeguirCabeza();
    }

    private void ColocarMenu()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null || menu == null) return;

        Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
        if (adelante.sqrMagnitude < 0.0001f) adelante = cabeza.up;
        adelante.Normalize();

        menu.SetPositionAndRotation(cabeza.position + adelante * DistanciaMenu, Quaternion.LookRotation(adelante, Vector3.up));
    }

    // ------------------------------------------------------------------
    // AYUDANTES
    // ------------------------------------------------------------------

    private static TMP_FontAsset FuenteDelJuego()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null &&
            GameManager.Instance.uiManager.textoTerminal != null && GameManager.Instance.uiManager.textoTerminal.font != null)
        {
            return GameManager.Instance.uiManager.textoTerminal.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private static RectTransform CrearCanvas(string nombre, int orden, Vector2 tamano, float escala)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = orden;

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = tamano;
        rt.localScale = Vector3.one * escala;
        return rt;
    }

    private GameObject CrearPantalla(string nombre)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(menu, false);
        Estirar((RectTransform)go.transform);
        return go;
    }

    private Image CrearImagen(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image img = go.AddComponent<Image>();
        img.material = materialUI;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TextMeshProUGUI CrearTexto(RectTransform padre, string texto, Vector2 posicion, Vector2 tamano, float tamanoFuente, Color color, FontStyles estilo)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.font = fuente;
        txt.fontSharedMaterial = materialTexto;
        txt.text = texto;
        txt.fontSize = tamanoFuente;
        txt.color = color;
        txt.fontStyle = estilo;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;
        return txt;
    }

    private void CrearBoton(RectTransform padre, string texto, Vector2 posicion, Action alElegir)
    {
        Image fondo = CrearImagen(padre, "Boton " + texto, posicion, new Vector2(520f, 104f), BotonNormal);
        RectTransform rt = fondo.rectTransform;
        CrearBorde(rt, 520f, 104f, 3f, Cian);

        TextMeshProUGUI txt = CrearTexto(rt, texto, Vector2.zero, new Vector2(500f, 100f), 48f, Cian, FontStyles.Bold);
        txt.characterSpacing = 8f;

        OpcionMirable mirable = fondo.gameObject.AddComponent<OpcionMirable>();
        mirable.Configurar(fondo, BotonNormal, BotonMirada, txt, TextoBotonMirada, alElegir);
    }

    /// <summary>Marco de 4 líneas finas alrededor de un rectángulo centrado de 'ancho' x 'alto'.</summary>
    private void CrearBorde(RectTransform padre, float ancho, float alto, float grosor, Color color)
    {
        CrearImagen(padre, "BordeArriba", new Vector2(0f, alto * 0.5f), new Vector2(ancho, grosor), color);
        CrearImagen(padre, "BordeAbajo", new Vector2(0f, -alto * 0.5f), new Vector2(ancho, grosor), color);
        CrearImagen(padre, "BordeIzq", new Vector2(-ancho * 0.5f, 0f), new Vector2(grosor, alto), color);
        CrearImagen(padre, "BordeDer", new Vector2(ancho * 0.5f, 0f), new Vector2(grosor, alto), color);
    }

    /// <summary>Esquinas en forma de L, gruesas, para el look "visor futurista".</summary>
    private void CrearEsquinas(RectTransform padre, float ancho, float alto, float largo, float grosor, Color color)
    {
        for (int sx = -1; sx <= 1; sx += 2)
        {
            for (int sy = -1; sy <= 1; sy += 2)
            {
                Vector2 esquina = new Vector2(sx * ancho * 0.5f, sy * alto * 0.5f);
                CrearImagen(padre, "EsquinaH", esquina + new Vector2(-sx * largo * 0.5f, 0f), new Vector2(largo, grosor), color);
                CrearImagen(padre, "EsquinaV", esquina + new Vector2(0f, -sy * largo * 0.5f), new Vector2(grosor, largo), color);
            }
        }
    }

    private static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}

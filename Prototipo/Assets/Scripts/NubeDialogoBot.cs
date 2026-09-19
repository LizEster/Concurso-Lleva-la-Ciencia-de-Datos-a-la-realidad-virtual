using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Nube de diálogo del bot guía. Se construye sola sobre su cabeza, se reorienta en cada
/// frame para mirar al jugador y crece o encoge según el texto que se le ponga.
///
/// Para escribir los diálogos desde otro script:
///     NubeDialogoBot.Instancia.Mostrar("Sigue la línea hasta el siguiente nodo.");
///     NubeDialogoBot.Instancia.Mostrar("Cuidado con el precipicio.", 4f); // se oculta sola
///     NubeDialogoBot.Instancia.Ocultar();
/// </summary>
public class NubeDialogoBot : MonoBehaviour
{
    public static NubeDialogoBot Instancia { get; private set; }

    [Header("Contenido")]
    [Tooltip("Texto de arranque. Es sólo un marcador: sustitúyelo por el diálogo real")]
    [TextArea(2, 4)]
    public string textoInicial = "Aqui estoy!!!!";
    [Tooltip("Si está desmarcado, la nube arranca oculta y sólo aparece al llamar a Mostrar()")]
    public bool visibleAlInicio = true;

    [Header("Colocación")]
    [Tooltip("Si lo dejas vacío, mira a la cámara del jugador (o al jugador con tag 'Player')")]
    public Transform objetivoAlQueMira;
    [Tooltip("Altura de la nube por encima del punto de anclaje del bot")]
    public float alturaSobreElBot = 1.25f;
    [Tooltip("Tamaño general de la nube en el mundo")]
    public float escala = 0.0035f;

    [Header("Aspecto")]
    public Color colorFondo = new Color(0.03f, 0.07f, 0.11f, 0.88f);
    public Color colorBorde = new Color(0f, 1f, 1f, 1f);
    public Color colorTexto = new Color(0.85f, 0.98f, 1f, 1f);
    public float tamanoFuente = 30f;
    [Tooltip("Ancho máximo antes de que el texto pase a la línea siguiente")]
    public float anchoMaximo = 330f;
    [Tooltip("Duración del fundido al aparecer y desaparecer")]
    public float duracionFundido = 0.2f;

    private Canvas lienzo;
    private RectTransform globo;
    private LayoutElement limiteTexto;
    private TextMeshProUGUI etiqueta;
    private CanvasGroup grupo;
    private Transform aQuienMirar;
    private Coroutine rutinaFundido;
    private Coroutine rutinaAutoOcultar;

    /// <summary>Texto que se muestra ahora mismo. Asignarlo actualiza la nube al instante.</summary>
    public string Texto
    {
        get => etiqueta != null ? etiqueta.text : textoInicial;
        set => Mostrar(value);
    }

    public bool EstaVisible => grupo != null && grupo.alpha > 0.01f;

    void Awake()
    {
        Instancia = this;
    }

    void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    void Start()
    {
        ConstruirNube();
        BuscarAQuienMirar();

        etiqueta.text = textoInicial;
        AjustarAncho(textoInicial);
        grupo.alpha = visibleAlInicio && !string.IsNullOrWhiteSpace(textoInicial) ? 1f : 0f;
    }

    /// <summary>Muestra un texto. Con duración > 0 la nube se oculta sola al terminar.</summary>
    public void Mostrar(string texto, float duracionSegundos = 0f)
    {
        if (etiqueta == null)
        {
            // Todavía no hemos pasado por Start(): lo guardamos para cuando se construya.
            textoInicial = texto;
            visibleAlInicio = true;
            return;
        }

        if (rutinaAutoOcultar != null)
        {
            StopCoroutine(rutinaAutoOcultar);
            rutinaAutoOcultar = null;
        }

        etiqueta.text = texto;
        AjustarAncho(texto);

        if (string.IsNullOrWhiteSpace(texto))
        {
            Ocultar();
            return;
        }

        Fundir(1f);

        if (duracionSegundos > 0f)
        {
            rutinaAutoOcultar = StartCoroutine(OcultarTrasEspera(duracionSegundos));
        }
    }

    /// <summary>
    /// El globo se ciñe al texto: una frase corta ocupa lo que mide, y sólo a partir de
    /// 'anchoMaximo' se parte en varias líneas.
    /// </summary>
    private void AjustarAncho(string texto)
    {
        if (limiteTexto == null || etiqueta == null) return;

        float anchoNatural = etiqueta.GetPreferredValues(texto ?? string.Empty).x;
        limiteTexto.preferredWidth = Mathf.Min(anchoNatural, anchoMaximo);

        if (globo != null) LayoutRebuilder.ForceRebuildLayoutImmediate(globo);
    }

    public void Ocultar()
    {
        if (rutinaAutoOcultar != null)
        {
            StopCoroutine(rutinaAutoOcultar);
            rutinaAutoOcultar = null;
        }

        Fundir(0f);
    }

    private IEnumerator OcultarTrasEspera(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        rutinaAutoOcultar = null;
        Fundir(0f);
    }

    private void Fundir(float alfaDestino)
    {
        if (grupo == null) return;

        if (rutinaFundido != null) StopCoroutine(rutinaFundido);

        if (duracionFundido <= 0f)
        {
            grupo.alpha = alfaDestino;
            return;
        }

        rutinaFundido = StartCoroutine(RutinaFundido(alfaDestino));
    }

    private IEnumerator RutinaFundido(float alfaDestino)
    {
        float alfaInicial = grupo.alpha;
        float transcurrido = 0f;

        while (transcurrido < duracionFundido)
        {
            transcurrido += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(alfaInicial, alfaDestino, transcurrido / duracionFundido);
            yield return null;
        }

        grupo.alpha = alfaDestino;
        rutinaFundido = null;
    }

    /// <summary>
    /// En LateUpdate para que el giro se aplique después de que el bot se haya movido
    /// en su propio Update; si no, la nube iría un frame por detrás.
    /// </summary>
    void LateUpdate()
    {
        if (lienzo == null) return;

        if (aQuienMirar == null) BuscarAQuienMirar();
        if (aQuienMirar == null) return;

        Transform lienzoTransform = lienzo.transform;
        lienzoTransform.localPosition = new Vector3(0f, alturaSobreElBot, 0f);
        lienzoTransform.localScale = Vector3.one * escala;

        // La cara visible del lienzo es su +Z, así que apunta en el sentido
        // "desde el jugador hacia la nube". Con Vector3.up el texto nunca se inclina.
        Vector3 haciaLaNube = lienzoTransform.position - aQuienMirar.position;
        haciaLaNube.y = 0f;

        if (haciaLaNube.sqrMagnitude > 0.0001f)
        {
            lienzoTransform.rotation = Quaternion.LookRotation(haciaLaNube.normalized, Vector3.up);
        }
    }

    private void BuscarAQuienMirar()
    {
        if (objetivoAlQueMira != null)
        {
            aQuienMirar = objetivoAlQueMira;
            return;
        }

        GameObject playerObjeto = GameObject.FindGameObjectWithTag("Player");
        if (playerObjeto != null)
        {
            // Preferimos la cámara del jugador: es el punto de vista real desde el que se lee.
            Camera camaraJugador = playerObjeto.GetComponentInChildren<Camera>();
            aQuienMirar = camaraJugador != null ? camaraJugador.transform : playerObjeto.transform;
            return;
        }

        if (Camera.main != null) aQuienMirar = Camera.main.transform;
    }

    private void ConstruirNube()
    {
        GameObject objetoLienzo = new GameObject("NubeDialogo");
        objetoLienzo.transform.SetParent(transform, false);
        objetoLienzo.transform.localPosition = new Vector3(0f, alturaSobreElBot, 0f);
        objetoLienzo.transform.localScale = Vector3.one * escala;

        lienzo = objetoLienzo.AddComponent<Canvas>();
        lienzo.renderMode = RenderMode.WorldSpace;
        lienzo.sortingOrder = 10;

        // Sin raycaster: la nube es decorativa y no debe robar clics al juego.
        grupo = objetoLienzo.AddComponent<CanvasGroup>();
        grupo.interactable = false;
        grupo.blocksRaycasts = false;

        RectTransform rectLienzo = objetoLienzo.GetComponent<RectTransform>();
        rectLienzo.sizeDelta = new Vector2(anchoMaximo, 100f);

        // --- Globo ---
        GameObject objetoGlobo = new GameObject("Globo", typeof(RectTransform));
        objetoGlobo.transform.SetParent(objetoLienzo.transform, false);
        globo = objetoGlobo.GetComponent<RectTransform>();
        globo.anchorMin = new Vector2(0.5f, 0f);
        globo.anchorMax = new Vector2(0.5f, 0f);
        globo.pivot = new Vector2(0.5f, 0f);
        globo.anchoredPosition = Vector2.zero;

        Image fondo = objetoGlobo.AddComponent<Image>();
        fondo.sprite = CrearSpriteGlobo(colorFondo, colorBorde);
        fondo.type = Image.Type.Sliced;
        fondo.raycastTarget = false;

        // El layout + el content size fitter hacen que el globo se ajuste solo al texto.
        HorizontalLayoutGroup disposicion = objetoGlobo.AddComponent<HorizontalLayoutGroup>();
        disposicion.padding = new RectOffset(26, 26, 18, 18);
        disposicion.childForceExpandWidth = false;
        disposicion.childForceExpandHeight = false;
        disposicion.childControlWidth = true;
        disposicion.childControlHeight = true;

        ContentSizeFitter ajuste = objetoGlobo.AddComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // --- Texto ---
        GameObject objetoTexto = new GameObject("Texto", typeof(RectTransform));
        objetoTexto.transform.SetParent(objetoGlobo.transform, false);

        etiqueta = objetoTexto.AddComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) etiqueta.font = TMP_Settings.defaultFontAsset;
        etiqueta.fontSize = tamanoFuente;
        etiqueta.color = colorTexto;
        etiqueta.alignment = TextAlignmentOptions.Center;
        etiqueta.textWrappingMode = TextWrappingModes.Normal;
        etiqueta.raycastTarget = false;

        limiteTexto = objetoTexto.AddComponent<LayoutElement>();
        limiteTexto.preferredWidth = anchoMaximo;

        // --- Cola que apunta hacia el bot ---
        GameObject objetoCola = new GameObject("Cola", typeof(RectTransform));
        objetoCola.transform.SetParent(objetoGlobo.transform, false);

        RectTransform rectCola = objetoCola.GetComponent<RectTransform>();
        rectCola.anchorMin = new Vector2(0.5f, 0f);
        rectCola.anchorMax = new Vector2(0.5f, 0f);
        rectCola.pivot = new Vector2(0.5f, 1f);
        rectCola.sizeDelta = new Vector2(34f, 24f);
        rectCola.anchoredPosition = new Vector2(0f, 2f);

        Image imagenCola = objetoCola.AddComponent<Image>();
        imagenCola.sprite = CrearSpriteCola(colorFondo);
        imagenCola.raycastTarget = false;

        // La cola va anclada a mano, no la coloca el layout del globo.
        LayoutElement colaSinLayout = objetoCola.AddComponent<LayoutElement>();
        colaSinLayout.ignoreLayout = true;
    }

    /// <summary>Rectángulo redondeado con borde, pensado para dibujarse en 9 trozos (sliced).</summary>
    private static Sprite CrearSpriteGlobo(Color relleno, Color borde)
    {
        const int lado = 64;
        const float radio = 18f;
        const float grosorBorde = 3f;

        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                // Distancia con signo al borde del rectángulo redondeado.
                float dx = Mathf.Max(radio - (x + 0.5f), (x + 0.5f) - (lado - radio), 0f);
                float dy = Mathf.Max(radio - (y + 0.5f), (y + 0.5f) - (lado - radio), 0f);
                float distancia = Mathf.Sqrt(dx * dx + dy * dy) - radio;

                Color color;
                if (distancia > 0f) color = Color.clear;                    // Fuera de la nube
                else if (distancia > -grosorBorde) color = borde;           // Filo de neón
                else color = relleno;                                       // Interior

                // Suavizado de un píxel para que las esquinas no salgan dentadas.
                if (distancia > -1f && distancia <= 0f)
                {
                    color.a *= Mathf.Clamp01(-distancia);
                }

                textura.SetPixel(x, y, color);
            }
        }

        textura.Apply();

        const float margen = 24f; // Mayor que el radio: las esquinas nunca se estiran.
        return Sprite.Create(
            textura,
            new Rect(0f, 0f, lado, lado),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect,
            new Vector4(margen, margen, margen, margen));
    }

    /// <summary>Triángulo apuntando hacia abajo, hacia la cabeza del bot.</summary>
    private static Sprite CrearSpriteCola(Color relleno)
    {
        const int lado = 32;

        Texture2D textura = new Texture2D(lado, lado, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < lado; y++)
        {
            for (int x = 0; x < lado; x++)
            {
                // El ancho disponible se estrecha conforme se baja hacia la punta.
                float mitadAncho = lado * 0.5f * (y / (float)(lado - 1));
                float distanciaAlCentro = Mathf.Abs(x + 0.5f - lado * 0.5f);

                Color color = relleno;
                color.a *= Mathf.Clamp01(mitadAncho - distanciaAlCentro);
                textura.SetPixel(x, y, color);
            }
        }

        textura.Apply();
        return Sprite.Create(textura, new Rect(0f, 0f, lado, lado), new Vector2(0.5f, 1f), 100f);
    }
}

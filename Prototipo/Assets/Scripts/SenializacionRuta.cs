using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dibuja en el SUELO la ruta hasta el siguiente objetivo, como una línea de neón que el
/// jugador puede seguir. La línea se pega al piso y se renderiza con test de profundidad
/// normal, así que las paredes la tapan: nunca se ve a través de un muro.
/// </summary>
public class SenializacionRuta : MonoBehaviour
{
    public static SenializacionRuta Instancia { get; private set; }

    [Header("Seguimiento del jugador")]
    [Tooltip("Si lo dejas vacío, lo busca automáticamente por el tag 'Player'")]
    public Transform jugador;

    [Header("Apariencia de la línea")]
    public Color colorLinea = new Color(0f, 1f, 1f, 1f);
    public float anchoLinea = 0.18f;
    [Tooltip("Separación sobre el piso. Súbelo un poco si ves parpadeo con el suelo (z-fighting)")]
    public float alturaSobreSuelo = 0.04f;
    [Tooltip("Distancia entre puntos de la línea: más bajo = se adapta mejor a rampas y escalones")]
    public float resolucionLinea = 0.35f;
    [Tooltip("Velocidad del desplazamiento de las marcas hacia el objetivo (0 = línea fija)")]
    public float velocidadFlujo = 1.5f;

    [Header("Recorte")]
    [Tooltip("Metros de línea que se muestran por delante del jugador (0 = toda la ruta)")]
    public float longitudVisible = 0f;
    [Tooltip("La línea arranca a esta distancia del jugador, para que no le quede bajo los pies")]
    public float margenInicial = 0.6f;

    [Header("Actualización de ruta")]
    [Tooltip("Cada cuántos segundos recalcula el camino (por rendimiento)")]
    public float intervaloRecalculo = 0.4f;

    [Header("Destino final")]
    [Tooltip("La línea siempre apunta hacia GameManager.puertaFinalOficina. Al llegar a esta distancia, deja de mostrarse.")]
    public float distanciaLlegadaFinal = 2f;

    /// <summary>Ruta actual pegada al suelo. El BotGuia la usa para caminar por delante del jugador.</summary>
    public IReadOnlyList<Vector3> RutaActual => rutaSuavizada;

    private BaldosaPregunta[] baldosas;
    private bool yaLlegoADestino;
    private LineRenderer linea;
    private Material materialLinea;
    private float tiempoDesdeUltimoCalculo;
    private bool avisoSinRuta;

    private readonly List<Vector3> rutaSuavizada = new List<Vector3>();
    private readonly List<Vector3> puntosLinea = new List<Vector3>();

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
        if (jugador == null)
        {
            GameObject playerObjeto = GameObject.FindGameObjectWithTag("Player");
            if (playerObjeto != null) jugador = playerObjeto.transform;
        }

        if (jugador == null)
        {
            Debug.LogWarning("[SenializacionRuta] No se encontró ningún GameObject con el tag 'Player'.");
        }

        baldosas = FindObjectsByType<BaldosaPregunta>(FindObjectsInactive.Exclude);
        if (jugador != null) NavegacionSuelo.Instancia.RegistrarIgnorado(jugador.gameObject);
        ConfigurarLinea();

        Debug.Log($"[SenializacionRuta] Inicializado. Jugador encontrado: {jugador != null}. Baldosas detectadas: {baldosas.Length}.");

        ActualizarRuta();
    }

    private void ConfigurarLinea()
    {
        GameObject objetoLinea = new GameObject("LineaGuiaSuelo");
        objetoLinea.transform.SetParent(transform, false);

        linea = objetoLinea.AddComponent<LineRenderer>();
        linea.useWorldSpace = true;
        linea.alignment = LineAlignment.TransformZ;                       // Cinta plana sobre el piso...
        objetoLinea.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);  // ...con su cara mirando hacia arriba.
        linea.textureMode = LineTextureMode.Tile;
        linea.numCapVertices = 2;
        linea.numCornerVertices = 4;
        linea.startWidth = anchoLinea;
        linea.endWidth = anchoLinea;
        linea.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        linea.receiveShadows = false;
        linea.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        linea.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        linea.positionCount = 0;

        materialLinea = new Material(ObtenerShaderTransparente());
        AplicarColor(materialLinea, colorLinea);
        materialLinea.mainTexture = CrearTexturaGuiones();
        materialLinea.mainTextureScale = new Vector2(1f, 1f);
        linea.material = materialLinea;

        linea.startColor = colorLinea;
        linea.endColor = colorLinea;
    }

    private static Shader ObtenerShaderTransparente()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Unlit/Transparent");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        return shader;
    }

    private static void AplicarColor(Material material, Color color)
    {
        // El Unlit de URP necesita que se le declare la mezcla alfa a mano cuando se crea por código.
        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
        if (material.HasProperty("_Blend")) material.SetFloat("_Blend", 0f);
        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
        if (material.HasProperty("_Cull")) material.SetFloat("_Cull", 0f); // Doble cara, por si se mira desde abajo.

        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHATEST_ON");
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;

        // Brillo de neón si el proyecto tiene bloom activo en el volumen de post-proceso.
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 2f);
        }
    }

    /// <summary>Textura de guiones que, al desplazarse, marca el sentido de la marcha.</summary>
    private static Texture2D CrearTexturaGuiones()
    {
        const int ancho = 32;
        Texture2D textura = new Texture2D(ancho, 1, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Repeat;
        textura.filterMode = FilterMode.Bilinear;

        for (int x = 0; x < ancho; x++)
        {
            // Guion largo y opaco, separación corta y semitransparente.
            float alfa = x < ancho * 0.7f ? 1f : 0.25f;
            textura.SetPixel(x, 0, new Color(1f, 1f, 1f, alfa));
        }

        textura.Apply();
        return textura;
    }

    void Update()
    {
        if (jugador == null) return;

        tiempoDesdeUltimoCalculo += Time.deltaTime;
        if (tiempoDesdeUltimoCalculo >= intervaloRecalculo)
        {
            tiempoDesdeUltimoCalculo = 0f;
            ActualizarRuta();
        }

        if (materialLinea != null && velocidadFlujo != 0f)
        {
            float desplazamiento = -Time.time * velocidadFlujo;
            materialLinea.mainTextureOffset = new Vector2(desplazamiento, 0f);
        }
    }

    private void ActualizarRuta()
    {
        Transform objetivo = ObtenerObjetivoActual();
        if (objetivo == null)
        {
            OcultarLinea();
            return;
        }

        bool huboRuta = NavegacionSuelo.Instancia.CalcularRuta(jugador.position, objetivo.position, rutaSuavizada);

        if (!huboRuta)
        {
            if (!avisoSinRuta)
            {
                avisoSinRuta = true;
                Debug.LogWarning("[SenializacionRuta] No se encontró un camino caminable hasta el objetivo (¿puerta cerrada o suelo aún no activado?). La línea se oculta hasta que exista ruta.");
            }
            rutaSuavizada.Clear();
            OcultarLinea();
            return;
        }

        avisoSinRuta = false;
        ConstruirPuntosLinea();
        DibujarLinea();
    }

    /// <summary>Remuestrea la ruta y apoya cada punto en el piso real, para que siga rampas y escalones.</summary>
    private void ConstruirPuntosLinea()
    {
        puntosLinea.Clear();

        float recorrido = 0f;
        float limite = longitudVisible > 0f ? longitudVisible + margenInicial : float.MaxValue;
        float paso = Mathf.Max(0.1f, resolucionLinea);

        for (int i = 0; i < rutaSuavizada.Count - 1; i++)
        {
            Vector3 inicio = rutaSuavizada[i];
            Vector3 fin = rutaSuavizada[i + 1];
            float largo = Vector3.Distance(inicio, fin);
            if (largo < 0.01f) continue;

            int muestras = Mathf.Max(1, Mathf.CeilToInt(largo / paso));
            for (int j = 0; j < muestras; j++)
            {
                float t = j / (float)muestras;
                float distanciaAcumulada = recorrido + largo * t;

                if (distanciaAcumulada < margenInicial) continue;
                if (distanciaAcumulada > limite) return;

                AgregarPuntoPegadoAlSuelo(Vector3.Lerp(inicio, fin, t));
            }

            recorrido += largo;
        }

        if (recorrido <= limite && rutaSuavizada.Count > 0)
        {
            AgregarPuntoPegadoAlSuelo(rutaSuavizada[rutaSuavizada.Count - 1]);
        }
    }

    private void AgregarPuntoPegadoAlSuelo(Vector3 punto)
    {
        float altura = NavegacionSuelo.Instancia.AlturaDelSuelo(punto, out float alturaSuelo) ? alturaSuelo : punto.y;
        puntosLinea.Add(new Vector3(punto.x, altura + alturaSobreSuelo, punto.z));
    }

    private void DibujarLinea()
    {
        if (linea == null) return;

        if (puntosLinea.Count < 2)
        {
            OcultarLinea();
            return;
        }

        linea.enabled = true;
        linea.startWidth = anchoLinea;
        linea.endWidth = anchoLinea;
        linea.positionCount = puntosLinea.Count;
        linea.SetPositions(puntosLinea.ToArray());

        // Una repetición de la textura por metro, para que los guiones no se estiren en tramos largos.
        float longitudTotal = 0f;
        for (int i = 0; i < puntosLinea.Count - 1; i++)
        {
            longitudTotal += Vector3.Distance(puntosLinea[i], puntosLinea[i + 1]);
        }
        if (materialLinea != null)
        {
            materialLinea.mainTextureScale = new Vector2(Mathf.Max(1f, longitudTotal), 1f);
        }
    }

    private void OcultarLinea()
    {
        if (linea != null)
        {
            linea.positionCount = 0;
            linea.enabled = false;
        }
    }

    public Transform ObtenerObjetivoActual()
    {
        if (jugador == null) return null;
        if (yaLlegoADestino) return null;

        if (GameManager.Instance == null || GameManager.Instance.puertaFinalOficina == null)
            return null;

        Transform puerta = GameManager.Instance.puertaFinalOficina.transform;

        // La línea guía siempre apunta a la puerta final (antes de las baldosas). Una vez
        // que el jugador llega, se apaga para el resto de la partida (no vuelve a guiar
        // hacia las baldosas Piso 1-5).
        if (Vector3.Distance(jugador.position, puerta.position) <= distanciaLlegadaFinal)
        {
            yaLlegoADestino = true;
            return null;
        }

        return puerta;
    }
}

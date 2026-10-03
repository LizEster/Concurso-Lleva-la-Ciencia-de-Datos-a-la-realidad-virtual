using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Puntero de mirada: un puntito en el centro de la vista que sirve para elegir opciones
/// (OpcionMirable): apuntas con la mirada (la opción y el anillo se iluminan) y aprietas
/// A, B, X o Y para elegirla. Se crea solo al iniciar el juego (no hay que arrastrarlo a
/// nada) y sólo se ve cuando hay opciones para elegir.
/// En el visor la mirada es la cabeza; en PC es hacia donde apunta la cámara (el mouse).
/// </summary>
[DefaultExecutionOrder(500)]
public class PunteroMirada : MonoBehaviour
{
    public static PunteroMirada Instancia { get; private set; }

    private const float DistanciaMaxima = 20f;      // metros: más lejos no cuenta como "mirar"
    private const float DistanciaSinObjetivo = 2f;  // dónde flota el puntito si no miras ninguna opción
    private const float EsperaTrasElegir = 0.6f;    // segundos sin elegir nada después de una elección
    private const float TamanoAngular = 0.00062f;   // escala por metro: el puntito se ve del mismo tamaño a cualquier distancia

    private static readonly Color ColorPunto = new Color(1f, 1f, 1f, 0.9f);
    private static readonly Color ColorAnilloFondo = new Color(1f, 1f, 1f, 0.25f);
    private static readonly Color ColorAnillo = new Color(0.35f, 1f, 0.55f, 1f);

    /// <summary>
    /// Frame en que se eligió algo con el puntero. Otros scripts que también escuchan el botón A
    /// (la terminal, el "continuar" del final) lo revisan para no reaccionar al mismo botonazo.
    /// </summary>
    public static int FrameUltimaEleccion { get; private set; } = -1;

    private OpcionMirable actual;
    private float bloqueoHasta;

    private Transform reticula;
    private Image anillo;
    private Transform cabezaActual;
    private float distanciaActual = DistanciaSinObjetivo;

    private static Camera camaraJugador;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Instancia != null) return;
        GameObject go = new GameObject("[PunteroMirada]");
        DontDestroyOnLoad(go);
        Instancia = go.AddComponent<PunteroMirada>();
    }

    /// <summary>
    /// Los ojos del jugador: la cabeza del visor en VR o la cámara del Player en PC (que no
    /// siempre es Camera.main, porque en PC viene etiquetada "Player").
    /// </summary>
    public static Transform Cabeza()
    {
        if (RigVR.Cabeza != null) return RigVR.Cabeza;

        if (camaraJugador == null || !camaraJugador.isActiveAndEnabled)
        {
            camaraJugador = null;
            PlayerMovement jugador = Object.FindAnyObjectByType<PlayerMovement>();
            if (jugador != null) camaraJugador = jugador.GetComponentInChildren<Camera>();
            if (camaraJugador == null) camaraJugador = Camera.main;
        }
        return camaraJugador != null ? camaraJugador.transform : null;
    }

    void Awake()
    {
        CrearReticula();
    }

    void OnEnable()
    {
        Application.onBeforeRender += PosicionarReticula;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= PosicionarReticula;
    }

    void LateUpdate()
    {
        Transform cabeza = Cabeza();
        cabezaActual = cabeza;
        if (cabeza == null)
        {
            reticula.gameObject.SetActive(false);
            return;
        }

        Ray rayo = new Ray(cabeza.position, cabeza.forward);

        OpcionMirable mirada = null;
        float distanciaMirada = float.MaxValue;
        bool hayOpciones = false;

        foreach (OpcionMirable opcion in OpcionMirable.Activas)
        {
            if (opcion == null || !opcion.Visible) continue;
            hayOpciones = true;

            if (opcion.Intersecta(rayo, out float d) && d <= DistanciaMaxima && d < distanciaMirada)
            {
                mirada = opcion;
                distanciaMirada = d;
            }
        }

        if (Time.unscaledTime < bloqueoHasta) mirada = null;

        if (mirada != actual)
        {
            if (actual != null) actual.SetMirada(false);
            actual = mirada;
            if (actual != null) actual.SetMirada(true);
        }

        // El anillo se enciende cuando estás apuntando a algo: ahí un botón lo elige.
        anillo.fillAmount = actual != null ? 1f : 0f;

        if (actual != null && !actual.soloMirar && EntradaVR.ConfirmarPresionado())
        {
            OpcionMirable elegida = actual;
            actual = null;
            bloqueoHasta = Time.unscaledTime + EsperaTrasElegir;
            FrameUltimaEleccion = Time.frameCount;
            elegida.Elegir();
        }

        reticula.gameObject.SetActive(hayOpciones);
        distanciaActual = mirada != null ? Mathf.Max(0.3f, distanciaMirada * 0.97f) : DistanciaSinObjetivo;
        PosicionarReticula();
    }

    /// <summary>
    /// Se llama en LateUpdate y otra vez justo antes de dibujar: en el visor la cabeza se
    /// actualiza a último momento, y si no el puntito se "arrastraría" un poco al girar.
    /// </summary>
    private void PosicionarReticula()
    {
        if (reticula == null || cabezaActual == null || !reticula.gameObject.activeSelf) return;

        reticula.SetPositionAndRotation(
            cabezaActual.position + cabezaActual.forward * distanciaActual,
            cabezaActual.rotation);
        reticula.localScale = Vector3.one * (TamanoAngular * distanciaActual);
    }

    private void CrearReticula()
    {
        GameObject go = new GameObject("Reticula", typeof(RectTransform));
        go.transform.SetParent(transform, false);

        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 32767; // encima de cualquier otro Canvas

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(100f, 100f);
        reticula = go.transform;

        // Material de UI que se dibuja siempre encima, aunque haya una pared delante.
        Material material = new Material(Canvas.GetDefaultCanvasMaterial());
        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        Sprite disco = CrearSpriteCirculo(128, 0f);
        Sprite aro = CrearSpriteCirculo(128, 0.72f);

        CrearImagen(rt, "AnilloFondo", aro, 70f, material, ColorAnilloFondo);
        anillo = CrearImagen(rt, "Anillo", aro, 70f, material, ColorAnillo);
        anillo.type = Image.Type.Filled; // fillAmount 0 = apagado, 1 = apuntando a una opción
        anillo.fillMethod = Image.FillMethod.Radial360;
        anillo.fillAmount = 0f;
        CrearImagen(rt, "Punto", disco, 16f, material, ColorPunto);

        go.SetActive(false);
    }

    private static Image CrearImagen(RectTransform padre, string nombre, Sprite sprite, float tamano, Material material, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(tamano, tamano);

        Image imagen = go.AddComponent<Image>();
        imagen.sprite = sprite;
        imagen.material = material;
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    /// <summary>Dibuja un círculo blanco (o un aro, si radioInterior > 0) con bordes suaves.</summary>
    private static Sprite CrearSpriteCirculo(int tamano, float radioInterior)
    {
        Texture2D textura = new Texture2D(tamano, tamano, TextureFormat.RGBA32, false);
        textura.wrapMode = TextureWrapMode.Clamp;
        textura.filterMode = FilterMode.Bilinear;

        float radio = tamano * 0.5f;
        Color32[] pixeles = new Color32[tamano * tamano];
        for (int y = 0; y < tamano; y++)
        {
            for (int x = 0; x < tamano; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radio, radio)) / radio;
                float exterior = Mathf.Clamp01((1f - d) * radio);
                float interior = radioInterior > 0f ? Mathf.Clamp01((d - radioInterior) * radio) : 1f;
                pixeles[y * tamano + x] = new Color32(255, 255, 255, (byte)(255f * Mathf.Min(exterior, interior)));
            }
        }
        textura.SetPixels32(pixeles);
        textura.Apply();

        return Sprite.Create(textura, new Rect(0, 0, tamano, tamano), new Vector2(0.5f, 0.5f), 100f);
    }
}

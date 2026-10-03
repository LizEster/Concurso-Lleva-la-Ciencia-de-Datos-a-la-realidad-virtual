using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Estilo visual "visor futurista" compartido (el mismo del menú principal): colores, fuente
/// y piezas de UI construidas por código (paneles, textos, botones elegibles con la mirada).
/// Todo se dibuja ENCIMA del mundo, para que una pared o baranda no tape un panel.
/// </summary>
public static class EstiloUI
{
    public static readonly Color Cian = new Color(0.2f, 0.9f, 1f, 1f);
    public static readonly Color CianSuave = new Color(0.2f, 0.9f, 1f, 0.35f);
    public static readonly Color FondoPanel = new Color(0.01f, 0.03f, 0.06f, 0.92f);
    public static readonly Color BotonNormal = new Color(0.02f, 0.14f, 0.2f, 0.95f);
    public static readonly Color BotonMirada = new Color(0.1f, 0.65f, 0.85f, 1f);
    public static readonly Color TextoBotonMirada = new Color(0.02f, 0.05f, 0.08f, 1f);

    public static readonly Color Magenta = new Color(1f, 0.4f, 0.7f, 1f);
    public static readonly Color BotonIANormal = new Color(0.2f, 0.03f, 0.12f, 0.95f);
    public static readonly Color BotonIAMirada = new Color(0.95f, 0.3f, 0.6f, 1f);

    public static readonly Color Rojo = new Color(1f, 0.3f, 0.3f, 1f);

    private static Material materialUI;
    private static Material materialTexto;
    private static TMP_FontAsset fuenteDelMaterial;

    /// <summary>La fuente del juego: la misma del texto de la terminal del HUD.</summary>
    public static TMP_FontAsset Fuente()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null &&
            GameManager.Instance.uiManager.textoTerminal != null && GameManager.Instance.uiManager.textoTerminal.font != null)
        {
            return GameManager.Instance.uiManager.textoTerminal.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    public static Material MaterialUI()
    {
        if (materialUI == null)
        {
            materialUI = new Material(Canvas.GetDefaultCanvasMaterial());
            materialUI.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
        }
        return materialUI;
    }

    public static Material MaterialTexto(TMP_FontAsset fuente)
    {
        if (materialTexto == null || fuenteDelMaterial != fuente)
        {
            materialTexto = new Material(fuente.material);
            materialTexto.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
            fuenteDelMaterial = fuente;
        }
        return materialTexto;
    }

    public static RectTransform CrearCanvas(string nombre, int orden, Vector2 tamano, float metrosPorPx)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = orden;

        RectTransform rt = (RectTransform)go.transform;
        rt.sizeDelta = tamano;
        rt.localScale = Vector3.one * metrosPorPx;
        return rt;
    }

    public static Image CrearImagen(RectTransform padre, string nombre, Vector2 posicion, Vector2 tamano, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        Image img = go.AddComponent<Image>();
        img.material = MaterialUI();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    public static TextMeshProUGUI CrearTexto(RectTransform padre, string texto, Vector2 posicion, Vector2 tamano,
                                             float tamanoFuente, Color color, FontStyles estilo)
    {
        GameObject go = new GameObject("Texto", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = tamano;

        TMP_FontAsset fuente = Fuente();
        TextMeshProUGUI txt = go.AddComponent<TextMeshProUGUI>();
        txt.font = fuente;
        txt.fontSharedMaterial = MaterialTexto(fuente);
        txt.text = texto;
        txt.fontSize = tamanoFuente;
        txt.color = color;
        txt.fontStyle = estilo;
        txt.alignment = TextAlignmentOptions.Center;
        txt.enableWordWrapping = false;
        txt.raycastTarget = false;
        return txt;
    }

    /// <summary>
    /// Botón que se elige apuntándolo con la mirada y apretando un botón del control.
    /// Devuelve su OpcionMirable (para deshabilitarlo, etc.).
    /// </summary>
    public static OpcionMirable CrearBoton(RectTransform padre, string texto, Vector2 posicion, Vector2 tamano, float tamanoFuente,
                                           Color fondoNormal, Color fondoMirada, Color colorTexto, Color colorBorde, Action alElegir)
    {
        Image fondo = CrearImagen(padre, "Boton", posicion, tamano, fondoNormal);
        RectTransform rt = fondo.rectTransform;
        CrearBorde(rt, tamano.x, tamano.y, 3f, colorBorde);

        TextMeshProUGUI txt = CrearTexto(rt, texto, Vector2.zero, tamano - new Vector2(40f, 10f), tamanoFuente, colorTexto, FontStyles.Bold);
        txt.enableAutoSizing = true; // si el texto es largo, se achica para caber
        txt.fontSizeMax = tamanoFuente;
        txt.fontSizeMin = tamanoFuente * 0.55f;

        OpcionMirable mirable = fondo.gameObject.AddComponent<OpcionMirable>();
        mirable.Configurar(fondo, fondoNormal, fondoMirada, txt, TextoBotonMirada, alElegir);
        return mirable;
    }

    /// <summary>Marco de 4 líneas finas alrededor de un rectángulo centrado de 'ancho' x 'alto'.</summary>
    public static void CrearBorde(RectTransform padre, float ancho, float alto, float grosor, Color color)
    {
        CrearImagen(padre, "BordeArriba", new Vector2(0f, alto * 0.5f), new Vector2(ancho, grosor), color);
        CrearImagen(padre, "BordeAbajo", new Vector2(0f, -alto * 0.5f), new Vector2(ancho, grosor), color);
        CrearImagen(padre, "BordeIzq", new Vector2(-ancho * 0.5f, 0f), new Vector2(grosor, alto), color);
        CrearImagen(padre, "BordeDer", new Vector2(ancho * 0.5f, 0f), new Vector2(grosor, alto), color);
    }

    /// <summary>Esquinas en forma de L, gruesas, para el look "visor futurista".</summary>
    public static void CrearEsquinas(RectTransform padre, float ancho, float alto, float largo, float grosor, Color color)
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

    public static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>Pone 'panel' delante de la mirada a 'distancia' metros, de pie y mirando al jugador.</summary>
    public static void ColocarDelante(Transform panel, float distancia, float bajarMetros)
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;

        Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
        if (adelante.sqrMagnitude < 0.0001f) adelante = cabeza.up;
        adelante.Normalize();

        panel.SetPositionAndRotation(
            cabeza.position + adelante * distancia + Vector3.down * bajarMetros,
            Quaternion.LookRotation(adelante, Vector3.up));
    }
}

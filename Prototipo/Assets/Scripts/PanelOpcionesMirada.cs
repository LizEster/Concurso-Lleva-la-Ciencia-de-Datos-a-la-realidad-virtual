using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

/// <summary>
/// Panel flotante con las opciones del jugador (por ejemplo, las respuestas al robot en el
/// diálogo inicial), opcionalmente con un mensaje encima (como la advertencia de la terminal).
/// Aparece delante de la mirada y se queda QUIETO en el mundo, para que puedas mirar cada
/// opción; se elige apuntándola con la mirada y apretando A/B/X/Y (ver PunteroMirada).
/// Se crea solo la primera vez que se usa: no hay que ponerlo en la escena.
/// </summary>
public class PanelOpcionesMirada : MonoBehaviour
{
    private const float AnchoPx = 900f;
    private const float MetrosPorPx = 0.0013f;   // 900 px ≈ 1,17 m de ancho
    private const float Distancia = 1.6f;        // metros delante de los ojos
    private const float BajarMetros = 0.2f;      // un poco bajo la línea de los ojos, más cómodo para leer
    private const float Espacio = 18f;           // px entre opciones
    private const float Margen = 36f;            // px de borde cuando hay mensaje

    private static readonly Color ColorFondoPanel = new Color(0.02f, 0.05f, 0.08f, 0.92f);
    private static readonly Color ColorFondoNormal = new Color(0.02f, 0.12f, 0.06f, 0.85f);
    private static readonly Color ColorFondoMirada = new Color(0.1f, 0.55f, 0.25f, 0.95f);
    private static readonly Color ColorTexto = Color.white;
    private static readonly Color ColorTextoMirada = new Color(0.75f, 1f, 0.8f, 1f);

    private static PanelOpcionesMirada instancia;

    private RectTransform contenedor;
    private Image fondoPanel;
    private Material materialFondo;
    private Material materialTexto;
    private TMP_FontAsset fuenteMaterial;

    /// <summary>
    /// Muestra las opciones delante del jugador. 'alElegir' recibe el índice de la opción
    /// elegida. 'fuente' es la misma TMP_FontAsset que usa el resto de la UI.
    /// </summary>
    public static void Mostrar(string[] opciones, TMP_FontAsset fuente, float tamanoFuente, Action<int> alElegir)
    {
        Mostrar(null, opciones, fuente, tamanoFuente, alElegir);
    }

    /// <summary>
    /// Igual, pero con un 'mensaje' arriba de las opciones (acepta rich text de TextMeshPro).
    /// Se puede llamar sin opciones para mostrar sólo el mensaje (ej. "Abriendo puerta...").
    /// </summary>
    public static void Mostrar(string mensaje, string[] opciones, TMP_FontAsset fuente, float tamanoFuente, Action<int> alElegir)
    {
        if (opciones == null) opciones = new string[0];
        if (opciones.Length == 0 && string.IsNullOrEmpty(mensaje))
        {
            Ocultar();
            return;
        }

        if (instancia == null) instancia = Construir();
        instancia.Armar(mensaje, opciones, fuente, tamanoFuente, alElegir);
    }

    public static void Ocultar()
    {
        if (instancia == null) return;
        instancia.Limpiar();
        instancia.gameObject.SetActive(false);
    }

    private static PanelOpcionesMirada Construir()
    {
        GameObject go = new GameObject("PanelOpcionesMirada", typeof(RectTransform));
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 100;

        PanelOpcionesMirada panel = go.AddComponent<PanelOpcionesMirada>();
        panel.contenedor = (RectTransform)go.transform;
        panel.contenedor.localScale = Vector3.one * MetrosPorPx;

        // Se dibuja encima de todo, por si queda medio metido en una pared.
        panel.materialFondo = new Material(Canvas.GetDefaultCanvasMaterial());
        panel.materialFondo.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);

        // Fondo de todo el panel: sólo se ve cuando hay mensaje.
        panel.fondoPanel = go.AddComponent<Image>();
        panel.fondoPanel.material = panel.materialFondo;
        panel.fondoPanel.color = ColorFondoPanel;
        panel.fondoPanel.raycastTarget = false;
        return panel;
    }

    private void Armar(string mensaje, string[] opciones, TMP_FontAsset fuente, float tamanoFuente, Action<int> alElegir)
    {
        Limpiar();
        gameObject.SetActive(true);
        Colocar();

        if (fuente == null) fuente = TMP_Settings.defaultFontAsset;
        if (materialTexto == null || fuenteMaterial != fuente)
        {
            materialTexto = new Material(fuente.material);
            materialTexto.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
            fuenteMaterial = fuente;
        }

        bool hayMensaje = !string.IsNullOrEmpty(mensaje);
        float margen = hayMensaje ? Margen : 0f;
        float alto = tamanoFuente * 1.9f;
        float y = margen; // px usados desde arriba

        if (hayMensaje)
        {
            GameObject mensajeGO = new GameObject("Mensaje", typeof(RectTransform));
            mensajeGO.transform.SetParent(contenedor, false);

            TextMeshProUGUI textoMensaje = mensajeGO.AddComponent<TextMeshProUGUI>();
            textoMensaje.font = fuente;
            textoMensaje.fontSharedMaterial = materialTexto;
            textoMensaje.fontSize = tamanoFuente * 0.8f;
            textoMensaje.color = ColorTexto;
            textoMensaje.alignment = TextAlignmentOptions.TopLeft;
            textoMensaje.enableWordWrapping = true;
            textoMensaje.raycastTarget = false;
            textoMensaje.text = mensaje;

            float anchoTexto = AnchoPx - 2f * margen;
            float altoMensaje = textoMensaje.GetPreferredValues(mensaje, anchoTexto, 0f).y;

            RectTransform mensajeRT = (RectTransform)mensajeGO.transform;
            mensajeRT.anchorMin = new Vector2(0f, 1f);
            mensajeRT.anchorMax = new Vector2(1f, 1f);
            mensajeRT.pivot = new Vector2(0.5f, 1f);
            mensajeRT.sizeDelta = new Vector2(-2f * margen, altoMensaje);
            mensajeRT.anchoredPosition = new Vector2(0f, -y);

            y += altoMensaje;
            if (opciones.Length > 0) y += Espacio * 2f;
        }

        float altoOpciones = opciones.Length > 0 ? opciones.Length * alto + (opciones.Length - 1) * Espacio : 0f;
        contenedor.sizeDelta = new Vector2(AnchoPx, y + altoOpciones + margen);
        fondoPanel.enabled = hayMensaje;

        for (int i = 0; i < opciones.Length; i++)
        {
            int indice = i;

            GameObject fila = new GameObject($"Opcion {i + 1}", typeof(RectTransform));
            fila.transform.SetParent(contenedor, false);
            RectTransform filaRT = (RectTransform)fila.transform;
            filaRT.anchorMin = new Vector2(0f, 1f);
            filaRT.anchorMax = new Vector2(1f, 1f);
            filaRT.pivot = new Vector2(0.5f, 1f);
            filaRT.sizeDelta = new Vector2(-2f * margen, alto);
            filaRT.anchoredPosition = new Vector2(0f, -(y + i * (alto + Espacio)));

            Image fondo = fila.AddComponent<Image>();
            fondo.material = materialFondo;
            fondo.color = ColorFondoNormal;
            fondo.raycastTarget = false;

            GameObject textoGO = new GameObject("Texto", typeof(RectTransform));
            textoGO.transform.SetParent(filaRT, false);
            RectTransform textoRT = (RectTransform)textoGO.transform;
            textoRT.anchorMin = Vector2.zero;
            textoRT.anchorMax = Vector2.one;
            textoRT.offsetMin = new Vector2(30f, 6f);
            textoRT.offsetMax = new Vector2(-30f, -6f);

            TextMeshProUGUI texto = textoGO.AddComponent<TextMeshProUGUI>();
            texto.font = fuente;
            texto.fontSharedMaterial = materialTexto;
            texto.text = opciones[i];
            texto.color = ColorTexto;
            texto.alignment = TextAlignmentOptions.Center;
            texto.enableAutoSizing = true; // si una opción es muy larga, se achica para caber
            texto.fontSizeMax = tamanoFuente;
            texto.fontSizeMin = tamanoFuente * 0.55f;
            texto.raycastTarget = false;

            OpcionMirable mirable = fila.AddComponent<OpcionMirable>();
            mirable.Configurar(fondo, ColorFondoNormal, ColorFondoMirada, texto, ColorTextoMirada,
                () => alElegir?.Invoke(indice));
        }
    }

    /// <summary>Lo pone delante de la mirada, de pie (sin inclinarse) y mirando al jugador.</summary>
    private void Colocar()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;

        Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
        if (adelante.sqrMagnitude < 0.0001f) adelante = cabeza.up; // mirando justo arriba/abajo
        adelante.Normalize();

        contenedor.SetPositionAndRotation(
            cabeza.position + adelante * Distancia + Vector3.down * BajarMetros,
            Quaternion.LookRotation(adelante, Vector3.up));
    }

    private void Limpiar()
    {
        if (contenedor == null) return;
        for (int i = contenedor.childCount - 1; i >= 0; i--)
        {
            Transform hijo = contenedor.GetChild(i);
            hijo.gameObject.SetActive(false); // sale de OpcionMirable.Activas al toque
            Destroy(hijo.gameObject);
        }
    }
}

using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso holográfico secundario que aparece en el BORDE SUPERIOR de la vista cada vez que se
/// responde una pregunta (correcta, incorrecta, error grave, IA) o el jugador se cae.
///
/// Es corto a propósito (solo ícono ✓ / ✕ + título), para leerlo de un vistazo:
/// - Flota arriba de la mirada (no tapa el panel de preguntas) y sigue la cabeza con retraso.
/// - Entra deslizándose con un "pop", el título se "decodifica" letra por letra (look hacker),
///   el ícono lanza ondas tipo radar y una barra abajo marca cuánto le queda.
///
/// Lo llama GameManager.RegistrarGastoComputacional (AvisoSistema.Mostrar). No hay que ponerlo
/// en la escena: se crea solo la primera vez.
/// </summary>
public class AvisoSistema : MonoBehaviour
{
    public enum Tipo { Correcto, Error, ErrorGrave, IA, Caida, Bloqueado, SinRetorno }

    // ----------------------- AJUSTES (edítalos aquí) -----------------------
    private const float Distancia = 1.6f;        // metros delante de los ojos
    private const float AnguloArriba = 21f;      // grados por encima del centro de la vista (borde superior)
    private const float AnchoMetros = 0.8f;      // ancho del aviso en el mundo
    private const float Suavizado = 5f;          // qué tan "perezoso" sigue la cabeza
    private const float Duracion = 10f;          // máximo en pantalla: normalmente se va antes, al aparecer la siguiente pregunta
    // ------------------------------------------------------------------------

    private const float Ancho = 900f;
    private const float Alto = 190f;

    private static readonly Color Verde = new Color(0.25f, 1f, 0.45f, 1f);
    private static readonly Color Naranja = new Color(1f, 0.55f, 0.15f, 1f);
    private static readonly Color Morado = new Color(0.7f, 0.5f, 1f, 1f);
    private const string CaracteresGlitch = "01#%&@$<>/\\[]{}=+*";

    private static AvisoSistema instancia;

    private RectTransform panel;
    private CanvasGroup grupo;
    private RectTransform contenido;
    private bool colocarDeGolpe = true;
    private Coroutine actual;
    private bool salirPronto;
    private float duracionActual = Duracion;
    private float minimoVisible; // la caída reaparece en la misma pregunta: que alcance a verse igual

    /// <summary>Muestra el aviso (si había otro, lo reemplaza al tiro).</summary>
    public static void Mostrar(Tipo tipo, string mensaje, float costoRefri, float litros)
    {
        if (instancia == null) instancia = new GameObject("[AvisoSistema]").AddComponent<AvisoSistema>();
        instancia.Lanzar(tipo, mensaje, costoRefri, litros);
    }

    /// <summary>Lo llama BaldosaPregunta al mostrar la siguiente pregunta: el aviso sale de inmediato.</summary>
    public static void OcultarYa()
    {
        if (instancia == null || instancia.panel == null) return;
        instancia.salirPronto = true; // Secuencia lo saca apenas cumpla su tiempo mínimo
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Lanzar(Tipo tipo, string mensaje, float costoRefri, float litros)
    {
        if (actual != null) StopCoroutine(actual);
        if (panel != null) Destroy(panel.gameObject);

        Construir(tipo, out TextMeshProUGUI titulo, out RectTransform barraVida, out RectTransform[] ondas, out RectTransform icono);
        colocarDeGolpe = true;
        salirPronto = false;
        minimoVisible = tipo == Tipo.Caida ? 2.5f : 0f;
        duracionActual = tipo == Tipo.SinRetorno || tipo == Tipo.Bloqueado ? 3.5f : Duracion; // avisos de guía: cortos
        actual = StartCoroutine(Secuencia(Titulo(tipo, mensaje), titulo, barraVida, ondas, icono));
    }

    // ------------------------------------------------------------------
    // SEGUIR LA CABEZA (en el borde superior de la vista)
    // ------------------------------------------------------------------

    void LateUpdate()
    {
        if (panel == null) return;
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;

        Vector3 direccion = Quaternion.AngleAxis(-AnguloArriba, cabeza.right) * cabeza.forward;
        Vector3 posicion = cabeza.position + direccion * Distancia;
        Quaternion rotacion = Quaternion.LookRotation(direccion, cabeza.up);

        if (colocarDeGolpe)
        {
            panel.SetPositionAndRotation(posicion, rotacion);
            colocarDeGolpe = false;
            return;
        }
        float t = 1f - Mathf.Exp(-Suavizado * Time.unscaledDeltaTime);
        panel.SetPositionAndRotation(Vector3.Lerp(panel.position, posicion, t), Quaternion.Slerp(panel.rotation, rotacion, t));
    }

    // ------------------------------------------------------------------
    // ANIMACIÓN
    // ------------------------------------------------------------------

    private IEnumerator Secuencia(string textoTitulo, TextMeshProUGUI titulo, RectTransform barraVida,
                                  RectTransform[] ondas, RectTransform icono)
    {
        float duracion = duracionActual;
        float anchoBarra = barraVida.sizeDelta.x;

        // Entrada: baja desde arriba con un pequeño rebote, mientras el título se decodifica.
        const float entrada = 0.45f;
        float t = 0f;
        while (t < duracion)
        {
            if (salirPronto && t >= minimoVisible) break;
            t += Time.unscaledDeltaTime;

            float e = Mathf.Clamp01(t / entrada);
            float rebote = EaseOutBack(e);
            grupo.alpha = Mathf.Clamp01(e * 1.6f);
            contenido.anchoredPosition = new Vector2(0f, Mathf.LerpUnclamped(110f, 0f, rebote));
            contenido.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, rebote);

            // Título "hackeado": las letras se van fijando de izquierda a derecha.
            float decodificado = Mathf.Clamp01((t - 0.1f) / 0.45f);
            titulo.text = Decodificar(textoTitulo, decodificado);

            // Ícono: late suave; ondas tipo radar que salen y se desvanecen.
            icono.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(t * 6f));
            for (int i = 0; i < ondas.Length; i++)
            {
                float fase = Mathf.Repeat(t * 0.9f - i * 0.5f, 1f);
                ondas[i].localScale = Vector3.one * Mathf.Lerp(1f, 1.9f, fase);
                ondas[i].GetComponent<Image>().color = WithAlpha(ondas[i].GetComponent<Image>().color, (1f - fase) * 0.6f);
            }

            // Barra de vida que se vacía hacia el centro.
            barraVida.sizeDelta = new Vector2(anchoBarra * (1f - Mathf.Clamp01(t / duracion)), barraVida.sizeDelta.y);
            yield return null;
        }

        yield return Salida();
    }

    /// <summary>Sube y se desvanece.</summary>
    private IEnumerator Salida()
    {
        float s = 0f;
        float alfaInicial = grupo.alpha;
        Vector2 desde = contenido.anchoredPosition;
        while (s < 0.35f)
        {
            s += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(s / 0.35f);
            grupo.alpha = alfaInicial * (1f - k);
            contenido.anchoredPosition = desde + new Vector2(0f, 70f * k * k);
            yield return null;
        }
        Destroy(panel.gameObject);
        panel = null;
        actual = null;
    }

    private static string Decodificar(string texto, float progreso)
    {
        int fijas = Mathf.FloorToInt(texto.Length * progreso);
        if (fijas >= texto.Length) return texto;
        StringBuilder sb = new StringBuilder(texto.Length);
        sb.Append(texto, 0, fijas);
        for (int i = fijas; i < texto.Length; i++)
            sb.Append(texto[i] == ' ' ? ' ' : CaracteresGlitch[Random.Range(0, CaracteresGlitch.Length)]);
        return sb.ToString();
    }

    private static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    private static Color WithAlpha(Color c, float a) { c.a = a; return c; }

    // ------------------------------------------------------------------
    // TEXTOS
    // ------------------------------------------------------------------

    private static string Titulo(Tipo tipo, string mensaje)
    {
        switch (tipo)
        {
            case Tipo.Correcto: return "PROCESAMIENTO CORRECTO";
            case Tipo.Error: return "RESPUESTA INCORRECTA";
            case Tipo.ErrorGrave: return "ERROR GRAVE";
            case Tipo.IA: return "RESPONDIÓ LA IA";
            case Tipo.Bloqueado: return "PRIMERO HABLA CON EL ROBOT";
            case Tipo.SinRetorno: return "NO HAY VUELTA ATRÁS";
            default:
                // "Caídas: 2/3" viene en el mensaje de BaldosaPregunta.
                System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(mensaje ?? "", @"(\d+)\s*/\s*(\d+)");
                return m.Success ? $"CAÍDA {m.Groups[1].Value}/{m.Groups[2].Value}" : "CAÍDA AL VACÍO";
        }
    }

    private static Color ColorDe(Tipo tipo)
    {
        switch (tipo)
        {
            case Tipo.Correcto: return Verde;
            case Tipo.Error: return Naranja;
            case Tipo.ErrorGrave: return EstiloUI.Rojo;
            case Tipo.IA: return EstiloUI.Magenta;
            case Tipo.Bloqueado: return EstiloUI.Cian;
            case Tipo.SinRetorno: return new Color(1f, 0.85f, 0.2f, 1f); // amarillo de "atención"
            default: return Morado;
        }
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN
    // ------------------------------------------------------------------

    private void Construir(Tipo tipo, out TextMeshProUGUI titulo, out RectTransform barraVida, out RectTransform[] ondas, out RectTransform icono)
    {
        Color acento = ColorDe(tipo);

        panel = EstiloUI.CrearCanvas("AvisoSistema", 1200, new Vector2(Ancho + 200f, Alto + 200f), AnchoMetros / Ancho);
        panel.SetParent(transform, false);
        grupo = panel.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        GameObject goContenido = new GameObject("Contenido", typeof(RectTransform));
        goContenido.transform.SetParent(panel, false);
        contenido = (RectTransform)goContenido.transform;
        contenido.sizeDelta = new Vector2(Ancho, Alto);

        // Halo de color detrás + fondo oscuro con líneas de escaneo.
        EstiloUI.CrearImagen(contenido, "Halo", Vector2.zero, new Vector2(Ancho + 40f, Alto + 40f), WithAlpha(acento, 0.1f));
        EstiloUI.CrearImagen(contenido, "Fondo", Vector2.zero, new Vector2(Ancho, Alto), new Color(0.01f, 0.03f, 0.06f, 0.94f));
        for (float y = -Alto * 0.5f + 24f; y < Alto * 0.5f; y += 24f)
            EstiloUI.CrearImagen(contenido, "Scan", new Vector2(0f, y), new Vector2(Ancho, 1f), WithAlpha(acento, 0.05f));
        EstiloUI.CrearBorde(contenido, Ancho, Alto, 2f, WithAlpha(acento, 0.45f));
        EstiloUI.CrearEsquinas(contenido, Ancho, Alto, 50f, 7f, acento);
        EstiloUI.CrearImagen(contenido, "Acento", new Vector2(-Ancho * 0.5f + 5f, 0f), new Vector2(10f, Alto), acento);

        // Ícono con ondas.
        Vector2 centroIcono = new Vector2(-Ancho * 0.5f + 110f, 0f);
        ondas = new RectTransform[2];
        for (int i = 0; i < ondas.Length; i++)
        {
            Image onda = EstiloUI.CrearImagen(contenido, "Onda", centroIcono, new Vector2(150f, 150f), WithAlpha(acento, 0f));
            onda.sprite = EstiloUI.Aro();
            ondas[i] = onda.rectTransform;
        }
        Image disco = EstiloUI.CrearImagen(contenido, "Icono", centroIcono, new Vector2(150f, 150f), WithAlpha(acento, 0.16f));
        disco.sprite = EstiloUI.Circulo();
        icono = disco.rectTransform;
        Image aro = EstiloUI.CrearImagen(icono, "Aro", Vector2.zero, new Vector2(150f, 150f), acento);
        aro.sprite = EstiloUI.Aro();
        DibujarSimbolo(icono, tipo, acento);

        // Solo el título, grande y centrado en el espacio que queda.
        float xTexto = -Ancho * 0.5f + 205f;
        float anchoTexto = Ancho - 235f;
        titulo = EstiloUI.CrearTexto(contenido, "", new Vector2(xTexto + anchoTexto * 0.5f, 0f), new Vector2(anchoTexto, 110f), 62f, acento, FontStyles.Bold);
        titulo.alignment = TextAlignmentOptions.Left;
        titulo.enableAutoSizing = true;
        titulo.fontSizeMax = 62f;
        titulo.fontSizeMin = 34f;
        titulo.characterSpacing = 3f;

        // Barra de vida (se vacía hacia el centro).
        EstiloUI.CrearImagen(contenido, "BarraFondo", new Vector2(0f, -Alto * 0.5f + 3f), new Vector2(Ancho, 6f), WithAlpha(acento, 0.15f));
        barraVida = EstiloUI.CrearImagen(contenido, "BarraVida", new Vector2(0f, -Alto * 0.5f + 3f), new Vector2(Ancho, 6f), acento).rectTransform;
    }

    /// <summary>Dibuja el símbolo con barritas (así no depende de que la fuente tenga ✓ o ⚠).</summary>
    private static void DibujarSimbolo(RectTransform padre, Tipo tipo, Color color)
    {
        const float g = 13f; // grosor
        switch (tipo)
        {
            case Tipo.Correcto: // ✓
                Barra(padre, new Vector2(-30f, 2f), new Vector2(-10f, -20f), g, color);
                Barra(padre, new Vector2(-10f, -20f), new Vector2(32f, 26f), g, color);
                break;
            case Tipo.Error: // ✕ (naranja)
            case Tipo.ErrorGrave: // ✕ (rojo)
                Barra(padre, new Vector2(-26f, 26f), new Vector2(26f, -26f), g, color);
                Barra(padre, new Vector2(-26f, -26f), new Vector2(26f, 26f), g, color);
                break;
            case Tipo.Bloqueado: // candado
                Image arco = EstiloUI.CrearImagen(padre, "Arco", new Vector2(0f, 14f), new Vector2(50f, 50f), color);
                arco.sprite = EstiloUI.Aro();
                EstiloUI.CrearImagen(padre, "Cuerpo", new Vector2(0f, -16f), new Vector2(64f, 46f), color);
                EstiloUI.CrearImagen(padre, "Ojo", new Vector2(0f, -14f), new Vector2(10f, 18f), new Color(0.02f, 0.05f, 0.08f, 1f));
                break;
            case Tipo.IA:
                EstiloUI.CrearTexto(padre, "IA", new Vector2(0f, 2f), new Vector2(120f, 80f), 58f, color, FontStyles.Bold);
                break;
            case Tipo.SinRetorno: // doble chevrón hacia ARRIBA = "sigue adelante"
                Barra(padre, new Vector2(-26f, -24f), new Vector2(0f, 2f), g, color);
                Barra(padre, new Vector2(0f, 2f), new Vector2(26f, -24f), g, color);
                Barra(padre, new Vector2(-26f, -2f), new Vector2(0f, 24f), g, WithAlpha(color, 0.55f));
                Barra(padre, new Vector2(0f, 24f), new Vector2(26f, -2f), g, WithAlpha(color, 0.55f));
                break;
            default: // doble chevrón hacia abajo
                Barra(padre, new Vector2(-26f, 24f), new Vector2(0f, -2f), g, color);
                Barra(padre, new Vector2(0f, -2f), new Vector2(26f, 24f), g, color);
                Barra(padre, new Vector2(-26f, 2f), new Vector2(0f, -24f), g, WithAlpha(color, 0.55f));
                Barra(padre, new Vector2(0f, -24f), new Vector2(26f, 2f), g, WithAlpha(color, 0.55f));
                break;
        }
    }

    private static void Barra(RectTransform padre, Vector2 a, Vector2 b, float grosor, Color color)
    {
        Vector2 d = b - a;
        Image img = EstiloUI.CrearImagen(padre, "Trazo", (a + b) * 0.5f, new Vector2(d.magnitude + grosor * 0.6f, grosor), color);
        img.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
    }
}

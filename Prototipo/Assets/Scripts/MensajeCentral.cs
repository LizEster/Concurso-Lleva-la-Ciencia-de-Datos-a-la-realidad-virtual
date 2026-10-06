using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Mensaje grande y centrado que se ve ENCIMA del negro (VeloNegro / intro), pegado a la vista,
/// con un ícono de joystick animado:
///   - JoystickBloqueado: la palanca intenta moverse, choca y vuelve (con una ✕ roja).
///     Se usa en la intro: "tu cuerpo aún no responde, solo puedes mirar".
///   - JoystickLibre: la palanca da vueltas libre (arriba, derecha, abajo, izquierda).
///     Se usa al terminar el diálogo con el robot: "ya puedes moverte".
/// El subtítulo se escribe letra por letra (acepta rich text: &lt;color&gt;, &lt;b&gt;...).
///
/// Uso desde una corrutina:
///     MensajeCentral m = MensajeCentral.Crear("TÍTULO", "subtítulo", MensajeCentral.Icono.JoystickLibre);
///     yield return m.Aparecer();
///     ...
///     yield return m.Desaparecer(1f);
/// </summary>
public class MensajeCentral : MonoBehaviour
{
    public enum Icono { Ninguno, JoystickBloqueado, JoystickLibre }

    private const float MetrosPorPx = 0.0012f;
    private const float Distancia = 1.6f;
    private const float SegundosPorLetra = 0.032f;

    private static readonly Color Cian = new Color(0.2f, 0.9f, 1f, 1f);
    private static readonly Color Rojo = new Color(1f, 0.3f, 0.3f, 1f);

    private RectTransform canvas;
    private TextMeshProUGUI textoTitulo;
    private string titulo;
    private Color colorTitulo = Cian;
    private bool glitchTitulo;
    private CanvasGroup grupo;
    private TextMeshProUGUI textoSubtitulo;
    private RectTransform perilla;
    private Icono icono;
    private float bajarMetros;

    /// <param name="colorTitulo">Color del título (por defecto cian).</param>
    /// <param name="glitchTitulo">Si es true, el título aparece "decodificándose" con caracteres raros.</param>
    public static MensajeCentral Crear(string titulo, string subtitulo, Icono icono, float bajarMetros = 0f,
                                       Color? colorTitulo = null, bool glitchTitulo = false)
    {
        MensajeCentral m = new GameObject("[MensajeCentral]").AddComponent<MensajeCentral>();
        m.icono = icono;
        m.bajarMetros = bajarMetros;
        m.titulo = titulo;
        if (colorTitulo.HasValue) m.colorTitulo = colorTitulo.Value;
        m.glitchTitulo = glitchTitulo;
        m.Construir(titulo, subtitulo);
        return m;
    }

    /// <summary>Opacidad de todo el mensaje (para fundirlo junto con otra cosa).</summary>
    public float Alfa
    {
        get => grupo != null ? grupo.alpha : 0f;
        set { if (grupo != null) grupo.alpha = value; }
    }

    /// <summary>Aparece y escribe el subtítulo letra por letra.</summary>
    public IEnumerator Aparecer(float duracionFundido = 0.5f)
    {
        yield return Animar(duracionFundido, t => grupo.alpha = t);

        if (glitchTitulo && textoTitulo != null && !string.IsNullOrEmpty(titulo))
        {
            const string raros = "01#%&@$<>/\\[]{}=+*";
            yield return Animar(0.7f, t =>
            {
                int fijas = Mathf.FloorToInt(titulo.Length * t);
                System.Text.StringBuilder sb = new System.Text.StringBuilder(titulo.Substring(0, fijas));
                for (int i = fijas; i < titulo.Length; i++) sb.Append(titulo[i] == ' ' ? ' ' : raros[Random.Range(0, raros.Length)]);
                if (textoTitulo != null) textoTitulo.text = sb.ToString();
            });
        }

        if (textoSubtitulo == null) yield break;
        textoSubtitulo.ForceMeshUpdate();
        int total = textoSubtitulo.textInfo.characterCount;
        for (int i = 0; i <= total; i++)
        {
            if (textoSubtitulo == null) yield break; // ya se destruyó (cambio de escena / Desaparecer)
            textoSubtitulo.maxVisibleCharacters = i;
            yield return new WaitForSeconds(SegundosPorLetra);
        }
    }

    /// <summary>Se desvanece y se destruye.</summary>
    public IEnumerator Desaparecer(float duracion)
    {
        float desde = Alfa;
        yield return Animar(duracion, t => Alfa = desde * (1f - t));
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------

    void OnEnable() { Application.onBeforeRender += Seguir; }
    void OnDisable() { Application.onBeforeRender -= Seguir; }

    void LateUpdate()
    {
        Seguir();
        AnimarJoystick();
    }

    private void Seguir()
    {
        if (canvas == null) return;
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;
        canvas.SetPositionAndRotation(cabeza.position + cabeza.forward * Distancia - cabeza.up * bajarMetros, cabeza.rotation);
    }

    private void AnimarJoystick()
    {
        if (perilla == null) return;
        float t = Time.unscaledTime;

        if (icono == Icono.JoystickLibre)
        {
            // Recorre arriba -> derecha -> abajo -> izquierda, con pausa breve en cada una.
            Vector2[] dirs = { Vector2.up, Vector2.right, Vector2.down, Vector2.left };
            float ciclo = t / 0.7f;
            int i = Mathf.FloorToInt(ciclo) % 4;
            float f = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((ciclo - Mathf.Floor(ciclo)) * 2.5f));
            Vector2 desde = dirs[(i + 3) % 4], hacia = dirs[i];
            perilla.anchoredPosition = Vector2.Lerp(desde, hacia, f) * 34f;
        }
        else if (icono == Icono.JoystickBloqueado)
        {
            // Intenta subir, "choca" (temblor) y vuelve al centro: no se puede mover.
            float c = Mathf.Repeat(t, 1.6f);
            float y = c < 0.25f ? Mathf.Lerp(0f, 12f, c / 0.25f)
                    : c < 0.55f ? 12f + Mathf.Sin(c * 80f) * 3f
                    : c < 0.75f ? Mathf.Lerp(12f, 0f, (c - 0.55f) / 0.2f) : 0f;
            perilla.anchoredPosition = new Vector2(0f, y);
        }
    }

    private void Construir(string titulo, string subtitulo)
    {
        canvas = EstiloUI.CrearCanvas("MensajeCentral", 1550, new Vector2(1300f, 560f), MetrosPorPx); // encima del velo negro (1400) y de la intro (1500)
        canvas.SetParent(transform, false);
        grupo = canvas.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        bool hayTitulo = !string.IsNullOrEmpty(titulo);

        if (icono != Icono.Ninguno)
        {
            Vector2 c = new Vector2(0f, hayTitulo ? 175f : 110f);
            Color colorIcono = icono == Icono.JoystickBloqueado ? new Color(1f, 1f, 1f, 0.75f) : Cian;

            Image halo = EstiloUI.CrearImagen(canvas, "Halo", c, new Vector2(200f, 200f), new Color(colorIcono.r, colorIcono.g, colorIcono.b, 0.08f));
            halo.sprite = EstiloUI.Circulo();
            Image base_ = EstiloUI.CrearImagen(canvas, "Base", c, new Vector2(140f, 140f), colorIcono);
            base_.sprite = EstiloUI.Aro();

            GameObject goPerilla = new GameObject("Perilla", typeof(RectTransform));
            goPerilla.transform.SetParent(canvas, false);
            RectTransform contenedor = (RectTransform)goPerilla.transform;
            contenedor.anchoredPosition = c;
            Image p = EstiloUI.CrearImagen(contenedor, "Bola", Vector2.zero, new Vector2(58f, 58f), colorIcono);
            p.sprite = EstiloUI.Circulo();
            perilla = p.rectTransform;

            if (icono == Icono.JoystickBloqueado)
            {
                // ✕ roja sobre el joystick.
                Vector2 badge = c + new Vector2(55f, -50f);
                Image fondo = EstiloUI.CrearImagen(canvas, "Badge", badge, new Vector2(58f, 58f), new Color(0.05f, 0.02f, 0.02f, 1f));
                fondo.sprite = EstiloUI.Circulo();
                Image aro = EstiloUI.CrearImagen(canvas, "BadgeAro", badge, new Vector2(58f, 58f), Rojo);
                aro.sprite = EstiloUI.Aro();
                for (int s = -1; s <= 1; s += 2)
                    EstiloUI.CrearImagen(canvas, "X", badge, new Vector2(34f, 7f), Rojo).rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f * s);
            }
        }

        if (hayTitulo)
        {
            textoTitulo = EstiloUI.CrearTexto(canvas, glitchTitulo ? "" : titulo, new Vector2(0f, 45f), new Vector2(1250f, 90f), 66f, colorTitulo, FontStyles.Bold);
            textoTitulo.characterSpacing = 6f;
        }

        if (!string.IsNullOrEmpty(subtitulo))
        {
            textoSubtitulo = EstiloUI.CrearTexto(canvas, subtitulo, new Vector2(0f, hayTitulo ? -85f : -50f), new Vector2(1200f, 170f), 40f,
                                                 new Color(1f, 1f, 1f, 0.9f), FontStyles.Normal);
            textoSubtitulo.enableWordWrapping = true;
            textoSubtitulo.alignment = TextAlignmentOptions.Top;
            textoSubtitulo.maxVisibleCharacters = 0;
        }

        Seguir();
    }

    private static IEnumerator Animar(float duracion, System.Action<float> aplicar)
    {
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            aplicar(Mathf.Clamp01(t / duracion));
            yield return null;
        }
        aplicar(1f);
    }
}

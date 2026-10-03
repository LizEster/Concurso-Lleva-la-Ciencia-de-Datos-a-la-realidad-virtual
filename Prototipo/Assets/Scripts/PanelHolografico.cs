using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Panel de preguntas de cada BaldosaPregunta, con el mismo estilo "visor futurista" del menú.
/// Se construye por código la primera vez que se muestra y aparece FLOTANDO DELANTE DEL
/// JUGADOR (a la altura de los ojos), con botones grandes y separados para que sea fácil
/// apuntarlos con la mirada. Cada opción se elige apuntándola con la mirada y apretando
/// A/B/X/Y (PunteroMirada + OpcionMirable); el panel le avisa a BaldosaPregunta qué opción se
/// eligió y BaldosaPregunta le pide resaltarla antes de resolver la respuesta de verdad.
/// El diseño viejo que quedó dentro de este objeto en la escena (Fondo, Textos, BotonHolograma)
/// se oculta solo y ya no se usa.
/// </summary>
public class PanelHolografico : MonoBehaviour
{
    [Header("Ubicación")]
    [Tooltip("Metros delante de los ojos del jugador donde aparece el panel.")]
    public float distancia = 2.2f;
    [Tooltip("Metros bajo la línea de los ojos (un poco más abajo es más cómodo para leer).")]
    public float bajarMetros = 0.1f;

    [Header("Textos")]
    public string textoEncabezado = "// CONSULTA DEL SISTEMA";
    public string textoBotonIA = "RESPONDER CON IA";

    [Header("Animación")]
    public float duracionFadeSimple = 0.25f;
    [Tooltip("Cuánto se queda iluminado el botón elegido antes de resolver la respuesta de verdad.")]
    public float duracionResaltado = 0.35f;

    [Header("Cronómetro")]
    [Tooltip("Desde cuántos segundos restantes el cronómetro se pone rojo.")]
    public float segundosUrgente = 10f;

    private const float Ancho = 1000f;
    private const float Alto = 860f;
    private const float MetrosPorPx = 0.0015f;

    private RectTransform panel;
    private CanvasGroup grupo;
    private Image fondo;
    private RectTransform lineaEscaneo;
    private TextMeshProUGUI textoEnunciado;
    private TextMeshProUGUI textoCronometro;
    private RectTransform barraCronometro;
    private Image imagenBarra;
    private GameObject grupoCronometro;
    private RectTransform contenedorOpciones;
    private OpcionMirable[] opcionesMirables = new OpcionMirable[0];

    private float tiempoTotal;
    private Coroutine corrutinaActual;

    void Awake()
    {
        // El diseño viejo del panel (en la escena) queda oculto: ahora todo se arma por código.
        foreach (Transform hijo in transform) hijo.gameObject.SetActive(false);
    }

    void OnDestroy()
    {
        if (panel != null) Destroy(panel.gameObject);
    }

    void Update()
    {
        if (panel == null || !panel.gameObject.activeSelf) return;

        // Toque de holograma: línea de escaneo que baja y un leve parpadeo del fondo.
        float y = Mathf.Repeat(Time.unscaledTime * 160f, Alto);
        lineaEscaneo.anchoredPosition = new Vector2(0f, Alto * 0.5f - y);

        Color c = EstiloUI.FondoPanel;
        c.a *= 0.94f + 0.06f * Mathf.PerlinNoise(Time.unscaledTime * 8f, 0f);
        fondo.color = c;
    }

    // ------------------------------------------------------------------
    // LO QUE USA BaldosaPregunta
    // ------------------------------------------------------------------

    /// <summary>
    /// Muestra la pregunta delante del jugador. 'alElegir' recibe el índice elegido:
    /// 0..2 = opciones, 3 = Responder con IA.
    /// </summary>
    public void Mostrar(string enunciado, string[] opciones, Action<int> alElegir)
    {
        // Este objeto (el panel viejo de la escena) tiene que estar activo para correr las
        // animaciones; su contenido viejo igual queda oculto (ver Awake).
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        if (panel == null) Construir();

        panel.gameObject.SetActive(true);
        EstiloUI.ColocarDelante(panel, distancia, bajarMetros);

        textoEnunciado.text = enunciado;
        tiempoTotal = 0f;
        grupoCronometro.SetActive(false); // aparece con la primera llamada a MostrarCronometro

        CrearOpciones(opciones, alElegir);

        grupo.alpha = 0f;
        ReiniciarCorrutina(FadeCorrutina(0f, 1f));
    }

    /// <summary>Llamado por BaldosaPregunta apenas se resuelve la respuesta (o se acaba el tiempo).</summary>
    public void Ocultar()
    {
        DeshabilitarOpciones(-1);
        if (panel == null || !panel.gameObject.activeSelf) return;
        ReiniciarCorrutina(FadeCorrutina(grupo.alpha, 0f, () => panel.gameObject.SetActive(false)));
    }

    /// <summary>Se mantiene por compatibilidad con Animation Events viejos.</summary>
    public void OcultarDeVerdad()
    {
        if (panel != null) panel.gameObject.SetActive(false);
    }

    /// <summary>
    /// BaldosaPregunta llama esto apenas se elige una opción: ilumina el botón elegido y,
    /// cuando termina ese resaltado, ejecuta 'alTerminar' (ahí BaldosaPregunta recién
    /// llama a EvaluarRespuesta o UsarIA).
    /// </summary>
    public void Resaltar(int indice, Action alTerminar)
    {
        DeshabilitarOpciones(indice); // mientras se resalta, no se puede elegir otra
        ReiniciarCorrutina(ResaltarCorrutina(indice, alTerminar));
    }

    /// <summary>BaldosaPregunta lo llama cada frame mientras corre el tiempo para responder.</summary>
    public void MostrarCronometro(float segundosRestantes)
    {
        if (panel == null) return;

        if (tiempoTotal <= 0f) tiempoTotal = Mathf.Max(0.01f, segundosRestantes);
        grupoCronometro.SetActive(true);

        int s = Mathf.Max(0, Mathf.CeilToInt(segundosRestantes));
        bool urgente = segundosRestantes <= segundosUrgente;
        Color color = urgente ? EstiloUI.Rojo : EstiloUI.Cian;

        textoCronometro.text = $"{s / 60}:{s % 60:00}";
        textoCronometro.color = color;

        float fraccion = Mathf.Clamp01(segundosRestantes / tiempoTotal);
        barraCronometro.sizeDelta = new Vector2((Ancho - 80f) * fraccion, barraCronometro.sizeDelta.y);
        imagenBarra.color = urgente ? new Color(1f, 0.3f, 0.3f, 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * 6f))) : color;
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN
    // ------------------------------------------------------------------

    private void Construir()
    {
        panel = EstiloUI.CrearCanvas("PanelPregunta (" + name + ")", 200, new Vector2(Ancho, Alto), MetrosPorPx);
        grupo = panel.gameObject.AddComponent<CanvasGroup>();

        fondo = EstiloUI.CrearImagen(panel, "Fondo", Vector2.zero, Vector2.zero, EstiloUI.FondoPanel);
        EstiloUI.Estirar(fondo.rectTransform);
        EstiloUI.CrearBorde(panel, Ancho, Alto, 3f, EstiloUI.CianSuave);
        EstiloUI.CrearEsquinas(panel, Ancho, Alto, 70f, 8f, EstiloUI.Cian);
        lineaEscaneo = EstiloUI.CrearImagen(panel, "Escaneo", Vector2.zero, new Vector2(Ancho, 4f), new Color(0.2f, 0.9f, 1f, 0.12f)).rectTransform;

        // Encabezado: etiqueta a la izquierda, cronómetro a la derecha.
        TextMeshProUGUI encabezado = EstiloUI.CrearTexto(panel, textoEncabezado, new Vector2(-170f, 375f), new Vector2(580f, 50f), 28f, new Color(0.2f, 0.9f, 1f, 0.7f), FontStyles.Normal);
        encabezado.alignment = TextAlignmentOptions.Left;

        grupoCronometro = new GameObject("Cronometro", typeof(RectTransform));
        grupoCronometro.transform.SetParent(panel, false);
        RectTransform crono = (RectTransform)grupoCronometro.transform;
        textoCronometro = EstiloUI.CrearTexto(crono, "0:30", new Vector2(330f, 375f), new Vector2(240f, 60f), 44f, EstiloUI.Cian, FontStyles.Bold);
        textoCronometro.alignment = TextAlignmentOptions.Right;

        // Barra de tiempo que se va achicando (de izquierda a derecha).
        EstiloUI.CrearImagen(crono, "BarraFondo", new Vector2(0f, 335f), new Vector2(Ancho - 80f, 8f), new Color(0.2f, 0.9f, 1f, 0.15f));
        imagenBarra = EstiloUI.CrearImagen(crono, "Barra", new Vector2(-(Ancho - 80f) * 0.5f, 335f), new Vector2(Ancho - 80f, 8f), EstiloUI.Cian);
        barraCronometro = imagenBarra.rectTransform;
        barraCronometro.pivot = new Vector2(0f, 0.5f);

        // Enunciado grande.
        textoEnunciado = EstiloUI.CrearTexto(panel, "", new Vector2(0f, 235f), new Vector2(Ancho - 80f, 150f), 60f, Color.white, FontStyles.Bold);
        textoEnunciado.enableWordWrapping = true;
        textoEnunciado.enableAutoSizing = true;
        textoEnunciado.fontSizeMax = 60f;
        textoEnunciado.fontSizeMin = 34f;

        GameObject contenedor = new GameObject("Opciones", typeof(RectTransform));
        contenedor.transform.SetParent(panel, false);
        contenedorOpciones = (RectTransform)contenedor.transform;
    }

    /// <summary>3 botones grandes, uno debajo del otro, y "Responder con IA" aparte, en magenta.</summary>
    private void CrearOpciones(string[] opciones, Action<int> alElegir)
    {
        for (int i = contenedorOpciones.childCount - 1; i >= 0; i--)
        {
            GameObject viejo = contenedorOpciones.GetChild(i).gameObject;
            viejo.SetActive(false); // sale de OpcionMirable.Activas al toque
            Destroy(viejo);
        }

        int cantidad = Mathf.Min(3, opciones != null ? opciones.Length : 0);
        opcionesMirables = new OpcionMirable[4];
        Vector2 tamano = new Vector2(Ancho - 100f, 108f);

        for (int i = 0; i < cantidad; i++)
        {
            int indice = i;
            opcionesMirables[i] = EstiloUI.CrearBoton(contenedorOpciones, opciones[i], new Vector2(0f, 80f - i * 125f), tamano, 46f,
                EstiloUI.BotonNormal, EstiloUI.BotonMirada, Color.white, EstiloUI.Cian, () => alElegir?.Invoke(indice));
        }

        opcionesMirables[3] = EstiloUI.CrearBoton(contenedorOpciones, textoBotonIA, new Vector2(0f, -325f), new Vector2(Ancho - 100f, 100f), 42f,
            EstiloUI.BotonIANormal, EstiloUI.BotonIAMirada, EstiloUI.Magenta, EstiloUI.Magenta, () => alElegir?.Invoke(3));
    }

    // ------------------------------------------------------------------
    // ANIMACIONES
    // ------------------------------------------------------------------

    private void DeshabilitarOpciones(int excepto)
    {
        for (int i = 0; i < opcionesMirables.Length; i++)
        {
            if (i == excepto || opcionesMirables[i] == null) continue;
            opcionesMirables[i].Deshabilitar();
        }
    }

    private void ReiniciarCorrutina(IEnumerator nueva)
    {
        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        corrutinaActual = StartCoroutine(nueva);
    }

    private IEnumerator ResaltarCorrutina(int indice, Action alTerminar)
    {
        if (indice >= 0 && indice < opcionesMirables.Length && opcionesMirables[indice] != null)
        {
            // Pequeño "pulso" del botón elegido (queda iluminado: OpcionMirable lo deja así al elegirse).
            Transform boton = opcionesMirables[indice].transform;
            Vector3 escala = boton.localScale;
            float t = 0f;
            while (t < duracionResaltado)
            {
                t += Time.deltaTime;
                float k = Mathf.Sin(Mathf.Clamp01(t / duracionResaltado) * Mathf.PI);
                boton.localScale = escala * (1f + 0.06f * k);
                yield return null;
            }
            boton.localScale = escala;
        }
        else
        {
            yield return new WaitForSeconds(duracionResaltado);
        }

        alTerminar?.Invoke();
    }

    private IEnumerator FadeCorrutina(float desde, float hasta, Action alTerminar = null)
    {
        float t = 0f;
        grupo.alpha = desde;

        while (t < duracionFadeSimple)
        {
            t += Time.deltaTime;
            grupo.alpha = Mathf.Lerp(desde, hasta, t / duracionFadeSimple);
            yield return null;
        }

        grupo.alpha = hasta;
        alTerminar?.Invoke();
    }
}

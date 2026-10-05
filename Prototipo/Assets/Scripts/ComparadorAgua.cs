using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Sensor de huella hídrica" de la escena final1: le da sentido a las esferas de agua.
///
/// FASE 1 - TU PARTIDA: un panel holográfico (celeste/azul, como las preguntas) explica que la
///   esfera es agua EVAPORADA para enfriar los servidores (no vuelve: menos es mejor). La esfera
///   crece decisión por decisión mientras se lista lo que gastó cada pregunta, se compara con
///   vasos de agua y con lo mínimo/máximo que se podía gastar, y se da un veredicto claro.
///   Termina con el botón SIGUIENTE.
/// FASE 2 - A ESCALA REAL: aparece la esfera gigante y, mientras se expande, el contador sube
///   desde tus litros hasta lo que evaporó entrenar UN modelo grande (~700.000 L), traducido a
///   vasos de agua y años de agua para una persona. Termina con "VER TU DESEMPEÑO".
///
/// Lo crea FinalManager al acercarse a la esfera (ComparadorAgua.Iniciar).
/// </summary>
public class ComparadorAgua : MonoBehaviour
{
    // ----------------------- DATOS (edítalos aquí) -----------------------
    private const float LitrosEntrenamiento = 700000f; // GPT-3, estimación de Li et al. (2023), "Making AI Less Thirsty"
    private const string FuenteDato = "Estimación de Li et al., 2023 (\"Making AI Less Thirsty\").";
    private const float LitrosPorVaso = 0.25f;
    private const float LitrosPorPersonaAlDia = 2f;
    // ----------------------------------------------------------------------

    private const float Ancho = 1000f;
    private const float Alto = 1340f;
    private const float MetrosPorPx = 0.0013f;

    private static readonly Color Azul = new Color(0.25f, 0.55f, 1f, 1f);
    private static readonly Color AzulSuave = new Color(0.25f, 0.55f, 1f, 0.35f);

    private Transform esfera;
    private Transform gigante;
    private float tamanoGigante;
    private Action iniciarGigante;
    private Action alTerminar;
    private RectTransform zonaBotones;
    private bool botonElegido;

    private RectTransform panel;
    private CanvasGroup grupo;
    private RectTransform lineaEscaneo;
    private TextMeshProUGUI textoTitulo, textoSubtitulo, textoContador, textoEquivalencia, textoLineas, textoVeredicto;
    private GameObject bloqueBarra;
    private RectTransform barraRelleno, marcador;
    private TextMeshProUGUI etiquetaMin, etiquetaMax;

    private float litrosJugador;
    private bool faseGigante;
    private float escalaInicialGigante = 1f;
    private Vector3 escalaBaseEsfera;

    /// <summary>La esfera pequeña arranca (desde que carga la escena) a este porcentaje de su tamaño real.</summary>
    public const float FactorInicial = 0.15f;

    /// <summary>
    /// 'alTerminar' se llama al elegir "VER TU DESEMPEÑO" (FinalManager pasa al informe final).
    /// 'escalaBaseEsfera' es el tamaño REAL de la esfera pequeña (antes de achicarla a FactorInicial).
    /// </summary>
    public static ComparadorAgua Iniciar(Transform esfera, Transform gigante, float tamanoGigante, Action iniciarGigante, Action alTerminar, Vector3 escalaBaseEsfera = default)
    {
        ComparadorAgua c = new GameObject("[ComparadorAgua]").AddComponent<ComparadorAgua>();
        c.escalaBaseEsfera = escalaBaseEsfera;
        c.esfera = esfera;
        c.gigante = gigante;
        c.tamanoGigante = tamanoGigante;
        c.iniciarGigante = iniciarGigante;
        c.alTerminar = alTerminar;
        return c;
    }

    /// <summary>Lo llama FinalManager al pasar al negro final.</summary>
    public void Ocultar()
    {
        StopAllCoroutines();
        StartCoroutine(OcultarCorrutina());
    }

    void Start()
    {
        litrosJugador = DatosFinales.aguaConsumida;
        Construir();
        Colocar();
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (panel == null) return;

        float y = Mathf.Repeat(Time.unscaledTime * 170f, Alto);
        lineaEscaneo.anchoredPosition = new Vector2(0f, Alto * 0.5f - y);

        // Fase 2: el contador sube al ritmo con que crece la esfera gigante (en escala
        // logarítmica: así se "siente" el salto de litros a cientos de miles).
        if (faseGigante && gigante != null)
        {
            float s = Mathf.InverseLerp(escalaInicialGigante, tamanoGigante * 0.95f, gigante.localScale.x);
            float desde = Mathf.Max(0.01f, litrosJugador);
            float litros = desde * Mathf.Pow(LitrosEntrenamiento / desde, Mathf.Clamp01(s));
            textoContador.text = FormatoLitros(litros);
        }
    }

    // ------------------------------------------------------------------
    // SECUENCIA
    // ------------------------------------------------------------------

    private IEnumerator Secuencia()
    {
        // ---------- FASE 1: TU PARTIDA ----------
        List<DatosFinales.Decision> decisiones = DatosFinales.decisiones;

        float minimo = 0f, maximo = 0f, aguaDecisiones = 0f;
        foreach (DatosFinales.Decision d in decisiones)
        {
            minimo += d.CostoAgua(d.correcta);
            float peor = DatosFinales.CostoIAAgua;
            for (int op = 0; op < d.costoAgua.Length; op++) peor = Mathf.Max(peor, d.costoAgua[op]);
            maximo += peor;
            aguaDecisiones += d.CostoAgua(d.elegida);
        }
        float aguaCaidas = Mathf.Max(0f, litrosJugador - aguaDecisiones);
        float posicion = maximo > minimo ? Mathf.Clamp01((litrosJugador - minimo) / (maximo - minimo)) : 0f;

        // La esfera empieza chiquita y crece hasta un tamaño que depende de cuánto gastaste.
        Vector3 escalaBase = escalaBaseEsfera.sqrMagnitude > 0.0001f ? escalaBaseEsfera : (esfera != null ? esfera.localScale : Vector3.one);
        float escalaFinal = Mathf.Lerp(0.6f, 1.8f, posicion);
        if (esfera != null) esfera.localScale = escalaBase * FactorInicial;

        yield return Animar(0.6f, t => grupo.alpha = t);

        float acumulado = 0f;
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        for (int i = 0; i < decisiones.Count; i++)
        {
            DatosFinales.Decision d = decisiones[i];
            bool ia = d.elegida == DatosFinales.OpcionIA;
            bool acerto = d.elegida == d.correcta;
            string tipo = ia ? "<color=#FF66BF>con IA</color>" : acerto ? "<color=#40FF73>correcta</color>" : "<color=#FF8C26>incorrecta</color>";
            sb.Append($"P{i + 1} · {tipo}<pos=74%>+{d.CostoAgua(d.elegida):0.00} L\n");
            yield return SumarLinea(sb.ToString(), acumulado, acumulado + d.CostoAgua(d.elegida), escalaBase, escalaFinal);
            acumulado += d.CostoAgua(d.elegida);
        }
        if (aguaCaidas > 0.01f)
        {
            sb.Append($"Caídas x{DatosFinales.caidas} <color=#9FB7FF>(reconstruir el camino)</color><pos=74%>+{aguaCaidas:0.00} L\n");
            yield return SumarLinea(sb.ToString(), acumulado, litrosJugador, escalaBase, escalaFinal);
        }
        textoContador.text = FormatoLitros(litrosJugador);
        if (esfera != null) esfera.localScale = escalaBase * escalaFinal;

        // Vasos de agua.
        float vasos = litrosJugador / LitrosPorVaso;
        textoEquivalencia.text = vasos < 1f ? "menos de un vaso de agua" : $"~{vasos:0} vasos de agua de 250 ml";
        yield return Animar(0.4f, t => textoEquivalencia.alpha = t);

        // Barra: dónde quedaste entre lo mínimo y lo máximo que se podía gastar.
        if (decisiones.Count > 0)
        {
            etiquetaMin.text = $"MÍNIMO POSIBLE\n<b>{minimo:0.00} L</b>";
            etiquetaMax.text = $"MÁXIMO\n<b>{maximo:0.00} L</b>";
            bloqueBarra.SetActive(true);
            float anchoBarra = Ancho - 140f;
            yield return Animar(1.2f, t =>
            {
                float k = Mathf.SmoothStep(0f, 1f, t) * posicion;
                barraRelleno.sizeDelta = new Vector2(anchoBarra * k, barraRelleno.sizeDelta.y);
                marcador.anchoredPosition = new Vector2(-anchoBarra * 0.5f + anchoBarra * k, marcador.anchoredPosition.y);
            });

            textoVeredicto.text = posicion <= 0.2f
                ? "<color=#40FF73>Muy bien:</color> gastaste casi lo mínimo posible. Pensar por tu cuenta ahorró agua."
                : posicion <= 0.55f
                    ? "<color=#FFD933>Bien, pero</color> parte de esta agua se podía evitar."
                    : "<color=#FF8C26>Alto:</color> la mayor parte de esta agua era evitable (errores y respuestas de la IA).";
            yield return Animar(0.5f, t => textoVeredicto.alpha = t);
        }

        yield return EsperarBoton("SIGUIENTE");

        // ---------- FASE 2: A ESCALA REAL ----------
        yield return Animar(0.4f, t => { textoLineas.alpha = 1f - t; textoVeredicto.alpha = 1f - t; textoEquivalencia.alpha = 1f - t; });
        bloqueBarra.SetActive(false);
        textoLineas.text = "";
        textoLineas.alpha = 1f;

        textoTitulo.text = "AHORA, A ESCALA REAL";
        textoSubtitulo.text = $"Tu partida evaporó <b>{FormatoLitros(litrosJugador)}</b>.\n" +
                              "Entrenar <b>UN</b> modelo de lenguaje como GPT-3 evaporó cerca de <b>700.000 litros</b> de agua dulce.\n" +
                              $"<size=75%><color=#9FB7FF>{FuenteDato}</color></size>";
        textoVeredicto.text = "";
        textoEquivalencia.text = "";
        yield return new WaitForSeconds(3f);

        if (gigante != null) escalaInicialGigante = Mathf.Max(0.01f, gigante.localScale.x);
        iniciarGigante?.Invoke();
        faseGigante = true;

        float espera = 0f;
        while (gigante != null && gigante.localScale.x < tamanoGigante * 0.95f && espera < 20f)
        {
            espera += Time.deltaTime;
            yield return null;
        }
        faseGigante = false;
        textoContador.text = FormatoLitros(LitrosEntrenamiento);

        float veces = LitrosEntrenamiento / Mathf.Max(0.01f, litrosJugador);
        textoEquivalencia.text = $"~{veces:N0} veces tu partida";
        yield return Animar(0.4f, t => textoEquivalencia.alpha = t);

        // El recuadro de abajo traduce los 700.000 L a cosas que se pueden imaginar.
        float vasosEntrenamiento = LitrosEntrenamiento / LitrosPorVaso;
        float anosBebiendo = LitrosEntrenamiento / (LitrosPorPersonaAlDia * 365f);
        textoLineas.text = $"<color=#33E6FF><b>{FormatoLitros(LitrosEntrenamiento)}</b> equivalen a...</color>\n\n" +
                           $"- unos <b>{vasosEntrenamiento:N0} vasos</b> de agua de 250 ml\n" +
                           $"- el agua que toma <b>una persona en ~{anosBebiendo:N0} años</b>\n   <size=80%><color=#9FB7FF>(2 litros al día)</color></size>\n\n" +
                           $"<color=#33E6FF>Tu partida:</color> <b>{FormatoLitros(litrosJugador)}</b> = el agua que una persona toma en <b>{TiempoDeAgua(litrosJugador)}</b>.";
        yield return Animar(0.6f, t => textoLineas.alpha = t);

        textoVeredicto.text = "Tu partida fue una gota. Pero millones de personas consultan una IA cada día:\n<b>cada consulta que evitas o haces mejor, cuenta.</b>";
        yield return Animar(0.6f, t => textoVeredicto.alpha = t);

        yield return EsperarBoton("VER TU DESEMPEÑO");
        alTerminar?.Invoke();
    }

    /// <summary>Muestra un botón (mirada + A/B/X/Y) y espera a que lo elijan.</summary>
    private IEnumerator EsperarBoton(string texto)
    {
        botonElegido = false;
        EstiloUI.CrearBoton(zonaBotones, texto, Vector2.zero, new Vector2(560f, 96f), 42f,
            EstiloUI.BotonNormal, EstiloUI.BotonMirada, EstiloUI.Cian, EstiloUI.Cian, () => botonElegido = true);
        while (!botonElegido) yield return null;

        for (int i = zonaBotones.childCount - 1; i >= 0; i--)
        {
            GameObject boton = zonaBotones.GetChild(i).gameObject;
            boton.SetActive(false);
            Destroy(boton);
        }
    }

    private IEnumerator SumarLinea(string lineas, float desde, float hasta, Vector3 escalaBase, float escalaFinal)
    {
        textoLineas.text = lineas;
        yield return Animar(0.7f, t =>
        {
            float litros = Mathf.Lerp(desde, hasta, t);
            textoContador.text = FormatoLitros(litros);
            if (esfera != null && litrosJugador > 0f)
                esfera.localScale = escalaBase * Mathf.Lerp(FactorInicial, escalaFinal, Mathf.Clamp01(litros / litrosJugador));
        });
        yield return new WaitForSeconds(0.25f);
    }

    private IEnumerator OcultarCorrutina()
    {
        float desde = grupo != null ? grupo.alpha : 0f;
        yield return Animar(0.6f, t => { if (grupo != null) grupo.alpha = desde * (1f - t); });
        Destroy(gameObject);
    }

    /// <summary>Cuánto tarda una persona en tomarse esa agua (2 litros al día), en la unidad que se entienda mejor.</summary>
    private static string TiempoDeAgua(float litros)
    {
        float dias = litros / LitrosPorPersonaAlDia;
        if (dias < 1f)
        {
            int horas = Mathf.RoundToInt(dias * 24f);
            if (horas < 1) return "menos de 1 hora";
            return horas == 1 ? "1 hora" : $"{horas} horas";
        }
        if (dias < 60f)
        {
            int d = Mathf.RoundToInt(dias);
            return d == 1 ? "1 día" : $"{d} días";
        }
        if (dias < 730f) return $"{Mathf.RoundToInt(dias / 30f)} meses";
        int a = Mathf.RoundToInt(dias / 365f);
        return a == 1 ? "1 año" : $"{a} años";
    }

    private static string FormatoLitros(float litros)
    {
        return litros < 100f ? $"{litros:0.00} L" : $"{litros:N0} L";
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN
    // ------------------------------------------------------------------

    /// <summary>Al costado de la esfera (a la derecha según hacia dónde está el jugador), mirándolo.</summary>
    private void Colocar()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null) return;

        Vector3 haciaEsfera = esfera != null ? esfera.position - cabeza.position : cabeza.forward;
        haciaEsfera = Vector3.ProjectOnPlane(haciaEsfera, Vector3.up);
        if (haciaEsfera.sqrMagnitude < 0.0001f) haciaEsfera = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
        haciaEsfera.Normalize();
        Vector3 derecha = Vector3.Cross(Vector3.up, haciaEsfera);

        Vector3 posicion = cabeza.position + haciaEsfera * 1.9f + derecha * 1.0f + Vector3.down * 0.1f;
        Vector3 mirada = Vector3.ProjectOnPlane(posicion - cabeza.position, Vector3.up).normalized;
        panel.SetPositionAndRotation(posicion, Quaternion.LookRotation(mirada, Vector3.up));
    }

    private void Construir()
    {
        panel = EstiloUI.CrearCanvas("SensorAgua", 1500, new Vector2(Ancho, Alto), MetrosPorPx);
        panel.SetParent(transform, false);
        grupo = panel.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        Image fondo = EstiloUI.CrearImagen(panel, "Fondo", Vector2.zero, Vector2.zero, new Color(0.01f, 0.03f, 0.08f, 0.93f));
        EstiloUI.Estirar(fondo.rectTransform);
        // "Olas" de fondo: líneas horizontales tenues.
        for (float y = -Alto * 0.5f + 60f; y < Alto * 0.5f; y += 60f)
            EstiloUI.CrearImagen(panel, "Onda", new Vector2(0f, y), new Vector2(Ancho, 1f), new Color(0.25f, 0.55f, 1f, 0.05f));
        EstiloUI.CrearBorde(panel, Ancho, Alto, 3f, AzulSuave);
        EstiloUI.CrearEsquinas(panel, Ancho, Alto, 70f, 8f, EstiloUI.Cian);
        lineaEscaneo = EstiloUI.CrearImagen(panel, "Escaneo", Vector2.zero, new Vector2(Ancho, 4f), new Color(0.2f, 0.9f, 1f, 0.12f)).rectTransform;

        TextMeshProUGUI etiqueta = EstiloUI.CrearTexto(panel, "// SENSOR DE AGUA EVAPORADA", new Vector2(-150f, 625f), new Vector2(640f, 44f), 26f, new Color(0.2f, 0.9f, 1f, 0.7f), FontStyles.Normal);
        etiqueta.alignment = TextAlignmentOptions.Left;
        Image gota = EstiloUI.CrearImagen(panel, "Gota", new Vector2(430f, 625f), new Vector2(26f, 26f), EstiloUI.Cian);
        gota.sprite = EstiloUI.Circulo();

        textoTitulo = EstiloUI.CrearTexto(panel, "TU HUELLA HÍDRICA", new Vector2(0f, 555f), new Vector2(Ancho - 60f, 80f), 60f, EstiloUI.Cian, FontStyles.Bold);
        textoTitulo.characterSpacing = 5f;

        textoSubtitulo = EstiloUI.CrearTexto(panel,
            "Esta esfera es el agua que se <b>EVAPORÓ</b> para enfriar los servidores mientras respondías. No vuelve: <b>menos es mejor</b>.",
            new Vector2(0f, 440f), new Vector2(Ancho - 90f, 150f), 31f, new Color(1f, 1f, 1f, 0.85f), FontStyles.Normal);
        textoSubtitulo.enableWordWrapping = true;
        textoSubtitulo.enableAutoSizing = true;
        textoSubtitulo.fontSizeMax = 31f;
        textoSubtitulo.fontSizeMin = 20f;

        textoContador = EstiloUI.CrearTexto(panel, "0.00 L", new Vector2(0f, 300f), new Vector2(Ancho - 60f, 130f), 112f, Color.white, FontStyles.Bold);
        textoEquivalencia = EstiloUI.CrearTexto(panel, "", new Vector2(0f, 208f), new Vector2(Ancho - 60f, 50f), 34f, EstiloUI.Cian, FontStyles.Normal);
        textoEquivalencia.alpha = 0f;

        Image cajaLineas = EstiloUI.CrearImagen(panel, "CajaLineas", new Vector2(0f, 10f), new Vector2(Ancho - 100f, 290f), new Color(0f, 0f, 0f, 0.3f));
        EstiloUI.CrearImagen(cajaLineas.rectTransform, "Acento", new Vector2(-(Ancho - 100f) * 0.5f, 0f), new Vector2(6f, 290f), Azul);
        textoLineas = EstiloUI.CrearTexto(cajaLineas.rectTransform, "", Vector2.zero, new Vector2(Ancho - 160f, 270f), 32f, Color.white, FontStyles.Normal);
        textoLineas.alignment = TextAlignmentOptions.TopLeft;
        textoLineas.enableWordWrapping = false;
        textoLineas.enableAutoSizing = true;
        textoLineas.fontSizeMax = 32f;
        textoLineas.fontSizeMin = 20f;

        // Barra mínimo -> máximo.
        bloqueBarra = new GameObject("Barra", typeof(RectTransform));
        bloqueBarra.transform.SetParent(panel, false);
        RectTransform barra = (RectTransform)bloqueBarra.transform;
        float anchoBarra = Ancho - 140f;
        etiquetaMin = EstiloUI.CrearTexto(barra, "", new Vector2(-anchoBarra * 0.5f + 150f, -305f), new Vector2(300f, 70f), 24f, new Color(0.4f, 1f, 0.55f, 0.9f), FontStyles.Normal);
        etiquetaMin.alignment = TextAlignmentOptions.Left;
        etiquetaMax = EstiloUI.CrearTexto(barra, "", new Vector2(anchoBarra * 0.5f - 150f, -305f), new Vector2(300f, 70f), 24f, new Color(1f, 0.55f, 0.15f, 0.9f), FontStyles.Normal);
        etiquetaMax.alignment = TextAlignmentOptions.Right;
        EstiloUI.CrearImagen(barra, "BarraFondo", new Vector2(0f, -250f), new Vector2(anchoBarra, 16f), AzulSuave);
        Image relleno = EstiloUI.CrearImagen(barra, "BarraRelleno", new Vector2(-anchoBarra * 0.5f, -250f), new Vector2(0f, 16f), EstiloUI.Cian);
        barraRelleno = relleno.rectTransform;
        barraRelleno.pivot = new Vector2(0f, 0.5f);
        GameObject marcadorGO = new GameObject("Marcador", typeof(RectTransform));
        marcadorGO.transform.SetParent(barra, false);
        marcador = (RectTransform)marcadorGO.transform;
        marcador.anchoredPosition = new Vector2(-anchoBarra * 0.5f, -250f);
        Image rombo = EstiloUI.CrearImagen(marcador, "Rombo", Vector2.zero, new Vector2(30f, 30f), Color.white);
        rombo.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        EstiloUI.CrearTexto(marcador, "TÚ", new Vector2(0f, 38f), new Vector2(80f, 34f), 24f, Color.white, FontStyles.Bold);
        bloqueBarra.SetActive(false);

        textoVeredicto = EstiloUI.CrearTexto(panel, "", new Vector2(0f, -425f), new Vector2(Ancho - 90f, 150f), 34f, Color.white, FontStyles.Normal);
        textoVeredicto.enableWordWrapping = true;
        textoVeredicto.enableAutoSizing = true;
        textoVeredicto.fontSizeMax = 34f;
        textoVeredicto.fontSizeMin = 22f;
        textoVeredicto.alpha = 0f;

        GameObject zona = new GameObject("Botones", typeof(RectTransform));
        zona.transform.SetParent(panel, false);
        zonaBotones = (RectTransform)zona.transform;
        zonaBotones.anchoredPosition = new Vector2(0f, -585f);
    }

    private static IEnumerator Animar(float duracion, Action<float> aplicar)
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

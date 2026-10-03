using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Mapa de decisiones" que aparece después del informe final (escena final1).
/// Es CURVO: cada columna (INICIO, P1..Pn, TU FINAL) es una faceta puesta en un arco que
/// envuelve al jugador, así cada pregunta queda de frente al girar la cabeza. Muestra:
///   - TU CAMINO, brillando: INICIO -> P1 -> P2 -> ... -> TU FINAL.
///   - En cada pregunta, una rama por cada opción que NO elegiste, terminando en el final al
///     que te habría llevado cambiar SÓLO esa decisión (verde óptimo, amarillo moderado,
///     naranja crítico, rojo colapso).
///   - El aro de cada pregunta dice cómo respondiste: verde acierto, rojo error, magenta IA.
/// Al apuntar con la mirada a un nodo, el cuadro de detalle se desliza por el arco hasta
/// quedar justo encima de esa columna, de frente. Se dibuja animado.
/// Usa lo que BaldosaPregunta fue guardando en DatosFinales durante la partida.
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    public enum Final { Optimo, Moderado, Critico, Colapso, Caidas }

    private const float MetrosPorPx = 0.002f;
    private const float Radio = 3.2f;           // metros del jugador al arco
    private const float PasoGrados = 26f;       // ángulo entre columnas
    private const float AltoFaceta = 1100f;     // px
    private const float YCamino = 130f;         // px: altura del camino dentro de cada faceta
    private const float BajarCentro = 0.15f;    // el arco va un poco bajo la línea de los ojos
    private const float YDetalle = 410f;        // px: el detalle va arriba del camino
    private const string EscenaJuego = "Nivel_Principal";

    private static readonly Color ColorAcierto = new Color(0.25f, 1f, 0.45f, 1f);
    private static readonly Color ColorError = new Color(1f, 0.3f, 0.3f, 1f);
    private static readonly Color ColorIA = new Color(1f, 0.4f, 0.75f, 1f);
    private static readonly Color FondoNodo = new Color(0.02f, 0.04f, 0.07f, 1f);

    public static Color ColorDe(Final f)
    {
        switch (f)
        {
            case Final.Optimo: return new Color(0.25f, 1f, 0.45f, 1f);
            case Final.Moderado: return new Color(1f, 0.85f, 0.2f, 1f);
            case Final.Critico: return new Color(1f, 0.55f, 0.15f, 1f);
            case Final.Colapso: return new Color(1f, 0.3f, 0.3f, 1f);
            default: return new Color(0.75f, 0.5f, 1f, 1f);
        }
    }

    public static string NombreDe(Final f)
    {
        switch (f)
        {
            case Final.Optimo: return "ÓPTIMO";
            case Final.Moderado: return "MODERADO";
            case Final.Critico: return "CRÍTICO";
            case Final.Colapso: return "COLAPSO";
            default: return "CAÍDAS";
        }
    }

    public static Final FinalPorRefrigeracion(float r)
    {
        if (r >= 60f) return Final.Optimo;
        if (r >= 25f) return Final.Moderado;
        if (r > 0f) return Final.Critico;
        return Final.Colapso;
    }

    public static Final FinalReal()
    {
        if (DatosFinales.colapsoTermico) return Final.Colapso;
        if (DatosFinales.demasiadasCaidas) return Final.Caidas;
        return FinalPorRefrigeracion(DatosFinales.refrigeracionRestante);
    }

    public static void Mostrar()
    {
        new GameObject("[MapaDecisiones]").AddComponent<MapaDecisiones>();
    }

    // ------------------------------------------------------------------

    private class Nodo
    {
        public RectTransform raiz;
        public Image disco;
        public TextMeshProUGUI etiqueta;
        public Color color;
        public string info;
        public Columna columna;
    }

    private class Linea
    {
        public RectTransform rt;
        public float largo;
    }

    private class Columna
    {
        public RectTransform canvas;
        public float angulo;
        public float ancho;
        public Nodo nodo;
        public Linea izquierda, derecha;
        public RectTransform escaneo;
        public readonly List<Linea> ramas = new List<Linea>();
        public readonly List<Nodo> nodosRamas = new List<Nodo>();
    }

    private Vector3 centro;
    private Vector3 adelante;
    private readonly List<Columna> columnas = new List<Columna>();
    private readonly List<CanvasGroup> grupos = new List<CanvasGroup>();

    private RectTransform canvasDetalle;
    private TextMeshProUGUI textoInfo;
    private float anguloDetalle;
    private float anguloDetalleObjetivo;
    private RectTransform zonaBotones;
    private Nodo nodoFinal;

    void Start()
    {
        // El arco se arma alrededor de donde está el jugador, centrado hacia donde mira.
        Transform cabeza = PunteroMirada.Cabeza();
        centro = cabeza != null ? cabeza.position + Vector3.down * BajarCentro : Vector3.zero;
        adelante = cabeza != null ? Vector3.ProjectOnPlane(cabeza.forward, Vector3.up) : Vector3.forward;
        if (adelante.sqrMagnitude < 0.0001f) adelante = Vector3.forward;
        adelante.Normalize();

        Construir();
        StartCoroutine(Aparecer());
    }

    void Update()
    {
        // Línea de escaneo, sincronizada en todas las facetas (se ve continua).
        float y = Mathf.Repeat(Time.unscaledTime * 220f, AltoFaceta);
        foreach (Columna c in columnas)
            if (c.escaneo != null) c.escaneo.anchoredPosition = new Vector2(0f, AltoFaceta * 0.5f - y);

        // El nodo de tu final late.
        if (nodoFinal != null && nodoFinal.disco != null)
            nodoFinal.disco.rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 3f));

        // El detalle se desliza por el arco hasta la columna que estás mirando.
        anguloDetalle = Mathf.LerpAngle(anguloDetalle, anguloDetalleObjetivo, 1f - Mathf.Exp(-6f * Time.unscaledDeltaTime));
        Colocar(canvasDetalle, anguloDetalle, YDetalle * MetrosPorPx, Radio - 0.15f);
    }

    // ------------------------------------------------------------------
    // GEOMETRÍA DEL ARCO
    // ------------------------------------------------------------------

    /// <summary>Pone un canvas sobre el arco: a 'angulo' grados de la mirada inicial, a 'altura' metros y mirando al jugador.</summary>
    private void Colocar(RectTransform canvas, float angulo, float altura, float radio)
    {
        Vector3 direccion = Quaternion.Euler(0f, angulo, 0f) * adelante;
        canvas.SetPositionAndRotation(centro + direccion * radio + Vector3.up * altura,
                                      Quaternion.LookRotation(direccion, Vector3.up));
    }

    private RectTransform CrearCanvasEnArco(string nombre, int orden, Vector2 tamano, float angulo, float altura, float radio)
    {
        RectTransform canvas = EstiloUI.CrearCanvas(nombre, orden, tamano, MetrosPorPx);
        canvas.SetParent(transform, false);
        Colocar(canvas, angulo, altura, radio);

        CanvasGroup grupo = canvas.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupos.Add(grupo);
        return canvas;
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN
    // ------------------------------------------------------------------

    private void Construir()
    {
        List<DatosFinales.Decision> decisiones = DatosFinales.decisiones;
        int n = decisiones.Count;
        int cantidad = n + 2; // INICIO + preguntas + TU FINAL

        // Ancho de cada faceta = la cuerda entre dos columnas vecinas: así se tocan en los bordes.
        float ancho = 2f * Radio * Mathf.Sin(PasoGrados * 0.5f * Mathf.Deg2Rad) / MetrosPorPx;

        for (int j = 0; j < cantidad; j++)
        {
            float angulo = (j - (cantidad - 1) * 0.5f) * PasoGrados;
            columnas.Add(CrearFaceta(j, angulo, ancho, j == 0, j == cantidad - 1));
        }

        ConstruirArbol(decisiones);
        ConstruirTitulo();
        ConstruirDetalle();
        ConstruirZonaBotones();
    }

    private Columna CrearFaceta(int indice, float angulo, float ancho, bool primera, bool ultima)
    {
        Columna col = new Columna { angulo = angulo, ancho = ancho };
        col.canvas = CrearCanvasEnArco($"Faceta {indice}", 1500, new Vector2(ancho, AltoFaceta), angulo, 0f, Radio);

        Image fondo = EstiloUI.CrearImagen(col.canvas, "Fondo", Vector2.zero, Vector2.zero, new Color(0.01f, 0.02f, 0.04f, 0.92f));
        EstiloUI.Estirar(fondo.rectTransform);

        Color grilla = new Color(0.2f, 0.9f, 1f, 0.05f);
        for (float y = -AltoFaceta * 0.5f + 100f; y < AltoFaceta * 0.5f; y += 100f)
            EstiloUI.CrearImagen(col.canvas, "GrillaH", new Vector2(0f, y), new Vector2(ancho, 1f), grilla);
        for (float x = -ancho * 0.5f + ancho / 6f; x < ancho * 0.5f - 1f; x += ancho / 6f)
            EstiloUI.CrearImagen(col.canvas, "GrillaV", new Vector2(x, 0f), new Vector2(1f, AltoFaceta), grilla);

        // Bordes de arriba y abajo en todas (se ven como una sola franja curva).
        EstiloUI.CrearImagen(col.canvas, "BordeArriba", new Vector2(0f, AltoFaceta * 0.5f), new Vector2(ancho, 3f), EstiloUI.CianSuave);
        EstiloUI.CrearImagen(col.canvas, "BordeAbajo", new Vector2(0f, -AltoFaceta * 0.5f), new Vector2(ancho, 3f), EstiloUI.CianSuave);

        // Extremos del arco: borde lateral y esquinas marcadas.
        if (primera || ultima)
        {
            float lado = primera ? -ancho * 0.5f : ancho * 0.5f;
            EstiloUI.CrearImagen(col.canvas, "BordeLado", new Vector2(lado, 0f), new Vector2(3f, AltoFaceta), EstiloUI.CianSuave);
            float s = primera ? 1f : -1f;
            EstiloUI.CrearImagen(col.canvas, "EsquinaArribaH", new Vector2(lado + s * 50f, AltoFaceta * 0.5f), new Vector2(100f, 10f), EstiloUI.Cian);
            EstiloUI.CrearImagen(col.canvas, "EsquinaArribaV", new Vector2(lado, AltoFaceta * 0.5f - 50f), new Vector2(10f, 100f), EstiloUI.Cian);
            EstiloUI.CrearImagen(col.canvas, "EsquinaAbajoH", new Vector2(lado + s * 50f, -AltoFaceta * 0.5f), new Vector2(100f, 10f), EstiloUI.Cian);
            EstiloUI.CrearImagen(col.canvas, "EsquinaAbajoV", new Vector2(lado, -AltoFaceta * 0.5f + 50f), new Vector2(10f, 100f), EstiloUI.Cian);
        }

        col.escaneo = EstiloUI.CrearImagen(col.canvas, "Escaneo", Vector2.zero, new Vector2(ancho, 4f), new Color(0.2f, 0.9f, 1f, 0.1f)).rectTransform;
        return col;
    }

    private void ConstruirArbol(List<DatosFinales.Decision> decisiones)
    {
        int n = decisiones.Count;
        Final finalReal = FinalReal();
        Color colorCamino = ColorDe(finalReal);

        // Refrigeración "sin recortar" del camino real: así un cambio de decisión se puede
        // calcular exacto (inicio - lo que costó cada decisión - lo que costaron las caídas).
        float gastado = 0f;
        foreach (DatosFinales.Decision d in decisiones) gastado += d.CostoRefri(d.elegida);
        float refriCamino = DatosFinales.refrigeracionInicial - DatosFinales.refriPerdidaPorCaidas - gastado;

        // Camino principal: en cada faceta, media línea a la izquierda y media a la derecha del nodo.
        for (int j = 0; j < columnas.Count; j++)
        {
            Columna col = columnas[j];
            if (j > 0) col.izquierda = CrearLinea(col.canvas, new Vector2(-col.ancho * 0.5f, YCamino), new Vector2(0f, YCamino), 10f, colorCamino);
            if (j < columnas.Count - 1) col.derecha = CrearLinea(col.canvas, new Vector2(0f, YCamino), new Vector2(col.ancho * 0.5f, YCamino), 10f, colorCamino);
        }

        Vector2 posNodo = new Vector2(0f, YCamino);

        columnas[0].nodo = CrearNodo(columnas[0], posNodo, 120f, Color.white, false, "", "INICIO", Color.white,
            $"<b>INICIO</b>: empezaste con <b>{DatosFinales.refrigeracionInicial:0}%</b> de refrigeración.");

        for (int i = 0; i < n; i++)
        {
            DatosFinales.Decision d = decisiones[i];
            Columna col = columnas[i + 1];

            bool acerto = d.elegida == d.correcta;
            bool ia = d.elegida == DatosFinales.OpcionIA;
            Color aro = ia ? ColorIA : acerto ? ColorAcierto : ColorError;
            string tipo = ia ? "<color=#FF66BF>usaste la IA</color>" : acerto ? "<color=#40FF73>correcta</color>" : "<color=#FF4D4D>incorrecta</color>";

            string info = $"<b>Pregunta {i + 1}:</b> {d.enunciado}\n" +
                          $"Elegiste <b>\"{d.Texto(d.elegida)}\"</b> ({tipo}): -{d.CostoRefri(d.elegida):0}% refrigeración, {d.CostoAgua(d.elegida):0.00} L de agua." +
                          (acerto ? "" : $"\nLa correcta era <b>\"{d.Texto(d.correcta)}\"</b>.");
            col.nodo = CrearNodo(col, posNodo, 140f, aro, true, $"P{i + 1}", "", aro, info);

            // Ramas: las opciones que no elegiste (incluida la IA), con el final al que llevaban.
            List<int> alternativas = new List<int>();
            for (int op = 0; op < d.opciones.Length && op < 3; op++) if (op != d.elegida) alternativas.Add(op);
            if (d.elegida != DatosFinales.OpcionIA) alternativas.Add(DatosFinales.OpcionIA);

            for (int k = 0; k < alternativas.Count; k++)
            {
                int op = alternativas[k];
                float refriAlt = refriCamino + d.CostoRefri(d.elegida) - d.CostoRefri(op);
                Final finalAlt = FinalPorRefrigeracion(refriAlt);
                Color c = ColorDe(finalAlt);

                float desplazamiento = (k - (alternativas.Count - 1) * 0.5f) * 165f;
                Vector2 posAlt = new Vector2(desplazamiento, YCamino - 330f - (k % 2 == 1 ? 150f : 0f));
                col.ramas.Add(CrearLinea(col.canvas, posNodo, posAlt, 5f, new Color(c.r, c.g, c.b, 0.7f)));

                string extra = op == d.correcta ? " <color=#40FF73>(era la correcta)</color>" : "";
                string notaCaidas = finalReal == Final.Caidas ? "\n<i>(Ojo: igual te habrías caído las mismas veces.)</i>" : "";
                string infoAlt = $"<b>Pregunta {i + 1}</b>: si hubieras elegido <b>\"{d.Texto(op)}\"</b>{extra}...\n" +
                                 $"terminabas con ~{Mathf.Clamp(refriAlt, 0f, 100f):0}% de refrigeración: final <b><color=#{ColorUtility.ToHtmlStringRGB(c)}>{NombreDe(finalAlt)}</color></b>." + notaCaidas;
                col.nodosRamas.Add(CrearNodo(col, posAlt, 80f, c, false, "", NombreDe(finalAlt), c, infoAlt));
            }
        }

        Columna ultima = columnas[columnas.Count - 1];
        string infoFinal = $"<b>TU FINAL: <color=#{ColorUtility.ToHtmlStringRGB(colorCamino)}>{NombreDe(finalReal)}</color></b>\n" +
                           $"Refrigeración restante: {DatosFinales.refrigeracionRestante:0}%  ·  Agua evaporada: {DatosFinales.aguaConsumida:0.00} L  ·  Caídas: {DatosFinales.caidas}";
        nodoFinal = CrearNodo(ultima, posNodo, 210f, colorCamino, false, "", "TU FINAL\n" + NombreDe(finalReal), colorCamino, infoFinal);
        ultima.nodo = nodoFinal;
    }

    private void ConstruirTitulo()
    {
        float altura = (AltoFaceta * 0.5f + 170f) * MetrosPorPx;
        RectTransform canvas = CrearCanvasEnArco("Titulo", 1500, new Vector2(2000f, 300f), 0f, altura, Radio);

        EstiloUI.CrearTexto(canvas, "MAPA DE DECISIONES", new Vector2(0f, 95f), new Vector2(1900f, 100f), 84f, EstiloUI.Cian, FontStyles.Bold).characterSpacing = 8f;
        EstiloUI.CrearTexto(canvas, "Gira la cabeza: tu camino brilla y cada rama muestra a qué final te llevaba cambiar SOLO esa decisión.",
            new Vector2(0f, 25f), new Vector2(1900f, 50f), 32f, new Color(1f, 1f, 1f, 0.75f), FontStyles.Normal);

        Final[] finales = { Final.Optimo, Final.Moderado, Final.Critico, Final.Colapso, Final.Caidas };
        float paso = 360f;
        float x0 = -paso * (finales.Length - 1) * 0.5f;
        for (int i = 0; i < finales.Length; i++)
        {
            Vector2 p = new Vector2(x0 + i * paso, -55f);
            Image punto = EstiloUI.CrearImagen(canvas, "Leyenda", p + new Vector2(-95f, 0f), new Vector2(30f, 30f), ColorDe(finales[i]));
            punto.sprite = EstiloUI.Circulo();
            TextMeshProUGUI txt = EstiloUI.CrearTexto(canvas, NombreDe(finales[i]), p + new Vector2(35f, 0f), new Vector2(220f, 44f), 30f, ColorDe(finales[i]), FontStyles.Bold);
            txt.alignment = TextAlignmentOptions.Left;
        }
    }

    private void ConstruirDetalle()
    {
        canvasDetalle = CrearCanvasEnArco("Detalle", 1600, new Vector2(1250f, 240f), 0f, YDetalle * MetrosPorPx, Radio - 0.15f);

        Image caja = EstiloUI.CrearImagen(canvasDetalle, "Caja", Vector2.zero, Vector2.zero, new Color(0.01f, 0.03f, 0.06f, 0.96f));
        EstiloUI.Estirar(caja.rectTransform);
        EstiloUI.CrearBorde(canvasDetalle, 1250f, 240f, 3f, EstiloUI.Cian);
        EstiloUI.CrearImagen(canvasDetalle, "Acento", new Vector2(-622f, 0f), new Vector2(8f, 240f), EstiloUI.Cian);

        textoInfo = EstiloUI.CrearTexto(canvasDetalle, "", Vector2.zero, new Vector2(1180f, 215f), 36f, Color.white, FontStyles.Normal);
        textoInfo.enableWordWrapping = true;
        textoInfo.enableAutoSizing = true;
        textoInfo.fontSizeMax = 36f;
        textoInfo.fontSizeMin = 22f;
        textoInfo.text = "<color=#33E6FF>Apunta a un nodo con la mirada: este cuadro se mueve hasta él y te explica qué pasó.</color>";
    }

    private void ConstruirZonaBotones()
    {
        float altura = -(AltoFaceta * 0.5f + 110f) * MetrosPorPx;
        RectTransform canvas = CrearCanvasEnArco("Botones", 1500, new Vector2(1400f, 140f), 0f, altura, Radio);
        GameObject zona = new GameObject("Zona", typeof(RectTransform));
        zona.transform.SetParent(canvas, false);
        zonaBotones = (RectTransform)zona.transform;
    }

    private Nodo CrearNodo(Columna columna, Vector2 posicion, float tamano, Color color, bool conAro, string textoDentro, string etiquetaAbajo, Color colorEtiqueta, string info)
    {
        GameObject go = new GameObject("Nodo", typeof(RectTransform));
        go.transform.SetParent(columna.canvas, false);
        RectTransform raiz = (RectTransform)go.transform;
        raiz.anchoredPosition = posicion;
        raiz.sizeDelta = Vector2.one * (tamano + 90f); // zona para apuntar más grande que el dibujo
        raiz.localScale = Vector3.zero;                  // aparece con un "pop" (ver Aparecer)

        Image halo = EstiloUI.CrearImagen(raiz, "Halo", Vector2.zero, Vector2.one * tamano * 1.9f, new Color(color.r, color.g, color.b, 0.12f));
        halo.sprite = EstiloUI.Circulo();

        Image disco = EstiloUI.CrearImagen(raiz, "Disco", Vector2.zero, Vector2.one * tamano, color);
        disco.sprite = conAro ? EstiloUI.Aro() : EstiloUI.Circulo();

        if (conAro)
        {
            Image centroNodo = EstiloUI.CrearImagen(raiz, "Centro", Vector2.zero, Vector2.one * tamano * 0.74f, FondoNodo);
            centroNodo.sprite = EstiloUI.Circulo();
        }

        if (!string.IsNullOrEmpty(textoDentro))
            EstiloUI.CrearTexto(raiz, textoDentro, Vector2.zero, Vector2.one * tamano, tamano * 0.36f, Color.white, FontStyles.Bold);

        TextMeshProUGUI etiqueta = null;
        if (!string.IsNullOrEmpty(etiquetaAbajo))
        {
            float alto = etiquetaAbajo.Contains("\n") ? 100f : 50f;
            etiqueta = EstiloUI.CrearTexto(raiz, etiquetaAbajo, new Vector2(0f, -(tamano * 0.5f + 20f + alto * 0.5f)), new Vector2(260f, alto), tamano > 150f ? 40f : 28f, colorEtiqueta, FontStyles.Bold);
        }

        return new Nodo { raiz = raiz, disco = disco, etiqueta = etiqueta, color = color, info = info, columna = columna };
    }

    /// <summary>Línea entre dos puntos de una faceta: una imagen fina rotada, que "crece" desde el origen.</summary>
    private Linea CrearLinea(RectTransform canvas, Vector2 desde, Vector2 hasta, float grosor, Color color)
    {
        Vector2 delta = hasta - desde;
        Image img = EstiloUI.CrearImagen(canvas, "Linea", desde, new Vector2(0f, grosor), color);
        RectTransform rt = img.rectTransform;
        rt.pivot = new Vector2(0f, 0.5f);
        rt.anchoredPosition = desde;
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        rt.SetSiblingIndex(1); // detrás de los nodos, delante del fondo
        return new Linea { rt = rt, largo = delta.magnitude };
    }

    // ------------------------------------------------------------------
    // ANIMACIÓN
    // ------------------------------------------------------------------

    private IEnumerator Aparecer()
    {
        yield return Animar(0.8f, t => { foreach (CanvasGroup g in grupos) g.alpha = t; });

        // El camino avanza de faceta en faceta: llega al nodo, el nodo aparece, sigue.
        for (int j = 0; j < columnas.Count; j++)
        {
            Columna col = columnas[j];
            if (col.izquierda != null) yield return Crecer(col.izquierda, 0.2f);
            yield return Pop(col.nodo);
            if (col.derecha != null) yield return Crecer(col.derecha, 0.2f);
        }

        // Brotan todas las ramas a la vez.
        yield return Animar(0.6f, t =>
        {
            foreach (Columna col in columnas)
                foreach (Linea linea in col.ramas) linea.rt.sizeDelta = new Vector2(linea.largo * t, linea.rt.sizeDelta.y);
        });
        foreach (Columna col in columnas)
            foreach (Nodo nodo in col.nodosRamas) StartCoroutine(Pop(nodo));
        yield return new WaitForSeconds(0.4f);

        CrearBotonesFinales();
    }

    private IEnumerator Crecer(Linea linea, float duracion)
    {
        yield return Animar(duracion, t => linea.rt.sizeDelta = new Vector2(linea.largo * t, linea.rt.sizeDelta.y));
    }

    private IEnumerator Pop(Nodo nodo)
    {
        if (nodo == null) yield break;
        yield return Animar(0.25f, t =>
        {
            float k = t < 0.7f ? Mathf.Lerp(0f, 1.15f, t / 0.7f) : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
            nodo.raiz.localScale = Vector3.one * k;
        });
        nodo.raiz.localScale = Vector3.one;

        // Recién ahora (con su tamaño real) se puede apuntar con la mirada.
        OpcionMirable mirable = nodo.raiz.gameObject.AddComponent<OpcionMirable>();
        mirable.soloMirar = true;
        mirable.escalaMirada = 1.2f;
        mirable.Configurar(nodo.disco, nodo.color, Color.white, nodo.etiqueta, Color.white, null);
        string info = nodo.info;
        float angulo = nodo.columna.angulo;
        mirable.alMirar = mirando =>
        {
            if (!mirando) return; // se queda la última explicación mientras no mires otro nodo
            textoInfo.text = info;
            anguloDetalleObjetivo = angulo;
        };
    }

    private void CrearBotonesFinales()
    {
        EstiloUI.CrearBoton(zonaBotones, "VOLVER A JUGAR", new Vector2(-320f, 0f), new Vector2(560f, 100f), 44f,
            EstiloUI.BotonNormal, EstiloUI.BotonMirada, EstiloUI.Cian, EstiloUI.Cian, () => StartCoroutine(VolverAJugar()));
        EstiloUI.CrearBoton(zonaBotones, "SALIR", new Vector2(320f, 0f), new Vector2(560f, 100f), 44f,
            EstiloUI.BotonNormal, EstiloUI.BotonMirada, EstiloUI.Cian, EstiloUI.Cian, Salir);
    }

    private IEnumerator VolverAJugar()
    {
        yield return VeloNegro.Fundir(0f, 1f, 1f);
        SceneManager.LoadScene(EscenaJuego);
    }

    private void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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

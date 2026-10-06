using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// "Mapa de decisiones" que aparece después del informe final (escena final1).
/// Es un CÍRCULO COMPLETO: cada columna (INICIO, P1..Pn, TU FINAL) es una faceta repartida en los 360° que
/// envuelve al jugador, así cada pregunta queda de frente al girar la cabeza. Muestra:
///   - TU CAMINO, brillando: INICIO -> P1 -> P2 -> ... -> TU FINAL.
///   - El aro de cada pregunta dice cómo respondiste (verde correcta, rojo incorrecta,
///     magenta IA) y lo repite escrito abajo. La leyenda de arriba explica los colores.
///   - (Opcional, apagado) ramas "¿y si hubieras elegido otra?": ver MostrarRamasAlternativas.
/// Al apuntar con la mirada a un nodo, el cuadro de detalle se desliza por el arco hasta
/// quedar justo encima de esa columna, de frente. Se dibuja animado.
/// Usa lo que BaldosaPregunta fue guardando en DatosFinales durante la partida.
/// </summary>
public class MapaDecisiones : MonoBehaviour
{
    public enum Final { Optimo, Moderado, Critico, Colapso, Caidas }

    private const float MetrosPorPx = 0.002f;
    private const float Radio = 2.4f;           // metros del jugador al círculo (más cerca = todo más grande)
    private const float AltoFaceta = 1100f;     // px
    private const float YCamino = 260f;         // px: altura del camino dentro de cada faceta (abajo van las tarjetas)
    private const float BajarCentro = 0.15f;    // el arco va un poco bajo la línea de los ojos
    private const string EscenaJuego = "Nivel_Principal";
    private const int TotalPreguntas = 5;       // baldosas del túnel
    private const float CostoMinimo = 5f;       // lo que cuesta acertar una pregunta (% de refrigeración)

    // Las ramas "¿y si hubieras elegido otra opción?" (los circulitos de colores colgando bajo
    // cada pregunta) confundían: el rojo se mezclaba con el rojo de "incorrecta". Apagadas por
    // defecto; pon true para volver a verlas.
    private const bool MostrarRamasAlternativas = false;

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
        public RectTransform tarjeta; // explicación siempre visible bajo el nodo
        public RectTransform punta;   // flecha grande que entra al nodo
        public readonly List<RectTransform> flechitas = new List<RectTransform>(); // ">" que avanzan por el camino
        public float radioNodo;
    }

    // Flechas del camino: chevrones ">" que viajan de INICIO hacia TU FINAL, como datos fluyendo.
    private const float VelocidadFlechas = 120f;   // px por segundo
    private const float SeparacionFlechas = 115f;  // px aprox. entre flechitas
    private bool flechasActivas;

    /// <summary>Flecha "&gt;" hecha con dos barritas (no depende de que la fuente tenga el símbolo).</summary>
    private static RectTransform CrearChevron(RectTransform padre, Vector2 posicion, float tamano, float grosor, Color color)
    {
        GameObject go = new GameObject("Flecha", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = posicion;
        rt.sizeDelta = Vector2.one * tamano;

        float m = tamano * 0.5f;
        for (int s = -1; s <= 1; s += 2)
        {
            Vector2 a = new Vector2(-m * 0.6f, s * m * 0.8f), b = new Vector2(m * 0.4f, 0f);
            Vector2 d = b - a;
            Image barra = EstiloUI.CrearImagen(rt, "Brazo", (a + b) * 0.5f, new Vector2(d.magnitude + grosor * 0.5f, grosor), color);
            barra.rectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        }
        return rt;
    }

    // Notas extra para la tarjeta de cada columna (caídas, punto de no retorno).
    private readonly Dictionary<int, string> notasColumna = new Dictionary<int, string>();

    private void AgregarNota(int columna, string nota)
    {
        notasColumna[columna] = notasColumna.TryGetValue(columna, out string previa) ? previa + "\n" + nota : nota;
    }

    private Vector3 centro;
    private Vector3 adelante;
    private readonly List<Columna> columnas = new List<Columna>();
    private readonly List<CanvasGroup> grupos = new List<CanvasGroup>();

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

        // El botón TERMINAR acompaña a la mirada: cuando giras más de 20°, se desliza (suave)
        // hasta quedar otra vez abajo-al-frente; si giras poco, se queda quieto para poder apuntarlo.
        Transform cabezaJugador = PunteroMirada.Cabeza();
        if (canvasBotones != null && cabezaJugador != null)
        {
            Vector3 mirada = Vector3.ProjectOnPlane(cabezaJugador.forward, Vector3.up);
            if (mirada.sqrMagnitude > 0.001f)
            {
                float objetivo = Vector3.SignedAngle(adelante, mirada, Vector3.up);
                float diferencia = Mathf.Abs(Mathf.DeltaAngle(anguloBotones, objetivo));
                if (diferencia > 20f) botonesSiguiendo = true;
                else if (diferencia < 2f) botonesSiguiendo = false;
                if (botonesSiguiendo)
                    anguloBotones = Mathf.LerpAngle(anguloBotones, objetivo, 1f - Mathf.Exp(-4f * Time.unscaledDeltaTime));
                Colocar(canvasBotones, anguloBotones, alturaBotones, RadioBotones);
            }
        }

        // Las flechitas avanzan por el camino (de izquierda a derecha = de INICIO a TU FINAL).
        if (flechasActivas)
        {
            float avance = Time.unscaledTime * VelocidadFlechas;
            for (int j = 0; j < columnas.Count; j++)
            {
                Columna c = columnas[j];
                int cant = c.flechitas.Count;
                if (cant == 0) continue;
                float paso = c.ancho / cant; // reparto exacto: así se ven continuas de una faceta a la otra
                for (int k = 0; k < cant; k++)
                {
                    float x = Mathf.Repeat(k * paso + avance, c.ancho) - c.ancho * 0.5f;
                    bool hayCamino = x < 0f ? j > 0 : j < columnas.Count - 1;
                    bool lejosDelNodo = Mathf.Abs(x) > c.radioNodo + 85f;
                    RectTransform f = c.flechitas[k];
                    f.gameObject.SetActive(hayCamino && lejosDelNodo);
                    f.anchoredPosition = new Vector2(x, YCamino);
                }
            }
        }

        // El nodo de tu final late.
        if (nodoFinal != null && nodoFinal.disco != null)
            nodoFinal.disco.rectTransform.localScale = Vector3.one * (1f + 0.06f * Mathf.Sin(Time.unscaledTime * 3f));

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

        // CÍRCULO COMPLETO: las columnas se reparten en los 360° alrededor del jugador, así el
        // mapa queda cerrado (TU FINAL termina justo al lado de INICIO, a tu izquierda).
        float pasoGrados = 360f / cantidad;

        // Ancho de cada faceta = la cuerda entre dos columnas vecinas: así se tocan en los bordes.
        float ancho = 2f * Radio * Mathf.Sin(pasoGrados * 0.5f * Mathf.Deg2Rad) / MetrosPorPx;

        for (int j = 0; j < cantidad; j++)
        {
            // INICIO queda un poco a la izquierda, la PREGUNTA 1 justo al frente, y se sigue
            // hacia la derecha dando la vuelta completa.
            float angulo = (j - 1) * pasoGrados;
            columnas.Add(CrearFaceta(j, angulo, ancho, j == 0, j == cantidad - 1));
        }

        ConstruirArbol(decisiones);
        ConstruirTitulo();
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

            // Flechas: una grande entrando a cada nodo y varias chicas que avanzan por el camino.
            col.radioNodo = j == 0 ? 60f : j == columnas.Count - 1 ? 105f : 70f;
            Color brillante = Color.Lerp(colorCamino, Color.white, 0.35f);
            if (j > 0)
            {
                col.punta = CrearChevron(col.canvas, new Vector2(-(col.radioNodo + 45f), YCamino), 70f, 14f, brillante);
                col.punta.localScale = Vector3.zero;
            }
            int cantidadFlechitas = Mathf.Max(1, Mathf.RoundToInt(col.ancho / SeparacionFlechas));
            for (int k = 0; k < cantidadFlechitas; k++)
            {
                RectTransform f = CrearChevron(col.canvas, new Vector2(0f, YCamino), 34f, 7f, new Color(brillante.r, brillante.g, brillante.b, 0.85f));
                f.gameObject.SetActive(false);
                col.flechitas.Add(f);
            }
        }

        Vector2 posNodo = new Vector2(0f, YCamino);

        columnas[0].nodo = CrearNodo(columnas[0], posNodo, 120f, Color.white, false, "", "INICIO", Color.white,
            $"<b>INICIO</b>: empezaste con <b>{DatosFinales.refrigeracionInicial:0}%</b> de refrigeración.");

        // Textos de las tarjetas que se ven SIEMPRE bajo cada nodo (sin tener que apuntar).
        string[] tarjetas = new string[columnas.Count];
        Color[] coloresTarjeta = new Color[columnas.Count];
        tarjetas[0] = $"<color=#33E6FF><b>INICIO</b></color>\n" +
                      $"Empezaste con <b>{DatosFinales.refrigeracionInicial:0}%</b> de refrigeración.\n\n" +
                      "La refrigeración es el agua que enfría a la IA.\n" +
                      "Cada error, caída o ayuda de la IA la va gastando.";
        coloresTarjeta[0] = Color.white;

        for (int i = 0; i < n; i++)
        {
            DatosFinales.Decision d = decisiones[i];
            Columna col = columnas[i + 1];

            bool acerto = d.elegida == d.correcta;
            bool ia = d.elegida == DatosFinales.OpcionIA;
            Color aro = ia ? ColorIA : acerto ? ColorAcierto : ColorError;
            string tipo = ia ? "<color=#FF66BF>usaste la IA</color>" : acerto ? "<color=#40FF73>correcta</color>" : "<color=#FF4D4D>incorrecta</color>";

            string info = $"<b>Pregunta {i + 1}:</b> {d.enunciado}\n" +
                          $"Elegiste <b>\"{d.Texto(d.elegida)}\"</b> ({tipo}): -{d.CostoRefri(d.elegida):0}% refrigeración, -{d.CostoAgua(d.elegida):0.00} L de agua." +
                          (acerto ? "" : $"\nLa correcta era <b>\"{d.Texto(d.correcta)}\"</b>.");
            string resultado = ia ? "CON IA" : acerto ? "CORRECTA" : "INCORRECTA";
            col.nodo = CrearNodo(col, posNodo, 140f, aro, true, $"P{i + 1}", resultado, aro, info);

            string queHiciste = ia
                ? $"<color=#FF66BF><b>Le pediste la respuesta a la IA.</b></color>\nLa correcta era <b>{d.Texto(d.correcta)}</b>. Fue rápido, pero gastó mucha más agua."
                : acerto
                    ? $"Elegiste <b>{d.Texto(d.elegida)}</b>.\n<color=#40FF73><b>¡Correcta!</b></color> Pensaste por tu cuenta y ahorraste agua."
                    : $"Elegiste <b>{d.Texto(d.elegida)}</b>.\n<color=#FF4D4D><b>Incorrecta.</b></color> La correcta era <b>{d.Texto(d.correcta)}</b>.";
            tarjetas[i + 1] = $"<color=#33E6FF><b>PREGUNTA {i + 1}</b></color>\n{d.enunciado}\n\n{queHiciste}\n\n" +
                              $"<size=85%>Gastó <b>-{d.CostoRefri(d.elegida):0}%</b> de refrigeración y <b>-{d.CostoAgua(d.elegida):0.00} L</b> de agua.</size>";
            coloresTarjeta[i + 1] = aro;

            if (!MostrarRamasAlternativas) continue;

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
                if (finalAlt == Final.Colapso && finalReal == Final.Colapso)
                    notaCaidas += "\n<i>Cambiar SOLO esta decisión no alcanzaba: el resto ya había gastado demasiado.</i>";
                string infoAlt = $"<b>Pregunta {i + 1}</b>: si hubieras elegido <b>\"{d.Texto(op)}\"</b>{extra}...\n" +
                                 $"terminabas con ~{Mathf.Clamp(refriAlt, 0f, 100f):0}% de refrigeración: final <b><color=#{ColorUtility.ToHtmlStringRGB(c)}>{NombreDe(finalAlt)}</color></b>." + notaCaidas;
                col.nodosRamas.Add(CrearNodo(col, posAlt, 80f, c, false, "", $"{NombreDe(finalAlt)} ~{Mathf.Clamp(refriAlt, 0f, 100f):0}%", c, infoAlt));
            }
        }

        ConstruirCaidas(posNodo);
        string notaHeroe = ConstruirNoRetorno(posNodo, finalReal);
        string notaIdeal = ConstruirIdeal(finalReal);

        Columna ultima = columnas[columnas.Count - 1];
        string infoFinal = $"<b>TU FINAL: <color=#{ColorUtility.ToHtmlStringRGB(colorCamino)}>{NombreDe(finalReal)}</color></b>\n" +
                           $"Refrigeración restante: {DatosFinales.refrigeracionRestante:0}%  ·  Agua evaporada: {DatosFinales.aguaConsumida:0.00} L  ·  Caídas: {DatosFinales.caidas}" + notaHeroe;
        nodoFinal = CrearNodo(ultima, posNodo, 210f, colorCamino, false, "", "TU FINAL\n" + NombreDe(finalReal), colorCamino, infoFinal);
        ultima.nodo = nodoFinal;

        int caidas = DatosFinales.caidas;
        tarjetas[columnas.Count - 1] =
            $"<b>TU FINAL: <color=#{ColorUtility.ToHtmlStringRGB(colorCamino)}>{NombreDe(finalReal)}</color></b>\n" +
            $"Te quedó <b>{DatosFinales.refrigeracionRestante:0}%</b> de refrigeración.\n" +
            $"Se evaporaron <b>{DatosFinales.aguaConsumida:0.00} L</b> de agua.\n" +
            (caidas > 0 ? $"Te caíste <b>{caidas} {(caidas == 1 ? "vez" : "veces")}</b>.\n" : "") +
            notaIdeal + notaHeroe;
        coloresTarjeta[columnas.Count - 1] = colorCamino;

        // Tarjetas: el texto queda fijo bajo cada nodo, debajo de su etiqueta.
        for (int j = 0; j < columnas.Count; j++)
        {
            string texto = tarjetas[j] ?? "";
            if (notasColumna.TryGetValue(j, out string nota)) texto += "\n\n" + nota;
            float arriba = j == 0 ? YCamino - 150f : j == columnas.Count - 1 ? YCamino - 245f : YCamino - 160f;
            columnas[j].tarjeta = CrearTarjeta(columnas[j], texto, coloresTarjeta[j], arriba);
        }
    }

    /// <summary>Recuadro con la explicación de esa columna, siempre visible (aparece con un "pop").</summary>
    private RectTransform CrearTarjeta(Columna col, string texto, Color color, float arriba)
    {
        float abajo = -AltoFaceta * 0.5f + 30f;
        float alto = arriba - abajo;
        float ancho = col.ancho - 60f;

        GameObject go = new GameObject("Tarjeta", typeof(RectTransform));
        go.transform.SetParent(col.canvas, false);
        RectTransform rt = (RectTransform)go.transform;
        rt.anchoredPosition = new Vector2(0f, (arriba + abajo) * 0.5f);
        rt.sizeDelta = new Vector2(ancho, alto);
        rt.localScale = Vector3.zero;

        EstiloUI.CrearImagen(rt, "Fondo", Vector2.zero, new Vector2(ancho, alto), new Color(0f, 0f, 0f, 0.55f));
        EstiloUI.CrearImagen(rt, "Acento", new Vector2(-ancho * 0.5f + 3f, 0f), new Vector2(6f, alto), color);
        EstiloUI.CrearImagen(rt, "Tope", new Vector2(0f, alto * 0.5f), new Vector2(ancho, 2f), new Color(color.r, color.g, color.b, 0.5f));

        TextMeshProUGUI txt = EstiloUI.CrearTexto(rt, texto, new Vector2(6f, 0f), new Vector2(ancho - 50f, alto - 40f), 42f, Color.white, FontStyles.Normal);
        txt.alignment = TextAlignmentOptions.TopLeft;
        txt.enableWordWrapping = true;
        txt.enableAutoSizing = true;
        txt.fontSizeMax = 42f;
        txt.fontSizeMin = 26f;
        return rt;
    }

    /// <summary>
    /// "Punto de no retorno": la primera pregunta después de la cual, ni acertando TODO lo que
    /// faltaba, se evitaba el colapso. Pone una insignia "!" en esa pregunta y devuelve el texto
    /// para el nodo TU FINAL (reconoce a quien siguió intentando acertar después de eso).
    /// </summary>
    private string ConstruirNoRetorno(Vector2 posNodo, Final finalReal)
    {
        if (finalReal != Final.Colapso) return "";

        List<DatosFinales.Decision> dec = DatosFinales.decisiones;
        int n = dec.Count;
        int p = -1;
        for (int i = 1; i <= n; i++)
        {
            float r = DatosFinales.refrigeracionInicial;
            for (int k = 0; k < i; k++) r -= dec[k].CostoRefri(dec[k].elegida);
            foreach (DatosFinales.Caida c in DatosFinales.registroCaidas) if (c.decisionesPrevias <= i) r -= c.refri;

            float mejorPosible = r - CostoMinimo * Mathf.Max(0, TotalPreguntas - i);
            if (mejorPosible <= 0f) { p = i; break; }
        }
        if (p < 1) return "";

        string infoBadge = $"<b>PUNTO DE NO RETORNO</b>: después de la <b>pregunta {p}</b>, ni acertando todo lo que faltaba se podía evitar el colapso.";
        Columna col = columnas[p];
        col.nodosRamas.Add(CrearNodo(col, posNodo + new Vector2(-80f, 80f), 70f, new Color(0.92f, 0.92f, 0.97f, 1f), true, "!", "", Color.white, infoBadge));
        AgregarNota(p, "<b>(!) Desde aquí ya no se podía evitar el colapso</b>, aunque acertaras todo lo que faltaba.");

        int despues = n - p;
        int bien = 0;
        for (int k = p; k < n; k++) if (dec[k].elegida == dec[k].correcta) bien++;

        string nota = $"\n<color=#FFFFFF>Desde la pregunta {p} el colapso ya era inevitable.</color>";
        if (despues > 0 && bien > 0) nota += $" Aun así acertaste <b>{bien} de {despues}</b> después de eso: ¡seguir intentándolo contó!";
        else if (despues > 0) nota += " Aun así seguiste intentando: ¡eso cuenta!";
        return nota;
    }

    /// <summary>El mejor final posible (todo bien y sin caídas): texto para la tarjeta de TU FINAL.</summary>
    private string ConstruirIdeal(Final finalReal)
    {
        List<DatosFinales.Decision> dec = DatosFinales.decisiones;
        float r = DatosFinales.refrigeracionInicial;
        foreach (DatosFinales.Decision d in dec)
        {
            float menor = float.MaxValue;
            for (int op = 0; op < d.opciones.Length && op < 3; op++) menor = Mathf.Min(menor, d.CostoRefri(op));
            r -= menor;
        }
        r -= CostoMinimo * Mathf.Max(0, TotalPreguntas - dec.Count);

        if (finalReal == Final.Optimo && Mathf.Abs(r - DatosFinales.refrigeracionRestante) < 0.5f)
            return "\n<color=#40FF73><b>¡Hiciste el mejor camino posible!</b></color>";

        Final f = FinalPorRefrigeracion(r);
        return $"\n<size=85%>Sin ningún error habrías terminado en <b><color=#{ColorUtility.ToHtmlStringRGB(ColorDe(f))}>{NombreDe(f)}</color></b> (~{Mathf.Clamp(r, 0f, 100f):0}% de refrigeración).</size>";
    }

    /// <summary>
    /// Las caídas al vacío, en morado: una insignia pegada al nodo del tramo donde te caíste
    /// (la columna P{k} = "en el camino hacia la pregunta k"; la última = hacia el final).
    /// </summary>
    private void ConstruirCaidas(Vector2 posNodo)
    {
        if (DatosFinales.registroCaidas.Count == 0) return;

        Color morado = ColorDe(Final.Caidas);

        // Agrupa por columna (puede haber más de una caída en el mismo tramo).
        SortedDictionary<int, List<DatosFinales.Caida>> porColumna = new SortedDictionary<int, List<DatosFinales.Caida>>();
        foreach (DatosFinales.Caida c in DatosFinales.registroCaidas)
        {
            int idx = Mathf.Clamp(c.decisionesPrevias + 1, 1, columnas.Count - 1);
            if (!porColumna.ContainsKey(idx)) porColumna[idx] = new List<DatosFinales.Caida>();
            porColumna[idx].Add(c);
        }

        foreach (KeyValuePair<int, List<DatosFinales.Caida>> par in porColumna)
        {
            Columna col = columnas[par.Key];
            List<DatosFinales.Caida> lista = par.Value;
            bool enElFinal = par.Key == columnas.Count - 1;

            float refri = 0f, agua = 0f;
            foreach (DatosFinales.Caida c in lista) { refri += c.refri; agua += c.agua; }

            string donde = enElFinal ? "en el camino hacia el final" : $"en el camino hacia la <b>pregunta {par.Key}</b>";
            string veces = lista.Count == 1 ? "una vez" : $"{lista.Count} veces";
            string info = $"<b><color=#{ColorUtility.ToHtmlStringRGB(morado)}>CAÍDA</color></b>: te caíste al vacío <b>{veces}</b> {donde}.\n" +
                          $"Reconstruir el camino costó <b>-{refri:0}%</b> de refrigeración y <b>-{agua:0.00} L</b> de agua.";

            string dondeSimple = enElFinal ? "antes de llegar al final" : "antes de llegar a esta pregunta";
            AgregarNota(par.Key, $"<color=#{ColorUtility.ToHtmlStringRGB(morado)}><b>Te caíste al vacío {veces}</b> {dondeSimple}:</color> " +
                                 $"<b>-{refri:0}%</b> de refrigeración y <b>-{agua:0.00} L</b> de agua.");

            float desfase = enElFinal ? 125f : 80f;
            Vector2 pos = posNodo + new Vector2(desfase, desfase);
            col.nodosRamas.Add(CrearNodo(col, pos, 70f, morado, false, lista.Count == 1 ? "1" : $"x{lista.Count}", "", morado, info));
        }
    }

    private void ConstruirTitulo()
    {
        float altura = (AltoFaceta * 0.5f + 170f) * MetrosPorPx;
        RectTransform canvas = CrearCanvasEnArco("Titulo", 1500, new Vector2(2000f, 300f), 0f, altura, Radio);

        EstiloUI.CrearTexto(canvas, "MAPA DE DECISIONES", new Vector2(0f, 95f), new Vector2(1900f, 100f), 84f, EstiloUI.Cian, FontStyles.Bold).characterSpacing = 8f;
        EstiloUI.CrearTexto(canvas, "Así fue tu partida, pregunta por pregunta. Date una vuelta completa para leerla: empieza en INICIO, a tu izquierda.",
            new Vector2(0f, 25f), new Vector2(1900f, 50f), 32f, new Color(1f, 1f, 1f, 0.75f), FontStyles.Normal);

        // Leyenda: qué significa el color de cada círculo.
        string[] nombres = { "CORRECTA", "INCORRECTA", "USASTE LA IA", "CAÍDA" };
        Color[] colores = { ColorAcierto, ColorError, ColorIA, ColorDe(Final.Caidas) };
        float paso = 420f;
        float x0 = -paso * (nombres.Length - 1) * 0.5f;
        for (int i = 0; i < nombres.Length; i++)
        {
            Vector2 p = new Vector2(x0 + i * paso, -55f);
            Image punto = EstiloUI.CrearImagen(canvas, "Leyenda", p + new Vector2(-110f, 0f), new Vector2(30f, 30f), colores[i]);
            punto.sprite = EstiloUI.Circulo();
            TextMeshProUGUI txt = EstiloUI.CrearTexto(canvas, nombres[i], p + new Vector2(40f, 0f), new Vector2(260f, 44f), 30f, colores[i], FontStyles.Bold);
            txt.alignment = TextAlignmentOptions.Left;
        }
    }

    // El botón TERMINAR sigue tu mirada (con calma) por abajo del círculo: siempre a mano.
    private RectTransform canvasBotones;
    private float alturaBotones;
    private const float RadioBotones = Radio - 0.5f; // un poco más cerca que el círculo, para que quede delante
    private float anguloBotones;
    private bool botonesSiguiendo;

    private void ConstruirZonaBotones()
    {
        float altura = -(AltoFaceta * 0.5f - 60f) * MetrosPorPx; // abajo, a la altura del borde inferior del círculo
        alturaBotones = altura;
        RectTransform canvas = CrearCanvasEnArco("Botones", 1700, new Vector2(1400f, 140f), 0f, altura, RadioBotones);
        canvasBotones = canvas;
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
            if (col.punta != null) StartCoroutine(PopFlecha(col.punta));
            yield return Pop(col.nodo);
            if (col.tarjeta != null) StartCoroutine(PopTarjeta(col.tarjeta));
            if (col.derecha != null) yield return Crecer(col.derecha, 0.2f);
        }

        // El camino ya está completo: las flechitas empiezan a avanzar.
        flechasActivas = true;

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

    private IEnumerator PopFlecha(RectTransform flecha)
    {
        yield return Animar(0.2f, t => flecha.localScale = Vector3.one * (t < 0.7f ? Mathf.Lerp(0f, 1.3f, t / 0.7f) : Mathf.Lerp(1.3f, 1f, (t - 0.7f) / 0.3f)));
        flecha.localScale = Vector3.one;
    }

    private IEnumerator PopTarjeta(RectTransform tarjeta)
    {
        yield return Animar(0.3f, t =>
        {
            float k = t < 0.7f ? Mathf.Lerp(0f, 1.05f, t / 0.7f) : Mathf.Lerp(1.05f, 1f, (t - 0.7f) / 0.3f);
            tarjeta.localScale = new Vector3(1f, k, 1f);
        });
        tarjeta.localScale = Vector3.one;
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
    }

    // ----------------------- PANTALLA "FIN DEL JUEGO" (edítalo aquí) -----------------------
    private const string Cita = "«Cuando el pozo se seca, conocemos el valor del agua.»";
    private const string AutorCita = "— Benjamin Franklin, <i>Poor Richard's Almanack</i> (1746)";
    private const string Reflexion = "La inteligencia artificial no solo consume datos: también bebe agua.\nUsarla con conciencia está en tus manos.";
    // ------------------------------------------------------------------------------------------

    /// <summary>Al terminar de dibujarse el mapa: un solo botón TERMINAR.</summary>
    private void CrearBotonesFinales()
    {
        bool elegido = false;
        EstiloUI.CrearBoton(zonaBotones, "TERMINAR", Vector2.zero, new Vector2(560f, 100f), 44f,
            EstiloUI.BotonNormal, EstiloUI.BotonMirada, EstiloUI.Cian, EstiloUI.Cian, () =>
            {
                if (elegido) return;
                elegido = true;
                StartCoroutine(FinDelJuego());
            });
    }

    /// <summary>
    /// El mapa se desvanece, todo queda en negro y aparece "FIN DEL JUEGO" (decodificándose),
    /// una cita de reflexión que se escribe letra por letra y, recién al final,
    /// los botones VOLVER A JUGAR y SALIR.
    /// </summary>
    private IEnumerator FinDelJuego()
    {
        // 1) Fuera el mapa, todo a negro.
        StartCoroutine(VeloNegro.Fundir(0f, 1f, 1.2f));
        yield return Animar(1.2f, t => { foreach (CanvasGroup g in grupos) g.alpha = 1f - t; });
        foreach (CanvasGroup g in grupos) g.gameObject.SetActive(false); // que no se puedan mirar los nodos
        yield return new WaitForSeconds(0.4f);

        // 2) Panel fijo delante del jugador (fijo, para poder apuntar los botones con la mirada).
        const float ancho = 1600f, alto = 1250f;
        RectTransform panel = EstiloUI.CrearCanvas("FinDelJuego", 1550, new Vector2(ancho, alto), 0.0013f); // encima del velo negro
        panel.SetParent(transform, false);
        EstiloUI.ColocarDelante(panel, 1.9f, 0.05f); // más cerca = letra más grande en el visor
        CanvasGroup grupo = panel.gameObject.AddComponent<CanvasGroup>();

        TextMeshProUGUI etiqueta = EstiloUI.CrearTexto(panel, "// SED ALGORÍTMICA", new Vector2(0f, 560f), new Vector2(ancho, 50f), 36f, new Color(0.2f, 0.9f, 1f, 0.6f), FontStyles.Normal);
        etiqueta.characterSpacing = 8f;
        TextMeshProUGUI titulo = EstiloUI.CrearTexto(panel, "", new Vector2(0f, 450f), new Vector2(ancho, 150f), 124f, EstiloUI.Cian, FontStyles.Bold);
        titulo.characterSpacing = 10f;
        RectTransform linea = EstiloUI.CrearImagen(panel, "Linea", new Vector2(0f, 355f), new Vector2(0f, 5f), EstiloUI.Cian).rectTransform;

        TextMeshProUGUI cita = EstiloUI.CrearTexto(panel, Cita, new Vector2(0f, 210f), new Vector2(ancho - 100f, 230f), 78f, Color.white, FontStyles.Italic);
        cita.enableWordWrapping = true;
        cita.maxVisibleCharacters = 0;
        TextMeshProUGUI autor = EstiloUI.CrearTexto(panel, AutorCita, new Vector2(0f, 55f), new Vector2(ancho - 100f, 64f), 46f, new Color(0.2f, 0.9f, 1f, 0.85f), FontStyles.Normal);
        autor.alpha = 0f;
        TextMeshProUGUI reflexion = EstiloUI.CrearTexto(panel, Reflexion, new Vector2(0f, -150f), new Vector2(ancho - 120f, 230f), 54f, new Color(1f, 1f, 1f, 0.8f), FontStyles.Normal);
        reflexion.enableWordWrapping = true;
        reflexion.alpha = 0f;

        GameObject goBotones = new GameObject("Botones", typeof(RectTransform));
        goBotones.transform.SetParent(panel, false);
        RectTransform botones = (RectTransform)goBotones.transform;
        botones.anchoredPosition = new Vector2(0f, -450f);

        // 3) "FIN DEL JUEGO" se decodifica (caracteres raros que se van fijando).
        const string textoTitulo = "FIN DEL JUEGO";
        const string glitch = "01#%&@$<>/\\[]{}=+*";
        yield return Animar(1.4f, t =>
        {
            int fijas = Mathf.FloorToInt(textoTitulo.Length * t);
            System.Text.StringBuilder sb = new System.Text.StringBuilder(textoTitulo.Substring(0, fijas));
            for (int i = fijas; i < textoTitulo.Length; i++) sb.Append(textoTitulo[i] == ' ' ? ' ' : glitch[Random.Range(0, glitch.Length)]);
            titulo.text = sb.ToString();
        });
        yield return Animar(0.6f, t => linea.sizeDelta = new Vector2(1000f * Mathf.SmoothStep(0f, 1f, t), 5f));
        yield return new WaitForSeconds(0.5f);

        // 4) La cita, letra por letra; luego el autor y la reflexión.
        cita.ForceMeshUpdate();
        int total = cita.textInfo.characterCount;
        for (int i = 0; i <= total; i++)
        {
            cita.maxVisibleCharacters = i;
            yield return new WaitForSeconds(0.045f);
        }
        yield return new WaitForSeconds(0.4f);
        yield return Animar(0.8f, t => autor.alpha = t);
        yield return new WaitForSeconds(0.8f);
        yield return Animar(1f, t => reflexion.alpha = t);
        yield return new WaitForSeconds(1.5f);

        // 5) Recién ahora: volver a jugar o salir.
        EstiloUI.CrearBoton(botones, "VOLVER A JUGAR", new Vector2(-350f, 0f), new Vector2(640f, 120f), 54f,
            EstiloUI.BotonNormal, EstiloUI.BotonMirada, EstiloUI.Cian, EstiloUI.Cian, () => StartCoroutine(VolverAJugar()));
        EstiloUI.CrearBoton(botones, "SALIR", new Vector2(350f, 0f), new Vector2(640f, 120f), 54f,
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

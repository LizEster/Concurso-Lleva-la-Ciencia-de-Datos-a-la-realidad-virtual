using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// "Informe final del sistema" de la escena final1: un panel holográfico (mismo estilo del
/// menú) cuyo color depende de cómo terminó la partida, con tarjetas de agua / refrigeración /
/// rendimiento que cuentan hacia arriba, los mensajes finales escribiéndose como en una
/// terminal, y botones que se eligen con la mirada + A/B/X/Y. Al final lleva al MapaDecisiones.
/// Lo crea FinalManager con InformeFinal.Mostrar(...).
/// </summary>
public class InformeFinal : MonoBehaviour
{
    private const float Ancho = 1200f;
    private const float Alto = 960f;   // más alto para que quepa el texto grande (se lee mejor en el visor)
    private const float MetrosPorPx = 0.0015f;
    private const float Distancia = 1.85f; // más cerca = todo se ve más grande en el visor
    private const float SegundosPorLetra = 0.025f;

    private string[] mensajes;
    private float agua;
    private float refrigeracion;

    // Tema según el final
    private Color acento;
    private string titulo;
    private string estado;

    private RectTransform panel;
    private CanvasGroup grupo;
    private RectTransform lineaEscaneo;
    private TextMeshProUGUI textoTitulo;
    private TextMeshProUGUI valorAgua;
    private TextMeshProUGUI valorRefri;
    private RectTransform barraRefri;
    private TextMeshProUGUI textoMensaje;
    private Image[] puntos;
    private RectTransform zonaBotones;
    private Image indicadorSeguir;
    private bool eligioContinuar;
    private FondoGrafos fondoGrafos;

    public static void Mostrar(string[] mensajes, float agua, float refrigeracion)
    {
        InformeFinal informe = new GameObject("[InformeFinal]").AddComponent<InformeFinal>();
        informe.mensajes = mensajes ?? new string[0];
        informe.agua = agua;
        informe.refrigeracion = refrigeracion;
    }

    void Start()
    {
        DefinirTema();

        // Red de nodos de fondo, del color del resultado (verde, amarillo, naranja, rojo o morado).
        // Queda suelta en la escena para poder desvanecerse sola cuando pasamos al mapa.
        fondoGrafos = FondoGrafos.Crear(null, acento);

        Construir();
        EstiloUI.ColocarDelante(panel, Distancia, 0.05f);
        StartCoroutine(Secuencia());
    }

    void Update()
    {
        if (panel == null) return;

        // Línea de escaneo y título que "respira".
        float y = Mathf.Repeat(Time.unscaledTime * 180f, Alto);
        lineaEscaneo.anchoredPosition = new Vector2(0f, Alto * 0.5f - y);

        Color c = acento;
        c.a = 0.8f + 0.2f * Mathf.Sin(Time.unscaledTime * 2.2f);
        textoTitulo.color = c;

        if (indicadorSeguir != null && indicadorSeguir.gameObject.activeSelf)
        {
            Color ci = acento;
            ci.a = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            indicadorSeguir.color = ci;
        }

        // Si el jugador se da vuelta, el informe vuelve a ponerse delante.
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza != null)
        {
            Vector3 haciaPanel = Vector3.ProjectOnPlane(panel.position - cabeza.position, Vector3.up);
            Vector3 adelante = Vector3.ProjectOnPlane(cabeza.forward, Vector3.up);
            if (adelante.sqrMagnitude > 0.001f && Vector3.Angle(haciaPanel, adelante) > 70f)
                EstiloUI.ColocarDelante(panel, Distancia, 0.05f);
        }
    }

    // ------------------------------------------------------------------
    // SECUENCIA
    // ------------------------------------------------------------------

    private IEnumerator Secuencia()
    {
        // Aparece el panel y los números cuentan hacia arriba.
        yield return Animar(0.6f, t => grupo.alpha = t);
        yield return Animar(1.6f, t =>
        {
            float k = 1f - Mathf.Pow(1f - t, 3f); // arranca rápido y frena al final
            valorAgua.text = $"{agua * k:0.00} L";
            valorRefri.text = $"{refrigeracion * k:0}%";
            barraRefri.sizeDelta = new Vector2(280f * Mathf.Clamp01(refrigeracion / 100f) * k, barraRefri.sizeDelta.y);
        });

        for (int i = 0; i < mensajes.Length; i++)
        {
            MarcarPagina(i);
            yield return Escribir(mensajes[i]);

            if (i < mensajes.Length - 1)
            {
                eligioContinuar = false;
                CrearBotonesContinuar();
                while (!eligioContinuar) yield return null;
            }
            else
            {
                CrearBotonesFinales();
            }
        }
    }

    /// <summary>Escribe el mensaje letra por letra. Apretar un botón mientras escribe lo muestra completo.</summary>
    private IEnumerator Escribir(string mensaje)
    {
        LimpiarBotones();
        indicadorSeguir.gameObject.SetActive(false);

        textoMensaje.text = Colorear(mensaje);
        textoMensaje.ForceMeshUpdate();
        int total = textoMensaje.textInfo.characterCount;
        textoMensaje.maxVisibleCharacters = 0;

        float tiempo = 0f;
        bool salto = false;
        while (textoMensaje.maxVisibleCharacters < total)
        {
            if (EntradaVR.ConfirmarPresionado() && PunteroMirada.FrameUltimaEleccion != Time.frameCount)
            {
                salto = true;
                break;
            }
            tiempo += Time.deltaTime;
            textoMensaje.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(tiempo / SegundosPorLetra));
            yield return null;
        }
        textoMensaje.maxVisibleCharacters = total;

        // Si se apretó para saltar, ese mismo botonazo no debe elegir el botón que aparece ahora.
        if (salto) yield return null;
        indicadorSeguir.gameObject.SetActive(true);
    }

    /// <summary>Los "> " del principio de cada línea van en el color del final, como un prompt.</summary>
    private string Colorear(string mensaje)
    {
        string hex = ColorUtility.ToHtmlStringRGB(acento);
        return mensaje.Replace("> ", $"<color=#{hex}><b>></b></color> ");
    }

    private void MarcarPagina(int indice)
    {
        for (int i = 0; i < puntos.Length; i++)
        {
            Color c = acento;
            c.a = i < indice ? 0.6f : i == indice ? 1f : 0.15f;
            puntos[i].color = c;
            puntos[i].rectTransform.sizeDelta = Vector2.one * (i == indice ? 22f : 14f);
        }
    }

    private void CrearBotonesContinuar()
    {
        LimpiarBotones();
        CrearBoton("CONTINUAR", new Vector2(0f, 0f), 420f, () => eligioContinuar = true);
    }

    private void CrearBotonesFinales()
    {
        LimpiarBotones();
        CrearBoton("VER MAPA DE DECISIONES", new Vector2(0f, 0f), 640f, () => StartCoroutine(IrAlMapa()));
    }

    /// <summary>El informe se desvanece y aparece el Mapa de decisiones (que tiene "Volver a jugar" y "Salir").</summary>
    private IEnumerator IrAlMapa()
    {
        LimpiarBotones();
        yield return Animar(0.5f, t => grupo.alpha = 1f - t);

        // El mapa vive dentro de la escena de tu final (naturaleza, basura, seca...). Si esa
        // escena todavía no existe en el build, se muestra aquí mismo.
        string escena = EscenaFinalSegunResultado.Nombre(MapaDecisiones.FinalReal());
        if (!string.IsNullOrEmpty(escena) && Application.CanStreamedLevelBeLoaded(escena))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(escena);
            yield break;
        }

        if (fondoGrafos != null) fondoGrafos.Desvanecer(0.8f);
        MapaDecisiones.Mostrar();
        Destroy(gameObject);
    }

    // ------------------------------------------------------------------
    // CONSTRUCCIÓN
    // ------------------------------------------------------------------

    private void DefinirTema()
    {
        if (DatosFinales.colapsoTermico)
        {
            acento = new Color(1f, 0.3f, 0.3f, 1f); titulo = "COLAPSO TÉRMICO"; estado = "COLAPSO";
        }
        else if (DatosFinales.demasiadasCaidas)
        {
            acento = new Color(0.75f, 0.5f, 1f, 1f); titulo = "USUARIO PERDIDO"; estado = "SIN SEÑAL";
        }
        else if (refrigeracion >= 60f)
        {
            acento = new Color(0.25f, 1f, 0.45f, 1f); titulo = "PROCESAMIENTO COMPLETADO"; estado = "ÓPTIMO"; // verde
        }
        else if (refrigeracion >= 25f)
        {
            acento = new Color(1f, 0.85f, 0.2f, 1f); titulo = "PROCESAMIENTO COMPLETADO"; estado = "MODERADO"; // amarillo
        }
        else
        {
            acento = new Color(1f, 0.55f, 0.15f, 1f); titulo = "COMPLETADO... APENAS"; estado = "CRÍTICO"; // naranja
        }
    }

    private Color Suave(float alfa) { Color c = acento; c.a = alfa; return c; }

    private void Construir()
    {
        panel = EstiloUI.CrearCanvas("InformeFinal", 1500, new Vector2(Ancho, Alto), MetrosPorPx);
        panel.SetParent(transform, false);
        grupo = panel.gameObject.AddComponent<CanvasGroup>();
        grupo.alpha = 0f;

        // Fondo con cuadrícula tenue, borde, esquinas y línea de escaneo.
        Image fondo = EstiloUI.CrearImagen(panel, "Fondo", Vector2.zero, Vector2.zero, EstiloUI.FondoPanel);
        EstiloUI.Estirar(fondo.rectTransform);
        for (float x = -Ancho * 0.5f + 100f; x < Ancho * 0.5f; x += 100f)
            EstiloUI.CrearImagen(panel, "GrillaV", new Vector2(x, 0f), new Vector2(1f, Alto), Suave(0.05f));
        for (float y = -Alto * 0.5f + 100f; y < Alto * 0.5f; y += 100f)
            EstiloUI.CrearImagen(panel, "GrillaH", new Vector2(0f, y), new Vector2(Ancho, 1f), Suave(0.05f));
        EstiloUI.CrearBorde(panel, Ancho, Alto, 3f, Suave(0.4f));
        EstiloUI.CrearEsquinas(panel, Ancho, Alto, 80f, 8f, acento);
        lineaEscaneo = EstiloUI.CrearImagen(panel, "Escaneo", Vector2.zero, new Vector2(Ancho, 4f), Suave(0.12f)).rectTransform;

        // Encabezado: etiqueta + "chip" de estado.
        TextMeshProUGUI etiqueta = EstiloUI.CrearTexto(panel, "// INFORME FINAL DEL SISTEMA", new Vector2(-240f, 420f), new Vector2(660f, 50f), 30f, Suave(0.7f), FontStyles.Normal);
        etiqueta.alignment = TextAlignmentOptions.Left;
        Image chip = EstiloUI.CrearImagen(panel, "Chip", new Vector2(420f, 420f), new Vector2(290f, 60f), Suave(0.15f));
        EstiloUI.CrearBorde(chip.rectTransform, 290f, 60f, 2f, acento);
        EstiloUI.CrearTexto(chip.rectTransform, estado, Vector2.zero, new Vector2(280f, 56f), 32f, acento, FontStyles.Bold);

        // Título.
        textoTitulo = EstiloUI.CrearTexto(panel, titulo, new Vector2(0f, 335f), new Vector2(Ancho - 80f, 100f), 76f, acento, FontStyles.Bold);
        textoTitulo.characterSpacing = 5f;
        textoTitulo.enableAutoSizing = true;
        textoTitulo.fontSizeMax = 76f;
        textoTitulo.fontSizeMin = 48f;
        EstiloUI.CrearImagen(panel, "Separador", new Vector2(0f, 275f), new Vector2(Ancho - 160f, 2f), Suave(0.4f));

        // Tarjetas.
        valorAgua = CrearTarjeta(new Vector2(-385f, 185f), "AGUA EVAPORADA", "0.00 L");
        valorRefri = CrearTarjeta(new Vector2(0f, 185f), "REFRIGERACIÓN", "0%");
        CrearTarjeta(new Vector2(385f, 185f), "RENDIMIENTO", estado);

        EstiloUI.CrearImagen(panel, "BarraRefriFondo", new Vector2(0f, 95f), new Vector2(280f, 8f), Suave(0.15f));
        Image barra = EstiloUI.CrearImagen(panel, "BarraRefri", new Vector2(-140f, 95f), new Vector2(0f, 8f), acento);
        barraRefri = barra.rectTransform;
        barraRefri.pivot = new Vector2(0f, 0.5f);

        // Caja del mensaje.
        Image caja = EstiloUI.CrearImagen(panel, "CajaMensaje", new Vector2(0f, -100f), new Vector2(Ancho - 100f, 360f), new Color(0f, 0f, 0f, 0.35f));
        EstiloUI.CrearImagen(caja.rectTransform, "Acento", new Vector2(-(Ancho - 100f) * 0.5f, 0f), new Vector2(6f, 360f), acento);
        textoMensaje = EstiloUI.CrearTexto(caja.rectTransform, "", Vector2.zero, new Vector2(Ancho - 160f, 330f), 46f, Color.white, FontStyles.Normal);
        textoMensaje.alignment = TextAlignmentOptions.TopLeft;
        textoMensaje.enableWordWrapping = true;
        textoMensaje.enableAutoSizing = true;
        textoMensaje.fontSizeMax = 46f;
        textoMensaje.fontSizeMin = 34f;
        textoMensaje.richText = true;
        indicadorSeguir = EstiloUI.CrearImagen(caja.rectTransform, "Listo", new Vector2((Ancho - 100f) * 0.5f - 24f, -156f), new Vector2(16f, 16f), acento);
        indicadorSeguir.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        indicadorSeguir.gameObject.SetActive(false);

        // Puntos de página.
        puntos = new Image[Mathf.Max(1, mensajes.Length)];
        float inicio = -(puntos.Length - 1) * 18f;
        for (int i = 0; i < puntos.Length; i++)
        {
            puntos[i] = EstiloUI.CrearImagen(panel, "Pagina", new Vector2(inicio + i * 36f, -312f), new Vector2(14f, 14f), Suave(0.15f));
            puntos[i].rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
        }

        GameObject zona = new GameObject("Botones", typeof(RectTransform));
        zona.transform.SetParent(panel, false);
        zonaBotones = (RectTransform)zona.transform;
        zonaBotones.anchoredPosition = new Vector2(0f, -405f);
    }

    private TextMeshProUGUI CrearTarjeta(Vector2 posicion, string etiqueta, string valor)
    {
        Image tarjeta = EstiloUI.CrearImagen(panel, "Tarjeta", posicion, new Vector2(360f, 150f), Suave(0.07f));
        EstiloUI.CrearBorde(tarjeta.rectTransform, 360f, 150f, 2f, Suave(0.35f));
        EstiloUI.CrearTexto(tarjeta.rectTransform, etiqueta, new Vector2(0f, 45f), new Vector2(345f, 40f), 28f, new Color(1f, 1f, 1f, 0.6f), FontStyles.Normal).characterSpacing = 4f;
        TextMeshProUGUI txt = EstiloUI.CrearTexto(tarjeta.rectTransform, valor, new Vector2(0f, -15f), new Vector2(345f, 76f), 60f, Color.white, FontStyles.Bold);
        return txt;
    }

    private void CrearBoton(string texto, Vector2 posicion, float ancho, System.Action alElegir)
    {
        Color normal = new Color(acento.r * 0.15f, acento.g * 0.15f, acento.b * 0.15f, 0.95f);
        EstiloUI.CrearBoton(zonaBotones, texto, posicion, new Vector2(ancho, 96f), 46f, normal, acento, acento, acento, alElegir);
    }

    private void LimpiarBotones()
    {
        if (zonaBotones == null) return;
        for (int i = zonaBotones.childCount - 1; i >= 0; i--)
        {
            GameObject boton = zonaBotones.GetChild(i).gameObject;
            boton.SetActive(false);
            Destroy(boton);
        }
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

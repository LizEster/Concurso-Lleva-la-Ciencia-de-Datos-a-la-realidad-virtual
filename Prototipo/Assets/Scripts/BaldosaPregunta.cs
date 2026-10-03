using System.Collections;
using UnityEngine;

public class BaldosaPregunta : MonoBehaviour
{
    [Header("Ajuste de Detección")]
    public float distanciaDeActivacion = 2.5f;

    [Header("Emersión del piso")]
    [Tooltip("Marca esto SOLO en el Piso 1: la plataforma inicial, que ya está en su sitio desde el arranque y nunca se hunde.")]
    public bool esPisoInicial = false;
    [Tooltip("Cuánto se hunde el piso bajo su posición final mientras espera en el vacío, antes de emerger")]
    public float profundidadEmersion = 4f;
    [Tooltip("Cuánto tarda en subir desde el vacío hasta su posición final, en segundos")]
    public float duracionEmersion = 1.2f;
    [Tooltip("La baldosa que debe emerger cuando ÉSTA se responde (arrástrala desde la Jerarquía). Déjalo vacío en la última baldosa.")]
    public BaldosaPregunta siguienteBaldosa;

    [Header("Panel Holográfico")]
    [Tooltip("Arrastra aquí el PanelHolografico que cuelga sobre esta baldosa. Si lo dejas vacío, se usa la terminal de texto como respaldo (comportamiento anterior).")]
    public PanelHolografico panelHolograma;

    [Header("Diálogo del robot en el Piso 1")]
    [Tooltip("Sólo se usa en la baldosa marcada como 'Piso Inicial': lo que dice el robot apenas el jugador la pisa por primera vez.")]
    [TextArea(1, 2)]
    public string textoRobotBuenaSuerte = "¡Mucha suerte!!";
    [Tooltip("Segundos que se muestra ese aviso del robot.")]
    public float duracionAvisoRobot = 4f;
    [Tooltip("Arrastra aquí el mismo AudioSource que usa el bot para su bip de diálogo (sólo hace falta asignarlo en la baldosa marcada como Piso Inicial).")]
    public AudioSource fuenteAudioBip;
    public AudioClip clipBip;

    [Header("Cronómetro para responder")]
    [Tooltip("Segundos para responder la pregunta desde que aparece. Si llega a 0, esta baldosa se cae contigo encima. 0 = sin límite.")]
    public float tiempoParaResponder = 30f;

    [Header("Caída y reaparición")]
    [Tooltip("Cuántos metros bajo la baldosa tiene que caer el jugador para reaparecer encima de ella.")]
    public float alturaCaidaParaReaparecer = 6f;
    [Tooltip("Reservas de refrigeración que se pierden al caerse.")]
    public float costoCaidaRefri = 20f;
    [Tooltip("Litros de agua que se gastan al caerse.")]
    public float costoCaidaAgua = 1f;
    [TextArea(1, 3)]
    public string mensajeCaida = "> Caída al vacío detectada.\nReconstruir el camino consumió reservas de refrigeración.";

    [Header("Colapso del piso (una vez que ya avanzaste)")]
    [Tooltip("Segundos que espera este piso, después de responder SU pregunta, antes de empezar a colapsar. Dale tiempo suficiente para cruzar al siguiente.")]
    public float tiempoAntesDeColapsar = 4f;
    [Tooltip("Cuánto tarda en hundirse una vez que empieza a colapsar, en segundos")]
    public float duracionColapso = 0.8f;
    [Tooltip("Cuánto se hunde el piso al colapsar, antes de desaparecer del todo")]
    public float profundidadColapso = 10f;

    // La pregunta de esta baldosa se saca al azar del banco compartido (BancoPreguntas) en
    // Start(): ya no se elige a mano por piso, así cada partida toca una combinación distinta
    // y sin repetir entre los 5 pisos.
    private string enunciadoPregunta;
    private string[] textoOpciones;
    private float[] costoRefri;
    private float[] costoAgua;
    private string[] mensajeResultado;
    private int indiceCorrecta = -1;

    private bool jugadorEncima = false;
    private bool avisoBienvenidaDicho = false;
    private bool respondida = false;
    private bool resolviendoSeleccion = false; // true mientras se resalta el botón elegido, antes de resolver de verdad
    public bool EstaRespondida => respondida;
    private GameObject playerObjeto;
    private MonoBehaviour scriptMovimientoPlayer;

    private Collider colisionadorPropio;
    private Renderer[] renderersPropios;
    private Vector3 posicionFinal;
    private bool yaEmergida;

    private float tiempoRestante;
    private bool cronometroCorriendo;
    private bool colapsadaPorTiempo;   // se cayó porque se acabó el tiempo (sin responder)
    private float alturaSuperficie;    // metros desde el pivote de la baldosa hasta su cara de arriba

    /// <summary>La última baldosa que pisó el jugador: si se cae, reaparece ahí.</summary>
    private static BaldosaPregunta ultimaPisada;

    void Start()
    {
        AsignarPreguntaDelBanco();
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null) col.isTrigger = false;

        colisionadorPropio = GetComponent<Collider>();
        renderersPropios = GetComponentsInChildren<Renderer>();
        posicionFinal = transform.position;
        alturaSuperficie = CalcularAlturaSuperficie();

        if (esPisoInicial)
        {
            // El Piso 1 ya está formado desde el arranque: no se hunde ni espera a emerger.
            yaEmergida = true;
        }
        else
        {
            // Empieza hundida en el vacío: invisible y sin colisión hasta que le toque emerger.
            yaEmergida = false;
            transform.position = posicionFinal - Vector3.up * profundidadEmersion;
            SetVisible(false);
            if (colisionadorPropio != null) colisionadorPropio.enabled = false;
        }
    }

    private void AsignarPreguntaDelBanco()
    {
        PreguntaVectorial datos = BancoPreguntas.SacarSiguiente();

        enunciadoPregunta = datos.enunciado;
        textoOpciones = datos.textoOpciones;
        indiceCorrecta = datos.indiceCorrecta;
        costoRefri = datos.costoRefri;
        costoAgua = datos.costoAgua;
        mensajeResultado = datos.mensajeResultado;
    }

    void Update()
    {
        if (playerObjeto == null) return;

        RevisarPisadaYCaida();

        // Mientras siga hundida en el vacío (todavía no le toca emerger) no puede detectar
        // al jugador ni mostrar su pregunta: físicamente no está ahí todavía.
        if (respondida || !yaEmergida || colapsadaPorTiempo) return;

        float distancia = Vector3.Distance(transform.position, playerObjeto.transform.position);

        if (distancia <= distanciaDeActivacion && !jugadorEncima)
        {
            jugadorEncima = true;

            if (esPisoInicial && !avisoBienvenidaDicho)
            {
                avisoBienvenidaDicho = true;
                AvisarBuenaSuerte();
            }

            // Ya no se congela al jugador al abrir la pregunta: puede seguir caminando
            // (y por lo tanto arriesgarse a caer si el piso de atrás ya colapsó) mientras decide.
            DesplegarPreguntaEnUI();
            IniciarCronometro();
        }

        ActualizarCronometro();
    }

    // ------------------------------------------------------------------
    // CRONÓMETRO: tiempo para responder. Si llega a 0, la baldosa se cae.
    // ------------------------------------------------------------------

    private void IniciarCronometro()
    {
        tiempoRestante = tiempoParaResponder;
        cronometroCorriendo = tiempoParaResponder > 0f;
        if (cronometroCorriendo && panelHolograma != null) panelHolograma.MostrarCronometro(tiempoRestante);
    }

    private void ActualizarCronometro()
    {
        // Mientras se resalta la opción elegida el tiempo se congela (ya respondiste).
        if (!cronometroCorriendo || !jugadorEncima || resolviendoSeleccion) return;

        tiempoRestante -= Time.deltaTime;
        if (panelHolograma != null) panelHolograma.MostrarCronometro(tiempoRestante);

        if (tiempoRestante <= 0f) TiempoAgotado();
    }

    /// <summary>Se acabó el tiempo sin responder: la baldosa se hunde con el jugador encima.</summary>
    private void TiempoAgotado()
    {
        cronometroCorriendo = false;
        colapsadaPorTiempo = true;

        if (panelHolograma != null) panelHolograma.Ocultar();
        else PanelOpcionesMirada.Ocultar();

        StopAllCoroutines();
        StartCoroutine(CorutinaColapsar());
    }

    // ------------------------------------------------------------------
    // CAÍDA: si el jugador cae bajo la última baldosa que pisó, reaparece en ella.
    // ------------------------------------------------------------------

    private void RevisarPisadaYCaida()
    {
        Vector3 jugador = playerObjeto.transform.position;

        bool estaPisable = yaEmergida && colisionadorPropio != null && colisionadorPropio.enabled;
        if (estaPisable && Vector3.Distance(transform.position, jugador) <= distanciaDeActivacion)
        {
            ultimaPisada = this;
        }

        if (ultimaPisada == this && jugador.y < posicionFinal.y - alturaCaidaParaReaparecer)
        {
            Reaparecer();
        }
    }

    private void Reaparecer()
    {
        ultimaPisada = null;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegistrarGastoComputacional(costoCaidaRefri, costoCaidaAgua, mensajeCaida);

            // Si con esta caída se acabó el agua, la caída ES el final: no se reaparece.
            if (GameManager.Instance.JuegoTerminado) return;
        }

        // Si esta baldosa ya estaba respondida (te caíste por quedarte encima cuando colapsó),
        // reapareces en la siguiente, que es donde tenías que ir. Si no, en esta misma.
        BaldosaPregunta destino = this;
        if (respondida && siguienteBaldosa != null && siguienteBaldosa.yaEmergida) destino = siguienteBaldosa;

        destino.RestaurarParaReaparecer();
        destino.ColocarJugadorEncima();
    }

    /// <summary>Vuelve a armar la baldosa en su sitio. Si no estaba respondida, la pregunta vuelve a aparecer con el cronómetro desde cero.</summary>
    private void RestaurarParaReaparecer()
    {
        StopAllCoroutines();
        transform.position = posicionFinal;
        yaEmergida = true;
        SetVisible(true);
        if (colisionadorPropio != null) colisionadorPropio.enabled = true;

        colapsadaPorTiempo = false;
        cronometroCorriendo = false;

        if (!respondida)
        {
            jugadorEncima = false;          // Update la vuelve a detectar y muestra la pregunta
            resolviendoSeleccion = false;
        }
        else
        {
            ProgramarPropioColapso();       // ya respondida: se vuelve a caer después del tiempo de siempre
        }
    }

    private void ColocarJugadorEncima()
    {
        if (playerObjeto == null) return;

        Vector3 pies = posicionFinal + Vector3.up * alturaSuperficie;
        PlayerMovement movimiento = playerObjeto.GetComponent<PlayerMovement>();
        if (movimiento != null) movimiento.TeletransportarPies(pies);
        else playerObjeto.transform.position = pies + Vector3.up * 1f;

        ultimaPisada = this;
    }

    private float CalcularAlturaSuperficie()
    {
        if (renderersPropios == null || renderersPropios.Length == 0) return 0f;

        float maxY = float.NegativeInfinity;
        foreach (Renderer r in renderersPropios)
        {
            if (r != null) maxY = Mathf.Max(maxY, r.bounds.max.y);
        }
        return float.IsInfinity(maxY) ? 0f : maxY - transform.position.y;
    }

    /// <summary>
    /// Se llama cuando el jugador elige una opción con la mirada + un botón (panel holográfico o, si
    /// no hay panel, el panel flotante de respaldo). 0..2 = opciones, 3 = Responder con IA.
    /// </summary>
    private void AlElegirConMirada(int indice)
    {
        if (!jugadorEncima || resolviendoSeleccion || respondida) return;

        if (indice == 3) SeleccionarOpcionIA();
        else SeleccionarOpcion(indice);
    }

    /// <summary>
    /// Se llama al elegir una opción: si hay panel holográfico asignado, primero lo ilumina
    /// (feedback visual) y recién cuando termina esa animación se resuelve de verdad la
    /// respuesta con EvaluarRespuesta. Sin panel asignado, resuelve al toque como antes.
    /// </summary>
    private void SeleccionarOpcion(int indiceOpcion)
    {
        resolviendoSeleccion = true;

        if (panelHolograma != null)
            panelHolograma.Resaltar(indiceOpcion, () => EvaluarRespuesta(indiceOpcion));
        else
            EvaluarRespuesta(indiceOpcion);
    }

    /// <summary>Mismo truco que SeleccionarOpcion pero para "Responder con IA".</summary>
    private void SeleccionarOpcionIA()
    {
        resolviendoSeleccion = true;

        if (panelHolograma != null)
            panelHolograma.Resaltar(3, UsarIA);
        else
            UsarIA();
    }

    /// <summary>Sólo se llama en el Piso 1: el robot desea suerte.</summary>
    private void AvisarBuenaSuerte()
    {
        if (NubeDialogoBot.Instancia == null) return;

        ReproducirBip();

        // Ya no se piden iniciales: si en la escena quedó guardado el texto viejo con "{0}",
        // se le quita para que no aparezca tal cual.
        string mensaje = (textoRobotBuenaSuerte ?? "").Replace(", {0}", "").Replace("{0}", "").Trim();
        if (string.IsNullOrEmpty(mensaje)) mensaje = "¡Mucha suerte!!";

        NubeDialogoBot.Instancia.Mostrar(mensaje, duracionAvisoRobot);
    }

    /// <summary>Mismo truco que en ControladorActo1: pitch al azar para que suene "distorsionado".</summary>
    private void ReproducirBip()
    {
        if (fuenteAudioBip == null || clipBip == null) return;
        fuenteAudioBip.pitch = Random.Range(0.6f, 1.3f);
        fuenteAudioBip.PlayOneShot(clipBip);
    }

    private void DesplegarPreguntaEnUI()
    {
        if (panelHolograma != null)
        {
            panelHolograma.Mostrar(enunciadoPregunta, textoOpciones, AlElegirConMirada);
            return;
        }

        // Respaldo por si todavía no arrastraste el PanelHolografico en el Inspector: la
        // pregunta va en la terminal y las opciones en el panel flotante de la mirada.
        TMPro.TMP_FontAsset fuente = null;
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            GameManager.Instance.uiManager.MostrarMensajeTerminal(enunciadoPregunta);
            if (GameManager.Instance.uiManager.textoTerminal != null)
                fuente = GameManager.Instance.uiManager.textoTerminal.font;
        }

        string[] opciones = { textoOpciones[0], textoOpciones[1], textoOpciones[2], "Responder con IA" };
        PanelOpcionesMirada.Mostrar(opciones, fuente, 52f, AlElegirConMirada);
    }

    // Delega la respuesta a la IA: avanza sin pensar, a costa de un consumo de agua mayor al de cualquier opción manual
    private void UsarIA()
    {
        respondida = true;
        jugadorEncima = false;
        cronometroCorriendo = false;

        if (panelHolograma != null) panelHolograma.Ocultar();
        else PanelOpcionesMirada.Ocultar();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UsarBotonIA();
        }

        CongelarJugador(false);

        // El camino sigue formándose independientemente de si se acertó o no: lo único que
        // cambia con el acierto es el costo de refrigeración/agua, nunca si puedes avanzar.
        EmergerSiguiente();
        ProgramarPropioColapso();
        AvisarSiEsLaUltima();
    }

    private void EvaluarRespuesta(int indiceOpcion)
    {
        // Marcamos como respondida inmediatamente para que NUNCA te vuelva a preguntar lo mismo
        respondida = true;
        jugadorEncima = false;
        cronometroCorriendo = false;

        if (panelHolograma != null) panelHolograma.Ocultar();
        else PanelOpcionesMirada.Ocultar();

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegistrarRespuesta(
                costoRefri[indiceOpcion], 
                costoAgua[indiceOpcion], 
                mensajeResultado[indiceOpcion],
                indiceOpcion == indiceCorrecta
            );
        }

        // Te libera sí o sí para que sigas caminando, sin importar si te equivocaste o no
        CongelarJugador(false);

        // Igual que arriba: la siguiente baldosa emerge sí o sí, acertar solo abarata el costo.
        EmergerSiguiente();
        ProgramarPropioColapso();
        AvisarSiEsLaUltima();
    }

    /// <summary>
    /// Si esta baldosa es la última del camino (no tiene "siguienteBaldosa" asignada), avisa
    /// al GameManager apenas se responde su pregunta: es la señal real de "llegaste a la
    /// salida", en vez de contar cuántas respuestas fueron correctas.
    /// </summary>
    private void AvisarSiEsLaUltima()
    {
        if (siguienteBaldosa == null && GameManager.Instance != null)
        {
            GameManager.Instance.TerminarNivelConExito();
        }
    }

    private void EmergerSiguiente()
    {
        if (siguienteBaldosa != null)
        {
            siguienteBaldosa.Emerger();
        }
    }

    /// <summary>
    /// Hace que esta baldosa suba desde el vacío hasta su posición final. La llama la baldosa
    /// anterior en cuanto el jugador responde (bien o mal), para que el camino se vaya
    /// formando a medida que se avanza.
    /// </summary>
    public void Emerger()
    {
        if (yaEmergida) return;
        yaEmergida = true;

        SetVisible(true);
        StopCoroutine(nameof(CorutinaEmerger));
        StartCoroutine(CorutinaEmerger());
    }

    private IEnumerator CorutinaEmerger()
    {
        Vector3 posicionInicial = transform.position;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionEmersion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tiempoTranscurrido / duracionEmersion);
            transform.position = Vector3.Lerp(posicionInicial, posicionFinal, t);
            yield return null;
        }

        transform.position = posicionFinal;

        // La colisión se habilita recién al llegar arriba: mientras sube, todavía no se puede
        // pisar (si se habilitara antes, el jugador podría "montarse" a mitad de la subida).
        if (colisionadorPropio != null) colisionadorPropio.enabled = true;
    }

    /// <summary>
    /// Programa que ESTA baldosa colapse un rato después de haber sido respondida: así el
    /// camino no queda "congelado" para siempre detrás tuyo, y si te demoras demasiado en
    /// avanzar, el piso puede desaparecer bajo tus pies.
    /// </summary>
    private void ProgramarPropioColapso()
    {
        StartCoroutine(CorutinaEsperarYColapsar());
    }

    private IEnumerator CorutinaEsperarYColapsar()
    {
        yield return new WaitForSeconds(tiempoAntesDeColapsar);
        yield return CorutinaColapsar();
    }

    private IEnumerator CorutinaColapsar()
    {
        // Se apaga la colisión de entrada: si alguien sigue parado encima, empieza a caer de
        // verdad junto con el piso, en vez de quedar flotando sobre un colisionador fantasma.
        if (colisionadorPropio != null) colisionadorPropio.enabled = false;

        Vector3 posicionInicial = transform.position;
        Vector3 posicionDestino = posicionInicial - Vector3.up * profundidadColapso;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionColapso)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionColapso;
            transform.position = Vector3.Lerp(posicionInicial, posicionDestino, t);
            yield return null;
        }

        transform.position = posicionDestino;
        SetVisible(false);
    }

    private void SetVisible(bool visible)
    {
        if (renderersPropios == null) return;
        foreach (Renderer r in renderersPropios)
        {
            if (r != null) r.enabled = visible;
        }
    }

    private void CongelarJugador(bool congelar)
    {
        if (playerObjeto == null) return;

        if (scriptMovimientoPlayer == null)
        {
            scriptMovimientoPlayer = playerObjeto.GetComponent<PlayerMovement>();
            if (scriptMovimientoPlayer == null) scriptMovimientoPlayer = playerObjeto.GetComponent("PlayerController") as MonoBehaviour;
            if (scriptMovimientoPlayer == null) scriptMovimientoPlayer = playerObjeto.GetComponent("FirstPersonController") as MonoBehaviour;
        }

        if (scriptMovimientoPlayer != null) scriptMovimientoPlayer.enabled = !congelar;
    }
}

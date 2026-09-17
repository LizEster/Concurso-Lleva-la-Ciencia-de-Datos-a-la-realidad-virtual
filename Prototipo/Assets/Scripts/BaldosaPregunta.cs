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
    private bool respondida = false;
    public bool EstaRespondida => respondida;
    private GameObject playerObjeto;
    private MonoBehaviour scriptMovimientoPlayer;

    private Collider colisionadorPropio;
    private Renderer[] renderersPropios;
    private Vector3 posicionFinal;
    private bool yaEmergida;

    void Start()
    {
        AsignarPreguntaDelBanco();
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null) col.isTrigger = false;

        colisionadorPropio = GetComponent<Collider>();
        renderersPropios = GetComponentsInChildren<Renderer>();
        posicionFinal = transform.position;

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
        // Mientras siga hundida en el vacío (todavía no le toca emerger) no puede detectar
        // al jugador ni mostrar su pregunta: físicamente no está ahí todavía.
        if (respondida || playerObjeto == null || !yaEmergida) return;

        float distancia = Vector3.Distance(transform.position, playerObjeto.transform.position);

        if (distancia <= distanciaDeActivacion && !jugadorEncima)
        {
            jugadorEncima = true;
            // Ya no se congela al jugador al abrir la pregunta: puede seguir caminando
            // (y por lo tanto arriesgarse a caer si el piso de atrás ya colapsó) mientras decide.
            DesplegarPreguntaEnUI();
        }
    }

    void OnGUI()
    {
        if (jugadorEncima && !respondida)
        {
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
                    EvaluarRespuesta(0);
                else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
                    EvaluarRespuesta(1);
                else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
                    EvaluarRespuesta(2);
                else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4)
                    UsarIA();
            }
        }
    }

    private void DesplegarPreguntaEnUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string textoCompleto = $"{enunciadoPregunta}\n\n" +
                                   $"[1] {textoOpciones[0]}\n" +
                                   $"[2] {textoOpciones[1]}\n" +
                                   $"[3] {textoOpciones[2]}\n" +
                                   $"[4] Responder con IA";

            GameManager.Instance.uiManager.MostrarMensajeTerminal(textoCompleto);
        }
    }

    // Delega la respuesta a la IA: avanza sin pensar, a costa de un consumo de agua mayor al de cualquier opción manual
    private void UsarIA()
    {
        respondida = true;
        jugadorEncima = false;

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

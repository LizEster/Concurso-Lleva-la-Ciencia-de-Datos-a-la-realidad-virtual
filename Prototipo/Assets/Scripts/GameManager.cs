using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Métricas del Data Center")]
    public float refrigeracion = 100f; 
    public float aguaConsumidaLitros = 0f;
    private bool juegoTerminado = false;
    /// <summary>True cuando el juego ya terminó (colapso térmico o éxito): no se reaparece al caer.</summary>
    public bool JuegoTerminado => juegoTerminado;

    [Header("Referencias de UI")]
    public UIManager uiManager;
    public NivelAgua nivelAgua;
    [Tooltip("Muestra los resultados de cada pregunta/caída como aviso holográfico en el borde superior de la vista (en vez del texto blanco de la terminal).")]
    public bool usarAvisosVisuales = true;

    [Header("Referencias del Mapa (Túnel Fucsia)")]
    [Tooltip("Arrastra aquí tu objeto 'Suelo_Tunel_Fucsia' desde la jerarquía")]
    public GameObject sueloTunelFucsia;
    [Tooltip("La puerta u objeto final que se abre al ganar")]
    public GameObject puertaFinalOficina;

    [Header("Caídas")]
    [Tooltip("Caídas permitidas en total (por tiempo o por accidente). Al llegar a este número se termina el juego y se pasa a la escena final.")]
    public int caidasMaximas = 3;
    [Tooltip("Segundos que se le quitan al tiempo para responder por cada caída.")]
    public float segundosMenosPorCaida = 5f;
    [Tooltip("El tiempo para responder nunca baja de esto.")]
    public float tiempoMinimoParaResponder = 5f;
    [Tooltip("Segundos de caída antes de pasar a la escena final cuando se acaban las caídas.")]
    public float segundosDeCaidaFinal = 2f;
    [TextArea(1, 3)]
    public string mensajeSinCaidas = "<color=red>> ERROR 404: Usuario perdido en el vacío.</color>\n> Demasiadas caídas: el sistema perdió tu rastro.";

    /// <summary>Cuántas veces se ha caído el jugador en esta partida.</summary>
    public int Caidas { get; private set; }

    [Header("Colapso térmico (sin reservas)")]
    [Tooltip("Segundos de caída libre antes de pasar a la escena final.")]
    public float segundosDeCaida = 3.5f;
    [Tooltip("Nombre de la escena final (la misma del final bueno).")]
    public string escenaFinal = "final1";
    [TextArea(1, 3)]
    public string mensajeColapso = "<color=red>> ERROR 508: Reservas agotadas. Colapso térmico.</color>\n> El puente de datos se desintegra...";

    void Awake()
    {
        if (Instance == null) 
        {
            Instance = this;
            DatosFinales.Reiniciar(refrigeracion); // partida nueva: se limpia el registro de decisiones
        }
        else 
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (uiManager != null)
        {
            uiManager.ActualizarMétricas(refrigeracion, aguaConsumidaLitros);
        }

        if (nivelAgua != null)
        {
            nivelAgua.ActualizarNivel(refrigeracion);
        }
    }

    /// <summary>
    /// Descuenta refrigeración/agua y avisa al jugador. Si viene 'tipo' (respuesta, IA o caída),
    /// el mensaje sale como AvisoSistema (holograma en el borde superior de la vista) en vez
    /// del texto blanco de la terminal.
    /// </summary>
    public void RegistrarGastoComputacional(float costoRefrigeracion, float litrosAgua, string mensaje, AvisoSistema.Tipo? tipo = null)
    {
        if (juegoTerminado) return;

        refrigeracion -= costoRefrigeracion;
        aguaConsumidaLitros += litrosAgua;

        refrigeracion = Mathf.Clamp(refrigeracion, 0f, 100f);

        if (uiManager != null)
        {
            uiManager.ActualizarMétricas(refrigeracion, aguaConsumidaLitros);
            if (tipo.HasValue && usarAvisosVisuales) uiManager.MostrarMensajeTerminal(""); // limpia el texto blanco viejo
            else uiManager.MostrarMensajeTerminal(mensaje);
        }
        if (tipo.HasValue && usarAvisosVisuales)
        {
            AvisoSistema.Mostrar(tipo.Value, mensaje, costoRefrigeracion, litrosAgua);
        }
        if (nivelAgua != null)
        {
            nivelAgua.ActualizarNivel(refrigeracion);
        }
        if (refrigeracion <= 0f)
        {
            StartCoroutine(ProcesarColapsoTermico());
        }
    }

    public void RegistrarRespuesta(float costoRefrigeracion, float litrosAgua, string mensaje, bool esCorrecta)
    {
        AvisoSistema.Tipo tipo = esCorrecta ? AvisoSistema.Tipo.Correcto
            : (mensaje != null && mensaje.ToLowerInvariant().Contains("grave")) ? AvisoSistema.Tipo.ErrorGrave
            : AvisoSistema.Tipo.Error;
        RegistrarGastoComputacional(costoRefrigeracion, litrosAgua, mensaje, tipo);
    }

    public void UsarBotonIA()
    {
        RegistrarGastoComputacional(DatosFinales.CostoIARefri, DatosFinales.CostoIAAgua, "> Respuesta generada automáticamente.\nMayor consumo computacional detectado por delegar razonamiento.", AvisoSistema.Tipo.IA);
    }

    /// <summary>
    /// Lo llama BaldosaPregunta cada vez que el jugador se cae. Devuelve true si todavía puede
    /// reaparecer; en la última caída permitida termina el juego (escena final) y devuelve false.
    /// </summary>
    public bool RegistrarCaida()
    {
        if (juegoTerminado) return false;

        Caidas++;
        if (Caidas >= caidasMaximas)
        {
            StartCoroutine(ProcesarFinPorCaidas());
            return false;
        }
        return true;
    }

    /// <summary>Tiempo para responder ya descontando las caídas (5 s menos por caída, por defecto).</summary>
    public float TiempoParaResponder(float tiempoBase)
    {
        if (tiempoBase <= 0f) return 0f; // 0 = sin límite
        return Mathf.Max(tiempoMinimoParaResponder, tiempoBase - Caidas * segundosMenosPorCaida);
    }

    // ------------------------------------------------------------------
    // FINALES CON CAÍDA (colapso o demasiadas caídas): todo pegado a los ojos, para que en
    // el visor se vea bien aunque el jugador vaya cayendo rápido.
    // ------------------------------------------------------------------

    private MensajeCentral mensajeFin;
    private bool cargandoFinal;

    /// <summary>
    /// Alarma mientras caes: la vista late de un color (rojo = colapso, morado = perdido),
    /// y un mensaje grande se "decodifica" delante de los ojos.
    /// </summary>
    private IEnumerator AlarmaDeCaida(Color color, string titulo, string subtitulo)
    {
        if (uiManager != null) uiManager.MostrarMensajeTerminal(""); // nada de texto blanco suelto
        AvisoSistema.OcultarYa();

        mensajeFin = MensajeCentral.Crear(titulo, subtitulo, MensajeCentral.Icono.Ninguno, 0f, color, true);
        StartCoroutine(mensajeFin.Aparecer(0.15f));

        Color tinte = new Color(color.r * 0.55f, color.g * 0.15f, color.b * 0.2f + (color.b > 0.8f ? 0.4f : 0f));
        float t = 0f;
        while (!cargandoFinal)
        {
            t += Time.deltaTime;
            // Latido de alarma: dos golpes rápidos y una pausa, como un corazón.
            float fase = Mathf.Repeat(t, 1.1f);
            float golpe = Mathf.Max(Mathf.Exp(-Mathf.Pow((fase - 0.1f) * 14f, 2f)), 0.7f * Mathf.Exp(-Mathf.Pow((fase - 0.35f) * 14f, 2f)));
            VeloNegro.Poner(tinte, 0.15f + 0.35f * golpe);
            yield return null;
        }
    }

    /// <summary>Dos destellos suaves del color del resultado, como una onda de energía que vuelve al sistema.</summary>
    private IEnumerator OndasDeEnergia(Color color)
    {
        Color tinte = new Color(color.r, color.g, color.b, 1f);
        for (int i = 0; i < 2 && !cargandoFinal; i++)
        {
            float t = 0f;
            while (t < 0.75f && !cargandoFinal)
            {
                t += Time.deltaTime;
                float a = t < 0.15f ? Mathf.Lerp(0f, 0.4f, t / 0.15f) : Mathf.Lerp(0.4f, 0f, (t - 0.15f) / 0.6f);
                VeloNegro.Poner(tinte, a);
                yield return null;
            }
            yield return new WaitForSeconds(0.25f);
        }
        if (!cargandoFinal) VeloNegro.Poner(tinte, 0f);
    }

    private IEnumerator ProcesarFinPorCaidas()
    {
        juegoTerminado = true;
        Debug.Log("<color=red>> FIN: demasiadas caídas.</color>");

        PanelOpcionesMirada.Ocultar();
        StartCoroutine(AlarmaDeCaida(new Color(0.75f, 0.5f, 1f, 1f), "CONEXIÓN PERDIDA",
            "Demasiadas caídas: el sistema perdió tu rastro en el vacío…"));

        // El jugador ya va cayendo: lo dejamos caer un poco más y pasamos al final.
        yield return new WaitForSeconds(segundosDeCaidaFinal);

        DatosFinales.colapsoTermico = false;
        DatosFinales.demasiadasCaidas = true;
        IrAEscenaFinal();
    }

    /// <summary>
    /// Se acabaron las reservas: el puente se cae (todas las baldosas colapsan, el jugador cae
    /// al vacío unos segundos) y después se pasa a la escena final, con los textos de colapso.
    /// </summary>
    private IEnumerator ProcesarColapsoTermico()
    {
        juegoTerminado = true;
        Debug.Log("<color=red>> COLAPSO TÉRMICO: el puente de datos se cae.</color>");

        PanelOpcionesMirada.Ocultar();
        StartCoroutine(AlarmaDeCaida(new Color(1f, 0.3f, 0.3f, 1f), "COLAPSO TÉRMICO",
            "Reservas de agua: <color=#FF4D4D><b>0%</b></color>\nEl sistema se sobrecalentó y el puente de datos se desintegra…"));

        // Se caen todas las baldosas (y el suelo del túnel, si está asignado).
        foreach (BaldosaPregunta baldosa in FindObjectsByType<BaldosaPregunta>(FindObjectsSortMode.None))
        {
            baldosa.ColapsarPorFinDelJuego();
        }

        if (sueloTunelFucsia != null)
        {
            foreach (Collider colisionador in sueloTunelFucsia.GetComponentsInChildren<Collider>())
            {
                colisionador.enabled = false;
            }
        }

        yield return new WaitForSeconds(segundosDeCaida);

        DatosFinales.colapsoTermico = true;
        DatosFinales.demasiadasCaidas = false;
        IrAEscenaFinal();
    }

    /// <summary>Guarda los datos y pasa a la escena final con el destello blanco (igual en los dos finales).</summary>
    private void IrAEscenaFinal()
    {
        if (cargandoFinal) return;
        StartCoroutine(EncandilarYCargar());
    }

    /// <summary>
    /// La vista se pone BLANCA (encandilamiento) con un velo pegado a los ojos y recién ahí
    /// se carga la escena final. Antes se usaba el panel blanco del Canvas (TransicionLuz),
    /// pero en el visor ese panel seguía la cabeza con retraso y, al caer, quedaba arriba
    /// como una "pared blanca".
    /// </summary>
    private IEnumerator EncandilarYCargar()
    {
        cargandoFinal = true;
        DatosFinales.aguaConsumida = aguaConsumidaLitros;
        DatosFinales.refrigeracionRestante = refrigeracion;

        if (mensajeFin != null) StartCoroutine(mensajeFin.Desaparecer(1f));
        yield return VeloNegro.FundirColor(Color.white, VeloNegro.AlfaActual, 1f, 1.5f);
        yield return new WaitForSeconds(0.3f);

        UnityEngine.SceneManagement.SceneManager.LoadScene(escenaFinal);
    }

    public void TerminarNivelConExito()
    {
        if (juegoTerminado) return;
        if (refrigeracion > 0f)
        {
            StartCoroutine(ProcesarTransicionExito());
        }
    }

    private IEnumerator ProcesarTransicionExito()
    {
        juegoTerminado = true;
        Debug.Log("> FIN DEL PROCESAMIENTO: Iniciando transición a escena final.");

        // ---------- ¡Celebración! ----------
        if (uiManager != null) uiManager.MostrarMensajeTerminal("");
        AvisoSistema.OcultarYa();
        PanelOpcionesMirada.Ocultar();

        // Color y título según cómo llegaste (igual que los finales).
        Color color; string titulo;
        if (refrigeracion >= 60f) { color = new Color(0.25f, 1f, 0.45f, 1f); titulo = "SISTEMA RESTAURADO"; }
        else if (refrigeracion >= 25f) { color = new Color(1f, 0.85f, 0.2f, 1f); titulo = "PUENTE COMPLETADO"; }
        else { color = new Color(1f, 0.55f, 0.15f, 1f); titulo = "COMPLETADO... APENAS"; }

        int total = DatosFinales.decisiones.Count, correctas = 0;
        foreach (DatosFinales.Decision d in DatosFinales.decisiones) if (d.elegida == d.correcta) correctas++;

        // 1) Dos ondas de energía del color del resultado recorren la vista.
        StartCoroutine(OndasDeEnergia(color));

        // 2) El robot, ya sano, te da las gracias.
        if (NubeDialogoBot.Instancia != null)
            NubeDialogoBot.Instancia.Mostrar(refrigeracion >= 25f
                ? "¡Lo lograste! El puente está completo y mis sistemas vuelven a respirar. ¡Gracias por pensar por ti misma/o!"
                : "Llegamos… por muy poco. Mis reservas casi se secan, pero lo logramos.", 6f);

        // 3) Mensaje grande que se decodifica delante de los ojos.
        yield return new WaitForSeconds(0.4f);
        mensajeFin = MensajeCentral.Crear(titulo,
            $"Cruzaste las <b>{total}</b> preguntas · <b>{correctas}</b> correctas\n" +
            $"Refrigeración restante: <b>{refrigeracion:0}%</b>\n<color=#33E6FF>La salida se abre…</color>",
            MensajeCentral.Icono.Ninguno, 0f, color, true);
        StartCoroutine(mensajeFin.Aparecer(0.3f));

        // 4) Se abre la puerta y, tras unos segundos, la luz blanca te lleva al final.
        yield return new WaitForSeconds(1.2f);
        if (puertaFinalOficina != null)
        {
            puertaFinalOficina.SetActive(false);
        }

        yield return new WaitForSeconds(3.8f);

        // Guarda los datos y carga la escena final (final1).
        DatosFinales.colapsoTermico = false;
        DatosFinales.demasiadasCaidas = false;
        IrAEscenaFinal();
    }
}

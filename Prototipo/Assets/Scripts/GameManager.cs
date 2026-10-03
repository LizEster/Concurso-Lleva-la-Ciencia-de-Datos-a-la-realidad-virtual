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

    public void RegistrarGastoComputacional(float costoRefrigeracion, float litrosAgua, string mensaje)
    {
        if (juegoTerminado) return;

        refrigeracion -= costoRefrigeracion;
        aguaConsumidaLitros += litrosAgua;

        refrigeracion = Mathf.Clamp(refrigeracion, 0f, 100f);

        if (uiManager != null)
        {
            uiManager.ActualizarMétricas(refrigeracion, aguaConsumidaLitros);
            uiManager.MostrarMensajeTerminal(mensaje);
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
        RegistrarGastoComputacional(costoRefrigeracion, litrosAgua, mensaje);
    }

    public void UsarBotonIA()
    {
        RegistrarGastoComputacional(35f, 2.5f, "> Respuesta generada automáticamente.\nMayor consumo computacional detectado por delegar razonamiento.");
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

    private IEnumerator ProcesarFinPorCaidas()
    {
        juegoTerminado = true;
        Debug.Log("<color=red>> FIN: demasiadas caídas.</color>");

        if (uiManager != null) uiManager.MostrarMensajeTerminal(mensajeSinCaidas);
        PanelOpcionesMirada.Ocultar();

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

        if (uiManager != null) uiManager.MostrarMensajeTerminal(mensajeColapso);
        PanelOpcionesMirada.Ocultar();

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
        TransicionLuz transicion = FindAnyObjectByType<TransicionLuz>();
        if (transicion != null)
        {
            transicion.nombreEscenaFinal = escenaFinal;
            transicion.IniciarTransicion(aguaConsumidaLitros, refrigeracion);
            return;
        }

        DatosFinales.aguaConsumida = aguaConsumidaLitros;
        DatosFinales.refrigeracionRestante = refrigeracion;
        if (ScreenFader.Instance != null) ScreenFader.Instance.TransicionarAEscena(escenaFinal);
        else UnityEngine.SceneManagement.SceneManager.LoadScene(escenaFinal);
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

        if (puertaFinalOficina != null)
        {
            puertaFinalOficina.SetActive(false);
        }

        yield return new WaitForSeconds(3f);

        // Guarda los datos y carga la escena final (final1).
        DatosFinales.colapsoTermico = false;
        DatosFinales.demasiadasCaidas = false;
        IrAEscenaFinal();
    }
}

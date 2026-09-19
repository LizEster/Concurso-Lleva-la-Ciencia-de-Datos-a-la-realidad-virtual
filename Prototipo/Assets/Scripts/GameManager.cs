using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Métricas del Data Center")]
    public float refrigeracion = 100f; 
    public float aguaConsumidaLitros = 0f;
    private bool juegoTerminado = false;

    [Header("Iniciales del jugador")]
    [Tooltip("Iniciales que el jugador escribió en el panel de la terminal (TerminalInteractiva). Se guardan aquí para poder usarlas después, por ejemplo cuando el robot desea suerte al llegar al Piso 1.")]
    public string inicialesJugador = "";

    [Header("Referencias de UI")]
    public UIManager uiManager;
    public NivelAgua nivelAgua;

    [Header("Referencias del Mapa (Túnel Fucsia)")]
    [Tooltip("Arrastra aquí tu objeto 'Suelo_Tunel_Fucsia' desde la jerarquía")]
    public GameObject sueloTunelFucsia;
    [Tooltip("La puerta u objeto final que se abre al ganar")]
    public GameObject puertaFinalOficina;

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

        if (nivelAgua != null)          // ← nuevo
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
        if (nivelAgua != null)          // ← nuevo
        {
            nivelAgua.ActualizarNivel(refrigeracion);
        }
        if (refrigeracion <= 0f)
        {
            StartCoroutine(ProcesarColapsoTermico());
        }
    }

    // Usado por las baldosas de pregunta: registra el costo correspondiente a la opción
    // elegida (haya sido correcta o no). El avance del nivel YA NO depende de acertar ni de
    // ningún contador interno: quien decide cuándo se termina el nivel es físicamente la
    // última baldosa del camino (la que no tiene "siguienteBaldosa"), llamando directamente a
    // TerminarNivelConExito() en cuanto se responde. Antes, un contador de 3 respuestas
    // correctas abría la salida aunque todavía quedaran pisos por cruzar; ahora el final
    // llega exactamente cuando cruzas el último piso, sin importar cuántas acertaste,
    // mientras te quede refrigeración.
    public void RegistrarRespuesta(float costoRefrigeracion, float litrosAgua, string mensaje, bool esCorrecta)
    {
        RegistrarGastoComputacional(costoRefrigeracion, litrosAgua, mensaje);
    }

    public void UsarBotonIA()
    {
        RegistrarGastoComputacional(35f, 2.5f, "> Respuesta generada automáticamente.\nMayor consumo computacional detectado por delegar razonamiento.");
    }

    // CORRUTINA: Maneja la caída física en el túnel fucsia y espera 4 segundos antes del Game Over
    private IEnumerator ProcesarColapsoTermico()
    {
        juegoTerminado = true;
        Debug.Log("<color=red>> COLAPSO TÉRMICO: Desactivando colisiones del túnel fucsia.</color>");

        if (sueloTunelFucsia != null)
        {
            MeshCollider colliderPrincipal = sueloTunelFucsia.GetComponent<MeshCollider>();
            if (colliderPrincipal != null) colliderPrincipal.enabled = false;

            foreach (MeshCollider childCollider in sueloTunelFucsia.GetComponentsInChildren<MeshCollider>())
            {
                childCollider.enabled = false;
            }
        }

        // CAMBIAR AQUÍ: De 2f a 4f segundos de caída libre
        yield return new WaitForSeconds(4f);

        if (uiManager != null) 
        {
            uiManager.MostrarPantallaFinal(false, refrigeracion, aguaConsumidaLitros);
        }
    }

    /// <summary>
    /// La llama la última baldosa del camino (la que no tiene "siguienteBaldosa" asignada)
    /// apenas se responde su pregunta, sin importar si fue correcta o no. Si todavía queda
    /// refrigeración, dispara el final exitoso; si ya llegó a 0%, el colapso térmico ya se
    /// está encargando del final malo y este llamado no hace nada.
    /// </summary>
    public void TerminarNivelConExito()
    {
        if (juegoTerminado) return;
        if (refrigeracion > 0f)
        {
            StartCoroutine(ProcesarTransicionExito());
        }
    }

    // CORRUTINA: Abre el camino fucsia, espera 3 segundos en el destello antes del final de absorción
    private IEnumerator ProcesarTransicionExito()
    {
        juegoTerminado = true;
        Debug.Log("<color=white>> FIN DEL PROCESAMIENTO: Abriendo salida hacia la oficina.</color>");

        if (puertaFinalOficina != null)
        {
            puertaFinalOficina.SetActive(false); // Abre físicamente la compuerta
        }

        // Esperamos los 3 segundos dramáticos que planeaste para el fundido/corte
        yield return new WaitForSeconds(3f);

        if (uiManager != null) 
        {
            uiManager.MostrarPantallaFinal(true, refrigeracion, aguaConsumidaLitros);
        }
    }
}

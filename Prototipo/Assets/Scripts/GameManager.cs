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

        yield return new WaitForSeconds(4f);

        if (uiManager != null) 
        {
            uiManager.MostrarPantallaFinal(false, refrigeracion, aguaConsumidaLitros);
        }
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

        // AQUÍ ESTÁ EL CAMBIO: en vez de mostrar la pantalla final vieja,
        // guarda los datos y carga la escena final1
        TransicionLuz transicion = FindAnyObjectByType<TransicionLuz>();
        if (transicion != null)
        {
            transicion.IniciarTransicion(aguaConsumidaLitros, refrigeracion);
        }
    }
}

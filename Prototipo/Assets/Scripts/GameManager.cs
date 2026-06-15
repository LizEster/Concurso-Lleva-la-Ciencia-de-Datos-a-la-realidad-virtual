using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Métricas del Data Center")]
    public float refrigeracion = 100f; 
    public float aguaConsumidaLitros = 0f;
    private int desafioActual = 1;
    private bool juegoTerminado = false;

    [Header("Referencias de UI")]
    public UIManager uiManager;

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

        if (refrigeracion <= 0f)
        {
            StartCoroutine(ProcesarColapsoTermico());
        }
    }

    public void ResponderCorrecto()
    {
        RegistrarGastoComputacional(2f, 0.15f, "> Procesamiento eficiente. Conexiones vectoriales estables.");
        AvanzarDesafio();
    }

    public void ResponderIncorrecto()
    {
        RegistrarGastoComputacional(5f, 0.4f, "> Error de coherencia vectorial.\nProcesamiento adicional requerido. Reiniciando entorno...");
    }

    public void UsarBotonIA()
    {
        RegistrarGastoComputacional(35f, 2.5f, "> Respuesta generada automáticamente.\nMayor consumo computacional detectado por delegar razonamiento.");
        AvanzarDesafio();
    }

    private void AvanzarDesafio()
    {
        desafioActual++;
        if (desafioActual > 3)
        {
            LlegarAlFinal();
        }
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

    private void ActivarColapsoTermico()
    {
        // Esta función queda en desuso ya que ahora lo maneja la corrutina de arriba
    }

    private void LlegarAlFinal()
    {
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
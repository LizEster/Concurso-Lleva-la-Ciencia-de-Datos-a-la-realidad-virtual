using UnityEngine;
using System.Collections;

public class GestionActo2 : MonoBehaviour
{
    [Header("Referencias del Mapa")]
    public UIManager uiManager;
    [Tooltip("Arrastra aquí el objeto que agrupa los suelos del túnel fucsia")]
    public GameObject sueloTunelFucsia; 
    [Tooltip("La compuerta final al terminar el túnel fucsia")]
    public GameObject puertaFinalOficina; 

    [Header("Variables de Control Térmico")]
    public float refrigeracion = 100f;
    public float aguaEvaporada = 0f;

    private bool juegoTerminado = false;

    // Se ejecutará cada vez que el jugador pise una baldosa-pregunta en el túnel fucsia
    public void AplicarDecisionIA(float costoRefri, float costoAgua)
    {
        if (juegoTerminado) return;

        refrigeracion = Mathf.Max(0, refrigeracion - costoRefri);
        aguaEvaporada += costoAgua;

        // Actualizamos las métricas de la pantalla (HUD)
        if (uiManager != null)
        {
            uiManager.ActualizarMétricas(refrigeracion, aguaEvaporada);
        }

        // Si la refrigeración se agota, el túnel colapsa bajo sus pies
        if (refrigeracion <= 0)
        {
            StartCoroutine(ProcesarColapsoTunel());
        }
    }

    private IEnumerator ProcesarColapsoTunel()
    {
        juegoTerminado = true;
        Debug.Log("<color=red>> COLAPSO TÉRMICO: Desactivando colisiones del túnel fucsia.</color>");

        // El jugador cae al vacío: Desactivamos los Mesh Colliders del túnel fucsia
        if (sueloTunelFucsia != null)
        {
            // Apaga el collider del objeto principal (si tiene)
            MeshCollider colliderPrincipal = sueloTunelFucsia.GetComponent<MeshCollider>();
            if (colliderPrincipal != null) colliderPrincipal.enabled = false;

            // Apaga los colliders de todas las baldosas hijas dentro del túnel fucsia
            foreach (MeshCollider childCollider in sueloTunelFucsia.GetComponentsInChildren<MeshCollider>())
            {
                childCollider.enabled = false;
            }
        }

        // Esperamos 2 segundos de caída libre en la oscuridad total
        yield return new WaitForSeconds(2f);

        // Desplegamos la pantalla negra con la reflexión de la huella de carbono/agua
        if (uiManager != null)
        {
            uiManager.MostrarPantallaFinal(false, refrigeracion, aguaEvaporada);
        }
    }

    // Se llamará al pisar con éxito la última baldosa del túnel fucsia
    public void LlegadaAlFinalConExito()
    {
        if (juegoTerminado) return;
        StartCoroutine(ProcesarTransicionOficina());
    }

    private IEnumerator ProcesarTransicionOficina()
    {
        juegoTerminado = true;
        Debug.Log("<color=white>> FIN DEL PROCESAMIENTO: Iniciando transición a blanco.</color>");

        // Abrimos la puerta final si es necesario
        if (puertaFinalOficina != null) puertaFinalOficina.SetActive(false);

        // Esperamos 3 segundos en el destello blanco antes de mostrar las métricas finales de éxito
        yield return new WaitForSeconds(3f);

        if (uiManager != null)
        {
            uiManager.MostrarPantallaFinal(true, refrigeracion, aguaEvaporada);
            // Aquí activarás el fundido a blanco o el cambio a la escena de la oficina de servidores
        }
    }
}
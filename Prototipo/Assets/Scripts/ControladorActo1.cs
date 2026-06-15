using UnityEngine;
using System.Collections; // Necesario para usar los temporizadores (Coroutines)

public class ControladorActo1 : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public UIManager uiManager;
    public PuertaDataCenter puertaInicio; 

    private int mensajeActual = 0;
    private bool secuenciaTerminada = false;

    private string[] dialogosGuion = new string[]
    {
        "> Al parecer te has perdido…\n\n[ Presiona E para avanzar ]",
        "> Has caído en las instalaciones de procesamiento del lenguaje para Large Language Model.\n\n[ Presiona E para avanzar ]",
        "> Objetivo: Dirígete a la salida.\n\n[ Presiona E para avanzar ]",
        "> Para encontrar la salida, debes encontrar el camino. Responde las preguntas para salir con éxito.\nRecuerda que estás en el camino del procesamiento de las Large Language Model; para dar respuestas, deberás pensar como una IA.\n\n[ Presiona E para avanzar ]",
        "> ¿Estás preparada/o?\n\n[ Presiona E para ACEPTAR y abrir la compuerta ]"
    };

    void Start()
    {
        if (uiManager != null)
        {
            uiManager.MostrarMensajeTerminal(dialogosGuion[0]);
        }
    }

    void Update()
    {
        if (secuenciaTerminada) return;

        if (UnityEngine.InputSystem.Keyboard.current != null && 
            UnityEngine.InputSystem.Keyboard.current.eKey.wasPressedThisFrame)
        {
            AvanzarSecuencia();
        }
    }

    private void AvanzarSecuencia()
    {
        mensajeActual++;

        if (mensajeActual < dialogosGuion.Length)
        {
            if (uiManager != null)
            {
                uiManager.MostrarMensajeTerminal(dialogosGuion[mensajeActual]);
            }
        }
        else
        {
            secuenciaTerminada = true;
            Debug.Log("> El jugador ha aceptado el desafío de la IA.");

            if (uiManager != null)
            {
                uiManager.ActivarVisorDataCenter();
                uiManager.MostrarMensajeTerminal("> Conexión establecida. Modo procesamiento activado. Avanza hacia el Lobby.");
                
                // NUEVO: Iniciamos el temporizador para borrar el texto después de 4.5 segundos
                StartCoroutine(BorrarTextoTerminal(4.5f));
            }

            if (puertaInicio != null)
            {
                puertaInicio.AbrirPuerta();
            }
        }
    }

    // NUEVO: Esta función espera los segundos que le digas y limpia la pantalla
    private IEnumerator BorrarTextoTerminal(float segundos)
    {
        yield return new WaitForSeconds(segundos);
        
        if (uiManager != null)
        {
            // Le mandamos un texto vacío al UIManager para limpiar la interfaz central
            uiManager.MostrarMensajeTerminal(""); 
            Debug.Log("> Interfaz de la terminal despejada para la exploración.");
        }
    }
}
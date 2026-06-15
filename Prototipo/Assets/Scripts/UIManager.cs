using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("Componentes de Texto UI")]
    public TextMeshProUGUI textoMétricas; // Mantenemos tu nombre original con tilde
    public TextMeshProUGUI textoTerminal;
    
    [Header("Pantallas Finales (Acto III)")]
    public GameObject panelFinal;
    public TextMeshProUGUI textoFinalExplicativo; // Mantenemos tu nombre original

    [Header("Efecto Terminal Retro")]
    [Tooltip("Arrastra aquí el componente AudioSource que reproducirá el clic")]
    public AudioSource fuenteAudioTeclado; 
    public float velocidadEscritura = 0.04f; // Tiempo en segundos entre cada letra

    private Coroutine corrutinaEscritura;

    void Start()
    {
        // ACTO I: Al aparecer en la sala negra, las métricas del visor inician apagadas
        if (textoMétricas != null) 
        {
            textoMétricas.gameObject.SetActive(false);
        }
        
        // Mensaje inicial del juego en la terminal con el nuevo efecto dinámico
        MostrarMensajeTerminal("Salón principal de Ciencia de Datos. Elige sobre qué deseas aprender.");
    }

    // Actualiza el indicador en tiempo real
    public void ActualizarMétricas(float refri, float agua)
    {
        if (textoMétricas != null)
        {
            textoMétricas.text = $"\nReservas de refrigeración: {refri:0}%\nAgua evaporada: {agua:0.2} L";
        }
    }

    // Activa el HUD visual cuando el jugador cae al datacenter (Acto II)
    public void ActivarVisorDataCenter()
    {
        if (textoMétricas != null) 
        {
            textoMétricas.gameObject.SetActive(true);
        }
    }

    // NUEVO: Cambia el texto usando la animación letra por letra
    public void MostrarMensajeTerminal(string mensaje)
    {
        if (textoTerminal != null) 
        {
            if (corrutinaEscritura != null)
            {
                StopCoroutine(corrutinaEscritura);
            }

            if (string.IsNullOrEmpty(mensaje))
            {
                textoTerminal.text = "";
            }
            else
            {
                corrutinaEscritura = StartCoroutine(EscribirTextoLetraPorLetra(mensaje));
            }
        }
    }

    // NUEVO: Corrutina que genera la animación y reproduce el sonido por cada carácter
    private IEnumerator EscribirTextoLetraPorLetra(string textoCompleto)
    {
        textoTerminal.text = ""; 

        // 1. ENCIENDE EL AUDIO AL INICIO: Como el sonido ya es un teclado escribiendo, 
        // lo reproducimos de fondo UNA SOLA VEZ al arrancar la frase.
        if (fuenteAudioTeclado != null && fuenteAudioTeclado.clip != null)
        {
            fuenteAudioTeclado.volume = 0.25f; // Lo dejamos sutil y de fondo
            fuenteAudioTeclado.Play();
        }

        foreach (char letra in textoCompleto.ToCharArray())
        {
            textoTerminal.text += letra; 
            yield return new WaitForSeconds(velocidadEscritura);
        }

        // 2. APAGA EL AUDIO AL TERMINAR: En cuanto se termina de escribir la última letra,
        // silenciamos el teclado ambiental para dar paso al silencio dramático.
        if (fuenteAudioTeclado != null)
        {
            fuenteAudioTeclado.Stop();
        }
    }

    // Despliega las conclusiones del Acto III según el rendimiento térmico
    public void MostrarPantallaFinal(bool exito, float refriRestante, float aguaTotal)
    {
        if (panelFinal != null) 
        {
            panelFinal.SetActive(true);
        }
        
        if (textoFinalExplicativo == null) return;
        
        if (exito)
        {
            textoFinalExplicativo.text = $"> OUTPUT GENERADO CON ÉXITO.\n" +
                $"Estado de refrigeración restante: {refriRestante:0}%\n\n" +
                $"Incluso siendo eficiente y usando tu propia deducción la mayor parte del tiempo, tu consulta evaporó {aguaTotal:0.2} litros de agua real. " +
                $"Mantener esta red viva tiene un costo físico inevitable.\n\n" +
                $"¿Sabías que entrenar grandes modelos de lenguaje en la vida real evapora cientos de miles de litros de agua?\n\n" +
                $"La Ciencia de Datos es una herramienta poderosa, pero cada vez que presionas 'Enter', el planeta paga una parte del precio.\n\n" +
                $"Úsala con conciencia.";
        }
        else
        {
            textoFinalExplicativo.text = $"<color=red>> ERROR 508: Límite de recursos excedido. Colapso térmico inminente.</color>\n\n" +
                $"Ya sea delegando todas las decisiones a la IA mediante el cálculo automático, o forzando al sistema a procesar errores continuos por falta de análisis, la sed del algoritmo secó las reservas por completo.\n\n" +
                $"Cada cálculo, cada error y cada prompt dentro de un LLM consume agua real de nuestro planeta para evitar que los componentes ardan.\n\n" +
                $"Entender cómo funciona esta tecnología no es solo técnica, es supervivencia.\n\n" +
                $"La próxima vez, calcula mejor tu huella.";
        }
    }
}
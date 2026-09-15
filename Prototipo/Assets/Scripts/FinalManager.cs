// FinalManager.cs
// Controla toda la secuencia del final en la escena "final1".
// Se coloca en un objeto vacío llamado "FinalManager" en la escena final.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FinalManager : MonoBehaviour
{
    [Header("Esfera de Agua Pequeña")]
    [Tooltip("Arrastra aquí la esfera de agua que ya existe en la escena")]
    public GameObject esferaAgua;

    [Tooltip("Distancia a la que el jugador activa la esfera")]
    public float distanciaActivacionEsfera = 5f;

    [Header("Esfera Gigante")]
    [Tooltip("Arrastra aquí la segunda esfera (la que crecerá)")]
    public GameObject esferaGigante;

    [Tooltip("Tamaño final de la esfera gigante")]
    public float tamanoFinalGigante = 30f;

    [Tooltip("Velocidad a la que crece la esfera gigante")]
    public float velocidadCrecimiento = 15f;

    [Tooltip("Segundos de pausa después de que la esfera termina de crecer antes del vacío negro")]
    public float pausaAntesDeNegro = 3f;

    [Header("UI - Mensaje de la Esfera")]
    [Tooltip("Texto que muestra los litros junto a la esfera")]
    public TextMeshProUGUI textoEsfera;

    [Header("UI - Pantalla Final (Vacío Negro)")]
    [Tooltip("Panel negro que cubre toda la pantalla para el final")]
    public Image panelNegro;

    [Tooltip("Texto donde aparecen los mensajes finales")]
    public TextMeshProUGUI textoFinal;

    [Header("UI - Transición de Entrada")]
    [Tooltip("Panel blanco para el fade de entrada a la escena")]
    public Image panelBlancoEntrada;

    [Tooltip("Duración del fade de entrada")]
    public float duracionFadeEntrada = 2f;

    // Estados internos
    private GameObject playerObjeto;
    private bool esferaActivada = false;
    private bool esferaGiganteCreciendo = false;
    private bool finalActivado = false;
    private bool mostrandoMensajes = false;
    private int mensajeActual = 0;
    private string[] mensajesFinales;

    void Start()
    {
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        float agua = DatosFinales.aguaConsumida;
        float refrigeracion = DatosFinales.refrigeracionRestante;

        mensajesFinales = new string[]
        {
            $"> Output Generado con éxito.\n> Estado de refrigeración restante: {refrigeracion:F0}%",
            $"> Incluso siendo eficiente y usando tu propia deducción la mayor parte del tiempo, tu consulta evaporó {agua:F2} litros de agua real.\n> Mantener esta red viva tiene un costo físico inevitable.\n> ¿Sabías que entrenar grandes modelos de lenguaje en la vida real evapora cientos de miles de litros?",
            "> La Ciencia de Datos es una herramienta poderosa, pero cada vez que presionas 'Enter', el planeta paga una parte del precio.",
            "> Úsala con conciencia."
        };

        if (esferaGigante != null)
        {
            esferaGigante.SetActive(false);
            esferaGigante.transform.localScale = Vector3.one * 1f;
        }

        if (textoEsfera != null)
            textoEsfera.gameObject.SetActive(false);

        if (panelNegro != null)
            panelNegro.gameObject.SetActive(false);

        if (textoFinal != null)
            textoFinal.gameObject.SetActive(false);

        if (panelBlancoEntrada != null)
        {
            panelBlancoEntrada.color = new Color(1f, 1f, 1f, 1f);
            StartCoroutine(FadeEntrada());
        }
    }

    void Update()
    {
        if (playerObjeto == null) return;

        if (!esferaActivada && esferaAgua != null)
        {
            float distancia = Vector3.Distance(playerObjeto.transform.position, esferaAgua.transform.position);
            if (distancia <= distanciaActivacionEsfera)
            {
                ActivarEsfera();
            }
        }

        if (esferaGiganteCreciendo && esferaGigante != null)
        {
            Vector3 escalaActual = esferaGigante.transform.localScale;
            Vector3 escalaObjetivo = Vector3.one * tamanoFinalGigante;

            esferaGigante.transform.localScale = Vector3.Lerp(escalaActual, escalaObjetivo, Time.deltaTime * velocidadCrecimiento * 0.1f);

            if (esferaGigante.transform.localScale.x >= tamanoFinalGigante * 0.95f)
            {
                esferaGigante.transform.localScale = escalaObjetivo;
                esferaGiganteCreciendo = false;

                // Automáticamente iniciar la transición al vacío negro
                if (!finalActivado)
                {
                    finalActivado = true;
                    StartCoroutine(EsperarYActivarFinal());
                }
            }
        }

        if (mostrandoMensajes)
        {
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                MostrarSiguienteMensaje();
            }
        }
    }

    private void ActivarEsfera()
    {
        esferaActivada = true;
        float agua = DatosFinales.aguaConsumida;

        if (textoEsfera != null)
        {
            textoEsfera.gameObject.SetActive(true);
            textoEsfera.text = $"Agua consumida al usar LLM de forma eficiente:\n{agua:F2} Litros";
        }

        StartCoroutine(EsperarYMostrarGigante());
    }
    
    private IEnumerator EsperarYMostrarGigante()
    {
        yield return new WaitForSeconds(4f);

        if (esferaGigante != null)
        {
            // Primero aparece estática al mismo tamaño que la otra
            esferaGigante.SetActive(true);

            // Espera 3 segundos para que el jugador la vea
            yield return new WaitForSeconds(3f);

            // Ahora empieza a crecer
            esferaGiganteCreciendo = true;
        }
    }

    private IEnumerator EsperarYActivarFinal()
    {
        // Pausa para que el jugador asimile la esfera gigante antes de que todo se ponga negro
        yield return new WaitForSeconds(pausaAntesDeNegro);
        StartCoroutine(TransicionAVacioNegro());
    }

    private IEnumerator TransicionAVacioNegro()
    {
        // Congelar al jugador
        MonoBehaviour movimiento = playerObjeto.GetComponent<PlayerMovement>();
        if (movimiento == null) movimiento = playerObjeto.GetComponent("FirstPersonController") as MonoBehaviour;
        if (movimiento != null) movimiento.enabled = false;

        // Ocultar el texto de la esfera
        if (textoEsfera != null)
            textoEsfera.gameObject.SetActive(false);

        // Fade a negro
        if (panelNegro != null)
        {
            panelNegro.gameObject.SetActive(true);
            panelNegro.color = new Color(0f, 0f, 0f, 0f);

            float tiempo = 0f;
            float duracion = 2f;

            while (tiempo < duracion)
            {
                tiempo += Time.deltaTime;
                float alpha = Mathf.Clamp01(tiempo / duracion);
                panelNegro.color = new Color(0f, 0f, 0f, alpha);
                yield return null;
            }
        }

        // Esperar un momento en negro antes de los mensajes
        yield return new WaitForSeconds(2f);

        // Empezar a mostrar mensajes
        if (textoFinal != null)
        {
            textoFinal.gameObject.SetActive(true);
            textoFinal.text = "";
        }

        mostrandoMensajes = true;
        MostrarSiguienteMensaje();
    }

    private void MostrarSiguienteMensaje()
    {
        if (mensajeActual < mensajesFinales.Length)
        {
            if (textoFinal != null)
            {
                StartCoroutine(EscribirMensajeGradual(mensajesFinales[mensajeActual]));
            }
            mensajeActual++;
        }
        else
        {
            mostrandoMensajes = false;
            StartCoroutine(FinalDelJuego());
        }
    }

    private IEnumerator EscribirMensajeGradual(string mensaje)
    {
        mostrandoMensajes = false;
        textoFinal.text = "";

        foreach (char letra in mensaje)
        {
            textoFinal.text += letra;
            yield return new WaitForSeconds(0.03f);
        }

        textoFinal.text += "\n\n[Presiona Espacio para continuar]";
        mostrandoMensajes = true;
    }

    private IEnumerator FinalDelJuego()
    {
        if (textoFinal != null)
        {
            textoFinal.text = "";
        }

        yield return new WaitForSeconds(2f);

        // Descomentar una de estas líneas según lo que quieran hacer al terminar:
        // SceneManager.LoadScene("MenuPrincipal");
        // Application.Quit();
    }

    private IEnumerator FadeEntrada()
    {
        float tiempo = 0f;

        while (tiempo < duracionFadeEntrada)
        {
            tiempo += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(tiempo / duracionFadeEntrada);
            panelBlancoEntrada.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        panelBlancoEntrada.gameObject.SetActive(false);
    }
}

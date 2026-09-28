// FinalManager.cs
// Controla toda la secuencia del final en la escena "final1".
// Los mensajes cambian según el nivel de refrigeración con el que el jugador terminó.

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
    private string textoEsferaPequena;
    private string textoEsferaGrande;

    void Start()
    {
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        float agua = DatosFinales.aguaConsumida;
        float refrigeracion = DatosFinales.refrigeracionRestante;

        // Preparar los textos según el nivel de refrigeración
        PrepararTextos(agua, refrigeracion);

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

    private void PrepararTextos(float agua, float refrigeracion)
    {
        // ===== TEXTO DE LA ESFERA PEQUEÑA (siempre muestra los litros reales) =====
        textoEsferaPequena = $"Agua consumida al usar LLM de forma eficiente:\n{agua:F2} Litros";

        // ===== TEXTO DE LA ESFERA GRANDE (cambia según refrigeración) =====
        if (refrigeracion >= 60f)
        {
            textoEsferaGrande = "Esta esfera representa el agua que consume un solo entrenamiento real de IA.\nTu recorrido fue eficiente, pero el sistema completo no lo es.";
        }
        else if (refrigeracion >= 25f)
        {
            textoEsferaGrande = "Esta esfera representa el agua que consume un solo entrenamiento real de IA.\nDelegaste parte de tu razonamiento. Cada atajo multiplicó el costo.";
        }
        else
        {
            textoEsferaGrande = "Esta esfera representa el agua que consume un solo entrenamiento real de IA.\nEl sistema casi colapsa. Ahora imagina millones de usuarios haciendo lo mismo.";
        }

        // ===== MENSAJES FINALES (cambian según refrigeración) =====
        if (refrigeracion >= 60f)
        {
            // FINAL EFICIENTE
            mensajesFinales = new string[]
            {
                $"> Output generado con éxito.\n> Estado de refrigeración restante: {refrigeracion:F0}%\n> Rendimiento: ÓPTIMO.",
                $"> Tu consulta evaporó {agua:F2} litros de agua real.\n> Fuiste eficiente. Usaste tu propia deducción la mayor parte del tiempo.\n> Pero incluso así, mantener esta red viva tiene un costo físico inevitable.",
                "> ¿Sabías que entrenar un solo modelo de lenguaje grande evapora cientos de miles de litros de agua?\n> Y eso es solo el entrenamiento. Cada consulta, cada respuesta, cada 'Enter' evapora un poco más.",
                "> La Ciencia de Datos es una herramienta poderosa.\n> Tú demostraste que se puede usar con inteligencia.\n> Sigue así: piensa antes de preguntar, y el planeta paga menos.",
                "> Úsala con conciencia."
            };
        }
        else if (refrigeracion >= 25f)
        {
            // FINAL INTERMEDIO
            mensajesFinales = new string[]
            {
                $"> Output generado con éxito.\n> Estado de refrigeración restante: {refrigeracion:F0}%\n> Rendimiento: MODERADO.",
                $"> Tu consulta evaporó {agua:F2} litros de agua real.\n> Podrías haber consumido menos si hubieses confiado más en tu propio razonamiento.\n> Cada vez que delegaste a la IA, el consumo de agua se disparó.",
                "> ¿Sabías que entrenar un solo modelo de lenguaje grande evapora cientos de miles de litros de agua?\n> Millones de personas delegan su pensamiento a estas máquinas todos los días.\n> Multiplica tu consumo por cada una de ellas.",
                "> La Ciencia de Datos es una herramienta poderosa, pero no gratuita.\n> Cada vez que presionas 'Enter' sin pensar primero, el planeta paga una parte del precio.",
                "> Úsala con conciencia."
            };
        }
        else
        {
            // FINAL CRÍTICO
            mensajesFinales = new string[]
            {
                $"> Output generado con éxito... apenas.\n> Estado de refrigeración restante: {refrigeracion:F0}%\n> Rendimiento: CRÍTICO.",
                $"> Tu consulta evaporó {agua:F2} litros de agua real.\n> Casi destruyes el sistema.\n> Delegar el razonamiento tiene un precio que no se ve, pero el planeta sí lo siente.",
                "> ¿Sabías que entrenar un solo modelo de lenguaje grande evapora cientos de miles de litros de agua?\n> Ahora imagina a millones de usuarios haciendo exactamente lo que tú hiciste:\n> presionar un botón en vez de pensar.",
                "> La Ciencia de Datos es una herramienta poderosa, pero peligrosa cuando se usa sin pensar.\n> Cada 'Enter' irreflexivo acelera un costo que el planeta no puede seguir pagando.",
                "> La próxima vez, piensa antes de delegar.\n> Úsala con conciencia."
            };
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

        // Mostrar mensaje de la esfera pequeña
        if (textoEsfera != null)
        {
            textoEsfera.gameObject.SetActive(true);
            textoEsfera.text = textoEsferaPequena;
        }

        StartCoroutine(EsperarYMostrarGigante());
    }

    private IEnumerator EsperarYMostrarGigante()
    {
        yield return new WaitForSeconds(4f);

        if (esferaGigante != null)
        {
            esferaGigante.SetActive(true);

            // Cambiar el texto al mensaje de la esfera grande
            if (textoEsfera != null)
            {
                textoEsfera.text = textoEsferaGrande;
            }

            // Espera 3 segundos para que el jugador la vea antes de que crezca
            yield return new WaitForSeconds(3f);

            esferaGiganteCreciendo = true;
        }
    }

    private IEnumerator EsperarYActivarFinal()
    {
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

        yield return new WaitForSeconds(2f);

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

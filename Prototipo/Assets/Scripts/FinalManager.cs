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
    private string[] mensajesFinales;
    private ComparadorAgua comparadorAgua;
    private float aguaFinal;
    private float refrigeracionFinal;
    private string textoEsferaPequena;
    private string textoEsferaGrande;

    void Start()
    {
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        float agua = DatosFinales.aguaConsumida;
        float refrigeracion = DatosFinales.refrigeracionRestante;
        aguaFinal = agua;
        refrigeracionFinal = refrigeracion;

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
        textoEsferaPequena = DatosFinales.colapsoTermico
            ? $"Agua consumida antes del colapso:\n{agua:F2} Litros"
            : DatosFinales.demasiadasCaidas
                ? $"Agua consumida antes de perderte en el vacío:\n{agua:F2} Litros"
                : $"Agua consumida al usar LLM de forma eficiente:\n{agua:F2} Litros";

        // ===== FINAL POR COLAPSO TÉRMICO (se acabaron las reservas y el jugador cayó) =====
        if (DatosFinales.colapsoTermico)
        {
            textoEsferaGrande = "Esta esfera representa el agua que consume un solo entrenamiento real de IA.\nTu sistema colapsó con mucho menos. Ahora imagina millones de usuarios.";
            mensajesFinales = new string[]
            {
                "> ERROR 508: Límite de recursos excedido.\n> Estado de refrigeración restante: 0%\n> Rendimiento: COLAPSO TÉRMICO.",
                $"> Tu consulta evaporó {agua:F2} litros de agua real antes de colapsar.\n> Ya sea delegando las decisiones a la IA o acumulando errores, la sed del algoritmo secó las reservas.",
                "> Cada cálculo, cada error y cada prompt consume agua real de nuestro planeta\n> para evitar que los componentes ardan.",
                "> Entender cómo funciona esta tecnología no es solo técnica, es supervivencia.",
                "> La próxima vez, calcula mejor tu huella.\n> Úsala con conciencia."
            };
            return;
        }

        // ===== FINAL POR DEMASIADAS CAÍDAS =====
        if (DatosFinales.demasiadasCaidas)
        {
            textoEsferaGrande = "Esta esfera representa el agua que consume un solo entrenamiento real de IA.\nCada intento fallido también costó agua.";
            mensajesFinales = new string[]
            {
                $"> ERROR 404: Usuario perdido en el vacío.\n> Estado de refrigeración restante: {refrigeracion:F0}%\n> Rendimiento: CONEXIÓN PERDIDA.",
                $"> Tu recorrido evaporó {agua:F2} litros de agua real.\n> Cada caída obligó al sistema a reconstruir el camino, y eso también tiene un costo.",
                "> Pensar con calma no es perder el tiempo: es lo que evita repetir el gasto.\n> Cada reintento, en una IA real, es otra consulta que evapora agua.",
                "> La próxima vez, avanza con conciencia."
            };
            return;
        }

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
    }

    private void ActivarEsfera()
    {
        esferaActivada = true;

        // El texto 3D viejo se reemplaza por el "sensor de huella hídrica" (ComparadorAgua):
        // explica qué es esta agua, la hace crecer según tus decisiones y, cuando corresponde,
        // hace aparecer la esfera gigante (un entrenamiento real de IA) llamando a este callback.
        if (textoEsfera != null) textoEsfera.gameObject.SetActive(false);

        comparadorAgua = ComparadorAgua.Iniciar(
            esferaAgua != null ? esferaAgua.transform : null,
            esferaGigante != null ? esferaGigante.transform : null,
            tamanoFinalGigante,
            () =>
            {
                if (esferaGigante == null) return;
                esferaGigante.SetActive(true);
                esferaGiganteCreciendo = true;
            });

        // Sin esfera gigante en la escena, el final sigue igual tras unos segundos.
        if (esferaGigante == null) StartCoroutine(FinalSinGigante());
    }

    private IEnumerator FinalSinGigante()
    {
        yield return new WaitForSeconds(20f);
        if (!finalActivado)
        {
            finalActivado = true;
            StartCoroutine(EsperarYActivarFinal());
        }
    }

    private IEnumerator EsperarYActivarFinal()
    {
        // Un poco más de pausa que antes: hay que alcanzar a leer el cierre del sensor de agua.
        yield return new WaitForSeconds(pausaAntesDeNegro + 4f);
        StartCoroutine(TransicionAVacioNegro());
    }

    private IEnumerator TransicionAVacioNegro()
    {
        // Congelar al jugador
        MonoBehaviour movimiento = playerObjeto.GetComponent<PlayerMovement>();
        if (movimiento == null) movimiento = playerObjeto.GetComponent("FirstPersonController") as MonoBehaviour;
        if (movimiento != null) movimiento.enabled = false;

        // Ocultar el texto de la esfera (y la UI vieja de la pantalla final, que ya no se usa)
        if (textoEsfera != null) textoEsfera.gameObject.SetActive(false);
        if (comparadorAgua != null) comparadorAgua.Ocultar();
        if (textoFinal != null) textoFinal.gameObject.SetActive(false);
        if (panelNegro != null) panelNegro.gameObject.SetActive(false);

        // Fundido a negro (funciona igual en el visor y en PC)
        yield return VeloNegro.Fundir(0f, 1f, 2f);
        yield return new WaitForSeconds(1f);

        // Informe final holográfico: tarjetas con los números, mensajes tipo terminal y
        // botones con la mirada + A/B/X/Y (ver InformeFinal).
        InformeFinal.Mostrar(mensajesFinales, aguaFinal, refrigeracionFinal);
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

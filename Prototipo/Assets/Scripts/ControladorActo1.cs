using System.Text.RegularExpressions;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
public class ControladorActo1 : MonoBehaviour
{
    [Header("Referencias de la Escena")]
    public UIManager uiManager;
    [Tooltip("Opcional: una puerta con PuertaDataCenter que quieras abrir al terminar el diálogo (además de la barrera).")]
    public PuertaDataCenter puertaInicio;
    [Tooltip("El robot guía: se pone rojo/enfermo al arrancar y se cura al terminar el diálogo.")]
    public BotGuia botGuia;
    [Tooltip("Objeto sólido (un Cube con Box Collider, sin 'Is Trigger') que bloquea el paso hacia el puente/túnel. Se desactiva al elegir 'Preparada/o.'.")]
    public GameObject barreraSalida;

    [Header("Texto inicial (pantalla negra / HUD)")]
    [TextArea(2, 3)]
    public string textoPantallaInicial = "Parece que te has perdido…";
    [Tooltip("Segundos que se muestra el texto inicial antes de que el robot empiece a hablar.")]
    public float duracionTextoInicial = 5f;
    [Tooltip("Segunda línea de la intro, bajo 'Parece que te has perdido…': avisa que todavía no te puedes mover.")]
    [TextArea(2, 3)]
    public string textoNoPuedesMoverte = "Tu cuerpo aún no responde.\nSolo puedes <color=#33E6FF>girar la cabeza</color> para mirar a tu alrededor.";

    [Header("Aviso al terminar el diálogo (todavía en negro)")]
    public string tituloYaPuedesMoverte = "YA PUEDES MOVERTE";
    [TextArea(2, 3)]
    public string textoYaPuedesMoverte = "Camina con el <color=#33E6FF>joystick</color>.\nSigue al robot y la línea del piso.";
    [Tooltip("Segundos que se lee el aviso en negro antes de que vuelva la imagen.")]
    public float segundosAvisoMoverte = 2.2f;

    [Header("Bip de atención")]
    [Tooltip("Arrastra aquí un AudioSource (puede vivir en el propio robot, o en cualquier objeto).")]
    public AudioSource fuenteAudioBip;
    public AudioClip clipBip;
    [Range(0f, 1f)]
    [Tooltip("Probabilidad, por cada letra escrita, de que suene un bip extra de 'glitch' además del bip inicial de cada frase.")]
    public float probabilidadBipExtra = 0.04f;

    [Header("Opciones del jugador")]
    [Tooltip("Tamaño de letra de las opciones que se eligen mirándolas (usan la misma fuente que el texto de la terminal).")]
    public float tamanoFuenteOpciones = 52f;

    [Header("Después del diálogo: negro y reubicación")]
    [Tooltip("Dónde aparece el jugador cuando termina el diálogo y todo se pone negro. Son los MISMOS números que muestra el Inspector del Player (relativos a su padre, 'Nivel'). Si asignas 'Punto Tras Dialogo', se usa ese objeto en vez de estos números.")]
    public Vector3 posicionTrasDialogo = new Vector3(-0.18f, -12.48f, -46.39f);
    [Tooltip("Hacia dónde mira el jugador al aparecer (rotación Y en grados, como en el Inspector del Player).")]
    public float rotacionYTrasDialogo = -540.1f;
    [Tooltip("Opcional: un objeto vacío en la escena que marque dónde aparece el jugador (usa su posición y su rotación Y).")]
    public Transform puntoTrasDialogo;
    public float duracionFundidoANegro = 1f;
    public float segundosEnNegro = 0.6f;
    public float duracionFundidoDesdeNegro = 1.5f;

    [Header("Páginas del diálogo")]
    [Tooltip("Los textos largos del robot se parten solos en páginas de más o menos este largo (cortando entre frases). Para cortar a mano en un lugar exacto, escribe || dentro del texto.")]
    public int maxCaracteresPorPagina = 170;
    [Tooltip("Texto de la opción para pasar a la siguiente página.")]
    public string textoBotonSiguiente = "Siguiente >";
    [Tooltip("Texto de la opción para volver a la página (o al diálogo) anterior.")]
    public string textoBotonAtras = "< Atrás";

    [Header("Ajuste fino de dónde apareces")]
    [Tooltip("Metros que se adelanta el punto de aparición, en la dirección hacia donde miras al aparecer (negativo = más atrás). La pared invisible se mueve junto con él.")]
    public float adelantarAparicion = 2f;

    [Header("No volver atrás (pared invisible)")]
    [Tooltip("Después del diálogo, el jugador no puede retroceder más allá de donde aparece. No usa ningún objeto: es una pared invisible por código.")]
    public bool bloquearVolverAtras = true;
    [Tooltip("Metros que se puede retroceder desde donde apareces antes de chocar con la pared invisible.")]
    public float margenHaciaAtras = 0.4f;
    [Tooltip("Ancho de la pared invisible (metros). Que sea más ancho que el pasillo.")]
    public float anchoBloqueo = 8f;
    [Tooltip("Marca esto si la pared quedó delante en vez de detrás (bloquea el lado contrario).")]
    public bool invertirBloqueo = false;

    [Header("Cierre del diálogo")]
    [TextArea(1, 2)]
    public string textoFinalTrasDialogo = "Sígueme...";

    // ------------------------------------------------------------------
    // GUION DEL ROBOT: cada nodo es una línea que dice el robot, mostrada
    // SOLO en su burbuja. Las opciones del jugador (las líneas "JUGADOR:")
    // NUNCA entran a la burbuja: aparecen aparte, en un panel flotante
    // (PanelOpcionesMirada) y se eligen con la mirada + un botón.
    // ------------------------------------------------------------------

    [System.Serializable]
    public class OpcionDialogo
    {
        public string textoOpcion;
        public int nodoSiguiente; // -1 = termina el diálogo
    }

    [System.Serializable]
    public class NodoDialogoRobot
    {
        [TextArea(2, 6)] public string textoRobot;
        public OpcionDialogo[] opciones;
    }

    public List<NodoDialogoRobot> guionRobot = new List<NodoDialogoRobot>()
    {
        new NodoDialogoRobot
        {
            textoRobot = "¡Al fin alguien aquí! No sé cómo entraste, pero has caído dentro de un servidor de procesamiento. Por favor ayúdame y te ayudaré a salir.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "¿Qué es este lugar?", nodoSiguiente = 1 },
                new OpcionDialogo { textoOpcion = "¿Qué tengo que hacer?", nodoSiguiente = 1 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Estoy enfermo y debilitado… Soy un agente de Large Language Model, el tipo de Inteligencia Artificial que lee miles de textos para responder como haría un humano. Mi trabajo es procesar datos, pero el paso hacia la salida está roto y mi sistema está por colapsar.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "¿Por qué estás enfermo?", nodoSiguiente = 2 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Me he quedado sin agua. Los servidores de Inteligencia Artificial viven de miles de litros de agua real para enfriarse y no sobrecalentarse. Como mis reservas están en las últimas, no puedo pensar. Necesito que tú repares el puente de salida asociando los conceptos manualmente.",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "Entendido, ¿cómo cruzo?", nodoSiguiente = 3 },
            }
        },
        new NodoDialogoRobot
        {
            textoRobot = "Piensa como una Inteligencia Artificial (IA): calcula la relación lógica entre palabras. Si una te cuesta mucho, puedes pedir \"Responder con IA\", pero ten cuidado. Delegarme la respuesta forzará el procesador y gastará gran parte de mis reservas de agua. Cada fallo también gasta agua, y si la reserva llega a 0%, colapsamos. Sé consciente de tus decisiones. ¿Preparada/o?",
            opciones = new[]
            {
                new OpcionDialogo { textoOpcion = "Preparada/o.", nodoSiguiente = -1 },
            }
        },
    };

    private int nodoActual = 0;
    private bool dialogoIniciado = false;
    private float proximoAvisoBloqueo = 0f;
    private List<string> paginas = new List<string>();
    private int paginaActual = 0;
    private readonly Stack<int> historial = new Stack<int>(); // nodos ya vistos, para "Atrás"
    private bool esperandoOpcion = false;
    private bool dialogoTerminado = false;
    private PlayerMovement movimientoJugador;

    /// <summary>
    /// True recién cuando el jugador terminó de leer TODO el diálogo del robot y eligió
    /// "Preparada/o.". Otros scripts (como TerminalInteractiva) lo consultan para no
    /// activarse antes de tiempo.
    /// </summary>
    public bool DialogoTerminado => dialogoTerminado;

    void Start()
    {
        // La barrera bloquea desde el minuto uno, hasta que se elija "Preparada/o.".
        if (barreraSalida != null) barreraSalida.SetActive(true);

        // El jugador no se puede mover desde el menú hasta terminar TODO el diálogo
        // (mirar alrededor sí, para apuntar a las opciones).
        movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        CongelarJugador(true);

        // El robot se queda quieto (esperando para hablar) hasta que termine el diálogo.
        if (botGuia != null) botGuia.enPausa = true;

        StartCoroutine(SecuenciaCompleta());
    }

    private IEnumerator SecuenciaCompleta()
    {
        // Por si NubeDialogoBot arranca mostrando su texto de ejemplo antes de que
        // nosotros tomemos el control del diálogo.
        if (NubeDialogoBot.Instancia != null)
        {
            NubeDialogoBot.Instancia.Ocultar();
        }

        // Primero el menú principal, y después su intro (negro + "Parece que te has
        // perdido…" + el juego apareciendo de a poco). Recién ahí habla el robot.
        // 'textoPantallaInicial' y 'duracionTextoInicial' los usa MenuPrincipal para esa intro.
        yield return null; // deja que MenuPrincipal alcance a crearse
        while (MenuPrincipal.Bloqueando) yield return null;

        // Por si algo lo soltó en el camino (el menú, otro script...).
        CongelarJugador(true);

        // El robot aparece de frente, delante de donde está mirando el jugador.
        if (botGuia != null)
        {
            botGuia.enPausa = true;
            botGuia.ColocarFrenteAlJugador();
        }

        dialogoIniciado = true;
        MostrarNodo(0);
    }

    /// <summary>
    /// Si durante la conversación el jugador empuja el joystick, le recordamos (con el aviso
    /// del borde superior) que primero tiene que hablar con el robot. Máximo uno cada 6 s.
    /// </summary>
    void Update()
    {
        if (!dialogoIniciado || dialogoTerminado || Time.time < proximoAvisoBloqueo) return;
        if (EntradaVR.Mover.magnitude < 0.6f) return;

        proximoAvisoBloqueo = Time.time + 6f;
        AvisoSistema.Mostrar(AvisoSistema.Tipo.Bloqueado, "", 0f, 0f);
    }

    private void CongelarJugador(bool congelar)
    {
        if (movimientoJugador == null) movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        if (movimientoJugador != null) movimientoJugador.enabled = !congelar;
    }

    private void MostrarNodo(int indice)
    {
        nodoActual = indice;
        esperandoOpcion = false;

        PanelOpcionesMirada.Ocultar(); // limpia las opciones del nodo anterior

        paginas = Paginar(guionRobot[indice].textoRobot, maxCaracteresPorPagina);
        paginaActual = 0;
        StartCoroutine(EscribirEnNube(paginas[0], AlTerminarDeEscribir, IndicadorPagina()));
    }

    /// <summary>¿Hay algo antes? (una página anterior de este texto o un diálogo anterior).</summary>
    private bool PuedeVolver => paginaActual > 0 || historial.Count > 0;

    /// <summary>
    /// Parte un texto largo en páginas cortas, sin cortar frases a la mitad.
    /// "||" fuerza un corte exacto. Una frase sola más larga que el máximo queda en su propia página.
    /// </summary>
    private static List<string> Paginar(string texto, int maximo)
    {
        List<string> resultado = new List<string>();
        if (string.IsNullOrEmpty(texto)) { resultado.Add(""); return resultado; }

        foreach (string bloque in texto.Split(new[] { "||" }, System.StringSplitOptions.RemoveEmptyEntries))
        {
            string[] frases = Regex.Split(bloque.Trim(), @"(?<=[.!?…])\s+(?=[¿¡""A-ZÁÉÍÓÚÑ])");
            string pagina = "";
            foreach (string frase in frases)
            {
                if (pagina.Length > 0 && pagina.Length + 1 + frase.Length > maximo)
                {
                    resultado.Add(pagina);
                    pagina = "";
                }
                pagina = pagina.Length == 0 ? frase : pagina + " " + frase;
            }
            if (pagina.Length > 0) resultado.Add(pagina);
        }
        if (resultado.Count == 0) resultado.Add(texto);
        return resultado;
    }

    /// <summary>"2/3" chiquito debajo del texto de la burbuja (vacío si el texto cabe en una página).</summary>
    private string IndicadorPagina()
    {
        if (paginas.Count <= 1) return "";
        return $"\n<size=60%><color=#7FFFD4>{paginaActual + 1}/{paginas.Count}</color></size>";
    }

    /// <summary>Opciones de una página intermedia: 0 = "Siguiente", 1 = "Atrás".</summary>
    private void ElegirEnPagina(int indice)
    {
        if (!esperandoOpcion || dialogoTerminado) return;
        esperandoOpcion = false;
        PanelOpcionesMirada.Ocultar();
        StartCoroutine(BorrarYCambiarPagina(indice == 0 ? 1 : -1));
    }

    private void Atras()
    {
        if (!esperandoOpcion || dialogoTerminado) return;
        esperandoOpcion = false;
        PanelOpcionesMirada.Ocultar();
        StartCoroutine(BorrarYCambiarPagina(-1));
    }

    /// <summary>
    /// Transición entre páginas: el texto se "borra" rápido (como retrocediendo).
    /// Hacia adelante se escribe la siguiente letra por letra; hacia atrás la anterior aparece
    /// de una (ya la leíste). Si estás en la primera página, "Atrás" vuelve al diálogo anterior.
    /// </summary>
    private IEnumerator BorrarYCambiarPagina(int direccion)
    {
        string anterior = paginas[paginaActual];
        if (NubeDialogoBot.Instancia != null)
        {
            for (int largo = anterior.Length; largo > 0; largo -= 7)
            {
                NubeDialogoBot.Instancia.Mostrar(anterior.Substring(0, largo));
                yield return null;
            }
        }

        if (direccion > 0)
        {
            paginaActual++;
            yield return EscribirEnNube(paginas[paginaActual], AlTerminarDeEscribir, IndicadorPagina());
            yield break;
        }

        if (paginaActual > 0)
        {
            paginaActual--;
        }
        else if (historial.Count > 0)
        {
            nodoActual = historial.Pop();
            paginas = Paginar(guionRobot[nodoActual].textoRobot, maxCaracteresPorPagina);
            paginaActual = paginas.Count - 1;
        }

        if (NubeDialogoBot.Instancia != null) NubeDialogoBot.Instancia.Mostrar(paginas[paginaActual] + IndicadorPagina());
        AlTerminarDeEscribir();
    }

    /// <summary>
    /// Escribe el texto letra por letra directamente en la burbuja del robot (NubeDialogoBot
    /// no tiene animación propia). Suena un bip al empezar la frase, y ocasionalmente algún
    /// bip extra "glitcheado" en medio, como si al robot se le distorsionara la voz al hablar.
    /// </summary>
    private IEnumerator EscribirEnNube(string mensaje, System.Action alTerminar, string sufijo = "")
    {
        if (NubeDialogoBot.Instancia == null)
        {
            // Sin burbuja no hay dónde escribir, pero el diálogo no se puede quedar pegado.
            alTerminar?.Invoke();
            yield break;
        }

        ReproducirBip();
        yield return new WaitForSeconds(0.35f); // el bip suena antes de que empiece el texto

        string acumulado = "";
        foreach (char letra in mensaje)
        {
            acumulado += letra;
            NubeDialogoBot.Instancia.Mostrar(acumulado);

            if (Random.value < probabilidadBipExtra)
            {
                ReproducirBip();
            }

            yield return new WaitForSeconds(0.028f);
        }

        if (!string.IsNullOrEmpty(sufijo)) NubeDialogoBot.Instancia.Mostrar(acumulado + sufijo);

        alTerminar?.Invoke();
    }

    private void ReproducirBip()
    {
        if (fuenteAudioBip == null || clipBip == null) return;
        // El pitch variable (más bajo/errático de lo normal) es lo que da la sensación
        // de que el sonido viene distorsionado, como un robot enfermo hablando mal.
        fuenteAudioBip.pitch = Random.Range(0.6f, 1.3f);
        fuenteAudioBip.PlayOneShot(clipBip);
    }

    private void AlTerminarDeEscribir()
    {
        TMP_FontAsset fuente = uiManager != null && uiManager.textoTerminal != null ? uiManager.textoTerminal.font : null;

        // Quedan páginas de este mismo texto: solo el botón "Siguiente".
        if (paginaActual < paginas.Count - 1)
        {
            esperandoOpcion = true;
            string[] botones = PuedeVolver ? new[] { textoBotonSiguiente, textoBotonAtras } : new[] { textoBotonSiguiente };
            PanelOpcionesMirada.Mostrar(null, botones, fuente, tamanoFuenteOpciones, ElegirEnPagina, 0.45f);
            return;
        }

        OpcionDialogo[] opciones = guionRobot[nodoActual].opciones;

        if (opciones == null || opciones.Length == 0)
        {
            TerminarDialogo();
            return;
        }

        esperandoOpcion = true;

        // Las opciones del jugador se muestran APARTE, en un panel flotante delante de la
        // mirada (nunca dentro de la burbuja del robot), y se eligen con la mirada + un botón.
        // Al final va "Atrás" (si hay algo antes), para no elegirlo sin querer.
        string[] textos = new string[opciones.Length + (PuedeVolver ? 1 : 0)];
        for (int i = 0; i < opciones.Length; i++)
        {
            textos[i] = opciones[i].textoOpcion;
        }
        if (PuedeVolver) textos[opciones.Length] = textoBotonAtras;

        // Un poco más abajo de lo normal, para no tapar al robot ni su burbuja.
        PanelOpcionesMirada.Mostrar(null, textos, fuente, tamanoFuenteOpciones, ElegirOpcion, 0.45f);
    }

    private void ElegirOpcion(int indice)
    {
        if (!esperandoOpcion || dialogoTerminado) return;
        if (indice >= guionRobot[nodoActual].opciones.Length) { Atras(); return; }
        esperandoOpcion = false;
        int siguiente = guionRobot[nodoActual].opciones[indice].nodoSiguiente;

        if (siguiente < 0 || siguiente >= guionRobot.Count)
        {
            TerminarDialogo();
        }
        else
        {
            historial.Push(nodoActual);
            MostrarNodo(siguiente);
        }
    }

    private void TerminarDialogo()
    {
        dialogoTerminado = true;

        PanelOpcionesMirada.Ocultar(); // ya no hay nada que elegir
        StartCoroutine(TransicionTrasDialogo());
    }

    /// <summary>
    /// Al elegir "Preparada/o.": todo se pone negro, el jugador y el robot aparecen en el
    /// punto de salida, vuelve la imagen, el robot dice "Sígueme..." y recién ahí el jugador
    /// se puede mover y el robot empieza a guiarlo.
    /// </summary>
    private IEnumerator TransicionTrasDialogo()
    {
        if (botGuia != null) botGuia.Curarse();
        if (NubeDialogoBot.Instancia != null) NubeDialogoBot.Instancia.Ocultar();

        yield return VeloNegro.Fundir(0f, 1f, duracionFundidoANegro);

        // --- Todo esto pasa en negro ---
        if (barreraSalida != null) barreraSalida.SetActive(false);
        if (puertaInicio != null) puertaInicio.AbrirPuerta();

        if (movimientoJugador == null) movimientoJugador = FindAnyObjectByType<PlayerMovement>();
        if (movimientoJugador != null)
        {
            Vector3 destino;
            float rotacionY;
            if (puntoTrasDialogo != null)
            {
                destino = puntoTrasDialogo.position;
                rotacionY = puntoTrasDialogo.eulerAngles.y;
            }
            else
            {
                // Los números del Inspector son locales al padre del Player ('Nivel', que está
                // desplazado 24,66 m en Y): se pasan a coordenadas de mundo.
                Transform padre = movimientoJugador.transform.parent;
                destino = padre != null ? padre.TransformPoint(posicionTrasDialogo) : posicionTrasDialogo;
                rotacionY = rotacionYTrasDialogo + (padre != null ? padre.eulerAngles.y : 0f);
            }
            // Ajuste fino: unos metros más adelante, en la dirección hacia donde miras.
            destino += Quaternion.Euler(0f, rotacionY, 0f) * Vector3.forward * adelantarAparicion;
            movimientoJugador.TeletransportarA(destino, rotacionY);

            // Pared invisible detrás de donde apareces: hacia dónde miras al aparecer es "adelante".
            if (bloquearVolverAtras)
            {
                Vector3 haciaAdelante = Quaternion.Euler(0f, rotacionY, 0f) * Vector3.forward;
                if (invertirBloqueo) haciaAdelante = -haciaAdelante;
                movimientoJugador.ActivarLimiteTrasero(destino - haciaAdelante * margenHaciaAtras, haciaAdelante, anchoBloqueo);
            }
        }

        yield return null; // un frame para que la cámara (y la cabeza en el visor) ya estén en el lugar nuevo

        if (botGuia != null) botGuia.ColocarFrenteAlJugador();

        // La línea del piso se enciende TODAVÍA EN NEGRO: así al volver la imagen ya están
        // el robot y la línea juntos (antes la línea aparecía después, de golpe).
        if (SenializacionRuta.Instancia != null) SenializacionRuta.Instancia.MostrarLinea(true);

        yield return new WaitForSeconds(Mathf.Max(segundosEnNegro, 0.3f));

        // Todavía en negro: "YA PUEDES MOVERTE" con un joystick que se mueve solo.
        MensajeCentral aviso = MensajeCentral.Crear(tituloYaPuedesMoverte, textoYaPuedesMoverte, MensajeCentral.Icono.JoystickLibre);
        yield return aviso.Aparecer();
        yield return new WaitForSeconds(segundosAvisoMoverte);

        // El aviso se va mientras vuelve la imagen, y desde ahí ya te puedes mover.
        StartCoroutine(aviso.Desaparecer(duracionFundidoDesdeNegro * 0.8f));
        CongelarJugador(false);
        yield return VeloNegro.Fundir(1f, 0f, duracionFundidoDesdeNegro);

        // El robot dice su última frase (con bip, igual que las demás).
        yield return EscribirEnNube(textoFinalTrasDialogo, null);

        CongelarJugador(false);
        if (botGuia != null) botGuia.enPausa = false;

    }
}

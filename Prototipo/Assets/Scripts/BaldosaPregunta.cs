using UnityEngine;

public class BaldosaPregunta : MonoBehaviour
{
    public enum NumeroDePiso { Piso1_Rey, Piso2_Paris, Piso3_Medico }
    
    [Header("Configuración del Nodo")]
    public NumeroDePiso elegirPregunta;

    [Header("Ajuste de Detección")]
    public float distanciaDeActivacion = 2.5f;

    private string enunciadoPregunta;
    private string[] textoOpciones = new string[3];
    private float[] costoRefri = new float[3];
    private float[] costoAgua = new float[3];
    private string[] mensajeResultado = new string[3];
    private int indiceCorrecta = -1;

    private bool jugadorEncima = false;
    private bool respondida = false;
    public bool EstaRespondida => respondida;
    private GameObject playerObjeto;
    private MonoBehaviour scriptMovimientoPlayer;

    void Start()
    {
        ConfigurarPreguntaAutomatica();
        playerObjeto = GameObject.FindGameObjectWithTag("Player");

        MeshCollider col = GetComponent<MeshCollider>();
        if (col != null) col.isTrigger = false;
    }

    private void ConfigurarPreguntaAutomatica()
    {
        // BALANCE DE DAÑO SINFÓNICO: 
        // Las opciones correctas consumen un sutil 5%.
        // Las respuestas incorrectas castigan con un masivo 45% o 50% de refrigeración.
        // Esto garantiza que 2 errores seguidos o combinados destruyan el suelo por completo (0%).

        switch (elegirPregunta)
        {
            case NumeroDePiso.Piso1_Rey:
                enunciadoPregunta = "Un panel holográfico se enciende frente al precipicio mostrando una ecuación de vectores incompleta:\n\nRey - Hombre + Mujer ≈ [¿?]";
                
                textoOpciones[0] = "Princesa"; 
                costoRefri[0] = 45f; costoAgua[0] = 1.5f; // Castigo alto
                mensajeResultado[0] = "> Error de coherencia vectorial. El datacenter disipa calor crítico.";

                textoOpciones[1] = "Reina"; // Correcta
                indiceCorrecta = 1;
                costoRefri[1] = 5f; costoAgua[1] = 0.15f; 
                mensajeResultado[1] = "> Procesamiento eficiente. El puente de luz se forma en verde brillante.";

                textoOpciones[2] = "Castillo"; 
                costoRefri[2] = 50f; costoAgua[2] = 2.0f; // Castigo crítico (Mitad de la barra)
                mensajeResultado[2] = "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta.";
                break;

            case NumeroDePiso.Piso2_Paris:
                enunciadoPregunta = "El panel holográfico se mantiene activo mostrando una nueva relación semántica:\n\nParís - Francia + Italia ≈ [¿?]";
                
                textoOpciones[0] = "Roma"; // Correcta
                indiceCorrecta = 0;
                costoRefri[0] = 5f; costoAgua[0] = 0.15f; 
                mensajeResultado[0] = "> Procesamiento eficiente. Las conexiones vectoriales se estabilizan.";

                textoOpciones[1] = "Venecia"; 
                costoRefri[1] = 45f; costoAgua[1] = 1.5f; 
                mensajeResultado[1] = "> Error de coherencia vectorial. Evaporación de agua acelerada en las celdas fucsias.";

                textoOpciones[2] = "Europa"; 
                costoRefri[2] = 50f; costoAgua[2] = 2.0f; 
                mensajeResultado[2] = "> Error de coherencia. La sobrecarga térmica reduce drásticamente las reservas.";
                break;

            case NumeroDePiso.Piso3_Medico:
                enunciadoPregunta = "Último cruce vectorial antes de la salida del túnel. Resuelve la incógnita:\n\nMédico - Hospital + Colegio ≈ [¿?]";
                
                textoOpciones[0] = "Alumno"; 
                costoRefri[0] = 50f; costoAgua[0] = 2.0f; 
                mensajeResultado[0] = "> Error de coherencia. Inestabilidad computacional crítica en el último tramo.";

                textoOpciones[1] = "Pizarra"; 
                costoRefri[1] = 45f; costoAgua[1] = 1.5f; 
                mensajeResultado[1] = "> Error de coherencia. Los ventiladores no logran mitigar el impacto térmico.";

                textoOpciones[2] = "Profesor"; // Correcta
                indiceCorrecta = 2;
                costoRefri[2] = 5f; costoAgua[2] = 0.15f; 
                mensajeResultado[2] = "> OUTPUT GENERADO. La gran compuerta del final se ilumina.";
                break;
        }
    }

    void Update()
    {
        if (respondida || playerObjeto == null) return;

        float distancia = Vector3.Distance(transform.position, playerObjeto.transform.position);

        if (distancia <= distanciaDeActivacion && !jugadorEncima)
        {
            jugadorEncima = true;
            CongelarJugador(true);
            DesplegarPreguntaEnUI();
        }
    }

    void OnGUI()
    {
        if (jugadorEncima && !respondida)
        {
            Event e = Event.current;
            if (e.isKey && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
                    EvaluarRespuesta(0);
                else if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
                    EvaluarRespuesta(1);
                else if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
                    EvaluarRespuesta(2);
                else if (e.keyCode == KeyCode.Alpha4 || e.keyCode == KeyCode.Keypad4)
                    UsarIA();
            }
        }
    }

    private void DesplegarPreguntaEnUI()
    {
        if (GameManager.Instance != null && GameManager.Instance.uiManager != null)
        {
            string textoCompleto = $"{enunciadoPregunta}\n\n" +
                                   $"[1] {textoOpciones[0]}\n" +
                                   $"[2] {textoOpciones[1]}\n" +
                                   $"[3] {textoOpciones[2]}\n" +
                                   $"[4] Responder con IA";

            GameManager.Instance.uiManager.MostrarMensajeTerminal(textoCompleto);
        }
    }

    // Delega la respuesta a la IA: avanza sin pensar, a costa de un consumo de agua mayor al de cualquier opción manual
    private void UsarIA()
    {
        respondida = true;
        jugadorEncima = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UsarBotonIA();
        }

        CongelarJugador(false);
    }

    private void EvaluarRespuesta(int indiceOpcion)
    {
        // Marcamos como respondida inmediatamente para que NUNCA te vuelva a preguntar lo mismo
        respondida = true;
        jugadorEncima = false;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegistrarRespuesta(
                costoRefri[indiceOpcion], 
                costoAgua[indiceOpcion], 
                mensajeResultado[indiceOpcion],
                indiceOpcion == indiceCorrecta
            );
        }

        // Te libera sí o sí para que sigas caminando, sin importar si te equivocaste o no
        CongelarJugador(false);
    }

    private void CongelarJugador(bool congelar)
    {
        if (playerObjeto == null) return;

        if (scriptMovimientoPlayer == null)
        {
            scriptMovimientoPlayer = playerObjeto.GetComponent<PlayerMovement>();
            if (scriptMovimientoPlayer == null) scriptMovimientoPlayer = playerObjeto.GetComponent("PlayerController") as MonoBehaviour;
            if (scriptMovimientoPlayer == null) scriptMovimientoPlayer = playerObjeto.GetComponent("FirstPersonController") as MonoBehaviour;
        }

        if (scriptMovimientoPlayer != null) scriptMovimientoPlayer.enabled = !congelar;
    }
}
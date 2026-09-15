using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bot guía que CAMINA por el suelo delante del jugador, con la mitad de su altura.
/// Ya no flota ni proyecta el haz que atravesaba los muros: la guía visual ahora es la
/// línea de <see cref="SenializacionRuta"/> pintada en el piso.
/// </summary>
public class BotGuia : MonoBehaviour
{
    [Header("Seguimiento del jugador")]
    [Tooltip("Si lo dejas vacío, lo busca automáticamente por el tag 'Player'")]
    public Transform jugador;
    [Tooltip("Metros que el bot intenta mantener por delante del jugador a lo largo de la ruta")]
    public float distanciaAdelante = 2.5f;
    [Tooltip("Si el jugador se queda más atrás que esto, el bot lo espera")]
    public float distanciaMaximaDelJugador = 5f;

    [Header("Caminar")]
    public float velocidad = 2.6f;
    public float velocidadRotacion = 8f;
    [Tooltip("Separación entre los pies y el piso")]
    public float alturaSobreSuelo = 0.02f;
    [Tooltip("Suavizado vertical al subir o bajar escalones y rampas")]
    public float suavizadoVertical = 10f;

    [Header("Cuerpo")]
    [Tooltip("Altura total del bot. La mitad de la del jugador (el CharacterController mide 2)")]
    public float altura = 1f;
    [Tooltip("Desactívalo si prefieres montar tú el modelo del bot como hijo de este objeto")]
    public bool construirCuerpoAutomatico = true;
    [Tooltip("Material del chasis. Si lo dejas vacío se genera uno oscuro automáticamente")]
    public Material materialCuerpo;
    public Color colorLuz = new Color(0f, 1f, 1f, 1f);

    private readonly List<Vector3> rutaPropia = new List<Vector3>();
    private BaldosaPregunta[] baldosas;
    private float tiempoDesdeUltimoCalculo;
    private float faseCaminata;
    private float velocidadActual;
    private float alturaObjetivo;

    private Transform piernaIzquierda;
    private Transform piernaDerecha;
    private Transform brazoIzquierdo;
    private Transform brazoDerecho;
    private Transform cuerpo;

    void Start()
    {
        if (jugador == null)
        {
            GameObject playerObjeto = GameObject.FindGameObjectWithTag("Player");
            if (playerObjeto != null) jugador = playerObjeto.transform;
        }

        if (jugador == null)
        {
            Debug.LogWarning("[BotGuia] No se encontró ningún GameObject con el tag 'Player'. El bot se quedará quieto donde lo colocaste en el Editor.");
        }

        baldosas = FindObjectsByType<BaldosaPregunta>(FindObjectsInactive.Exclude);

        // El bot no debe empujar al jugador ni bloquear puertas, ni contar como pared al trazar la ruta.
        foreach (Collider colisionador in GetComponentsInChildren<Collider>(true))
        {
            colisionador.enabled = false;
        }
        NavegacionSuelo.Instancia.RegistrarIgnorado(gameObject);
        if (jugador != null) NavegacionSuelo.Instancia.RegistrarIgnorado(jugador.gameObject);

        if (construirCuerpoAutomatico) ConstruirCuerpo();

        ColocarJuntoAlJugador();

        Debug.Log($"[BotGuia] Inicializado caminando. Altura: {altura} m (la mitad del jugador). Baldosas detectadas: {baldosas.Length}.");
    }

    /// <summary>
    /// Arma un robot bípedo sencillo con primitivas, escalado a <see cref="altura"/>.
    /// Así no hace falta importar ningún modelo y el tamaño siempre sale exacto.
    /// </summary>
    private void ConstruirCuerpo()
    {
        // La escala del objeto en escena se ignora: el tamaño lo fija 'altura'.
        transform.localScale = Vector3.one;

        MeshRenderer rendererRaiz = GetComponent<MeshRenderer>();
        if (rendererRaiz != null)
        {
            if (materialCuerpo == null) materialCuerpo = rendererRaiz.sharedMaterial;
            rendererRaiz.enabled = false;
        }

        if (materialCuerpo == null)
        {
            Shader shaderChasis = Shader.Find("Universal Render Pipeline/Lit");
            if (shaderChasis == null) shaderChasis = Shader.Find("Standard");
            materialCuerpo = new Material(shaderChasis);
            Color gris = new Color(0.12f, 0.14f, 0.18f, 1f);
            if (materialCuerpo.HasProperty("_BaseColor")) materialCuerpo.SetColor("_BaseColor", gris);
            materialCuerpo.color = gris;
            if (materialCuerpo.HasProperty("_Smoothness")) materialCuerpo.SetFloat("_Smoothness", 0.7f);
        }

        Material materialLuz = CrearMaterialEmisivo(colorLuz);

        // Proporciones en fracción de la altura total, medidas desde los pies.
        float largoPierna = altura * 0.38f;
        float altoTorso = altura * 0.36f;
        float radioCabeza = altura * 0.13f;
        float anchoCaderas = altura * 0.14f;

        cuerpo = new GameObject("Cuerpo").transform;
        cuerpo.SetParent(transform, false);
        cuerpo.localPosition = Vector3.zero;

        // Torso
        Transform torso = CrearPieza(PrimitiveType.Capsule, "Torso", cuerpo, materialCuerpo);
        torso.localPosition = new Vector3(0f, largoPierna + altoTorso * 0.5f, 0f);
        torso.localScale = new Vector3(altura * 0.30f, altoTorso * 0.5f, altura * 0.24f);

        // Cabeza y visor
        Transform cabeza = CrearPieza(PrimitiveType.Sphere, "Cabeza", cuerpo, materialCuerpo);
        cabeza.localPosition = new Vector3(0f, largoPierna + altoTorso + radioCabeza * 0.9f, 0f);
        cabeza.localScale = Vector3.one * (radioCabeza * 2f);

        Transform visor = CrearPieza(PrimitiveType.Cube, "Visor", cabeza, materialLuz);
        visor.localPosition = new Vector3(0f, 0.05f, 0.44f);
        visor.localScale = new Vector3(0.62f, 0.22f, 0.2f);

        // Piernas (pivote en la cadera, para que el giro de la caminata sea creíble)
        piernaIzquierda = CrearMiembro("PiernaIzquierda", cuerpo, materialCuerpo,
            new Vector3(-anchoCaderas, largoPierna, 0f), largoPierna, altura * 0.09f);
        piernaDerecha = CrearMiembro("PiernaDerecha", cuerpo, materialCuerpo,
            new Vector3(anchoCaderas, largoPierna, 0f), largoPierna, altura * 0.09f);

        // Brazos
        float largoBrazo = altoTorso * 0.85f;
        brazoIzquierdo = CrearMiembro("BrazoIzquierdo", cuerpo, materialCuerpo,
            new Vector3(-altura * 0.17f, largoPierna + altoTorso * 0.92f, 0f), largoBrazo, altura * 0.06f);
        brazoDerecho = CrearMiembro("BrazoDerecho", cuerpo, materialCuerpo,
            new Vector3(altura * 0.17f, largoPierna + altoTorso * 0.92f, 0f), largoBrazo, altura * 0.06f);

        // Luz de posición del propio bot: pequeña y de rango corto, no ilumina el nivel.
        Transform baliza = CrearPieza(PrimitiveType.Sphere, "Baliza", cuerpo, materialLuz);
        baliza.localPosition = new Vector3(0f, largoPierna + altoTorso + radioCabeza * 2.1f, 0f);
        baliza.localScale = Vector3.one * (altura * 0.06f);
    }

    private Transform CrearMiembro(string nombre, Transform padre, Material material, Vector3 posicionPivote, float largo, float grosor)
    {
        // El pivote se queda arriba (cadera / hombro) y la pieza cuelga hacia abajo.
        GameObject pivote = new GameObject(nombre);
        pivote.transform.SetParent(padre, false);
        pivote.transform.localPosition = posicionPivote;

        Transform pieza = CrearPieza(PrimitiveType.Capsule, nombre + "_Malla", pivote.transform, material);
        pieza.localPosition = new Vector3(0f, -largo * 0.5f, 0f);
        pieza.localScale = new Vector3(grosor, largo * 0.5f, grosor);

        return pivote.transform;
    }

    private Transform CrearPieza(PrimitiveType tipo, string nombre, Transform padre, Material material)
    {
        GameObject pieza = GameObject.CreatePrimitive(tipo);
        pieza.name = nombre;
        pieza.transform.SetParent(padre, false);

        Collider colisionador = pieza.GetComponent<Collider>();
        if (colisionador != null)
        {
            colisionador.enabled = false; // Destroy es diferido: lo apagamos ya para que no toque al jugador.
            Destroy(colisionador);
        }

        MeshRenderer renderizador = pieza.GetComponent<MeshRenderer>();
        renderizador.sharedMaterial = material;
        renderizador.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderizador.receiveShadows = false;

        return pieza.transform;
    }

    private static Material CrearMaterialEmisivo(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");

        Material material = new Material(shader);
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        material.color = color;
        material.EnableKeyword("_EMISSION");
        if (material.HasProperty("_EmissionColor")) material.SetColor("_EmissionColor", color * 3f);

        return material;
    }

    private void ColocarJuntoAlJugador()
    {
        if (jugador == null) return;

        Vector3 destino = jugador.position + jugador.forward * distanciaAdelante;
        if (NavegacionSuelo.Instancia.AlturaDelSuelo(destino, out float alturaSuelo))
        {
            destino.y = alturaSuelo + alturaSobreSuelo;
        }
        else
        {
            destino.y = transform.position.y;
        }

        transform.position = destino;
        alturaObjetivo = destino.y;
    }

    void Update()
    {
        if (jugador == null) return;

        Vector3 puntoObjetivo = ObtenerPuntoDeGuia();
        MoverHacia(puntoObjetivo);
        PegarAlSuelo();
        AnimarCaminata();
    }

    /// <summary>
    /// Punto de la ruta que el bot intenta ocupar: avanza <see cref="distanciaAdelante"/> metros
    /// sobre la misma línea que ve el jugador, así el bot siempre camina justo sobre ella.
    /// </summary>
    private Vector3 ObtenerPuntoDeGuia()
    {
        IReadOnlyList<Vector3> ruta = ObtenerRuta();

        if (ruta == null || ruta.Count < 2)
        {
            // Sin ruta: el bot se queda esperando al lado del jugador.
            return jugador.position + jugador.forward * 1.2f;
        }

        // Proyecta al jugador sobre la ruta y avanza a lo largo de ella.
        float restante = distanciaAdelante;
        Vector3 puntoActual = ruta[0];

        for (int i = 0; i < ruta.Count - 1; i++)
        {
            Vector3 inicio = ruta[i];
            Vector3 fin = ruta[i + 1];
            float largo = Vector3.Distance(inicio, fin);
            if (largo < 0.001f) continue;

            if (restante <= largo)
            {
                return Vector3.Lerp(inicio, fin, restante / largo);
            }

            restante -= largo;
            puntoActual = fin;
        }

        return puntoActual;
    }

    private IReadOnlyList<Vector3> ObtenerRuta()
    {
        // Preferimos la ruta que ya calcula la señalización: bot y línea comparten camino
        // y el cálculo se hace una sola vez.
        if (SenializacionRuta.Instancia != null && SenializacionRuta.Instancia.isActiveAndEnabled)
        {
            IReadOnlyList<Vector3> compartida = SenializacionRuta.Instancia.RutaActual;
            if (compartida != null && compartida.Count >= 2) return compartida;
            return null;
        }

        // Si no hay señalización en la escena, el bot calcula su propia ruta.
        tiempoDesdeUltimoCalculo += Time.deltaTime;
        if (tiempoDesdeUltimoCalculo >= 0.4f)
        {
            tiempoDesdeUltimoCalculo = 0f;
            Transform objetivo = ObtenerObjetivoActual();
            if (objetivo == null) rutaPropia.Clear();
            else NavegacionSuelo.Instancia.CalcularRuta(jugador.position, objetivo.position, rutaPropia);
        }

        return rutaPropia;
    }

    private void MoverHacia(Vector3 puntoObjetivo)
    {
        Vector3 posicionPlana = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 objetivoPlano = new Vector3(puntoObjetivo.x, 0f, puntoObjetivo.z);
        Vector3 jugadorPlano = new Vector3(jugador.position.x, 0f, jugador.position.z);

        // Si el jugador se descuelga, el bot lo espera en lugar de irse solo.
        bool esperandoAlJugador = Vector3.Distance(posicionPlana, jugadorPlano) > distanciaMaximaDelJugador;

        Vector3 desplazamiento = objetivoPlano - posicionPlana;
        float distancia = desplazamiento.magnitude;

        if (esperandoAlJugador || distancia < 0.15f)
        {
            velocidadActual = Mathf.Lerp(velocidadActual, 0f, 10f * Time.deltaTime);
        }
        else
        {
            // Acelera un poco si se ha quedado rezagado respecto a su sitio en la ruta.
            float velocidadDeseada = velocidad * Mathf.Clamp(distancia / 1.5f, 0.35f, 1.6f);
            velocidadActual = Mathf.Lerp(velocidadActual, velocidadDeseada, 6f * Time.deltaTime);

            Vector3 direccion = desplazamiento / distancia;
            Vector3 paso = direccion * Mathf.Min(velocidadActual * Time.deltaTime, distancia);
            transform.position += paso;
        }

        // Mira hacia donde avanza; si está parado, se gira hacia el jugador.
        Vector3 direccionMirada = velocidadActual > 0.1f && distancia > 0.15f
            ? desplazamiento.normalized
            : (jugadorPlano - posicionPlana);

        if (direccionMirada.sqrMagnitude > 0.001f)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccionMirada.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime);
        }
    }

    private void PegarAlSuelo()
    {
        if (NavegacionSuelo.Instancia.AlturaDelSuelo(transform.position, out float alturaSuelo))
        {
            alturaObjetivo = alturaSuelo + alturaSobreSuelo;
        }

        Vector3 posicion = transform.position;
        posicion.y = Mathf.Lerp(posicion.y, alturaObjetivo, suavizadoVertical * Time.deltaTime);
        transform.position = posicion;
    }

    private void AnimarCaminata()
    {
        if (cuerpo == null) return;

        faseCaminata += velocidadActual * 3.2f * Time.deltaTime;

        float amplitud = Mathf.Clamp01(velocidadActual / velocidad) * 38f;
        float balanceo = Mathf.Sin(faseCaminata) * amplitud;

        if (piernaIzquierda != null) piernaIzquierda.localRotation = Quaternion.Euler(balanceo, 0f, 0f);
        if (piernaDerecha != null) piernaDerecha.localRotation = Quaternion.Euler(-balanceo, 0f, 0f);
        if (brazoIzquierdo != null) brazoIzquierdo.localRotation = Quaternion.Euler(-balanceo * 0.6f, 0f, 0f);
        if (brazoDerecho != null) brazoDerecho.localRotation = Quaternion.Euler(balanceo * 0.6f, 0f, 0f);

        // Rebote corto del torso, el del propio paso: nada de flotar.
        float rebote = Mathf.Abs(Mathf.Sin(faseCaminata)) * altura * 0.02f * Mathf.Clamp01(velocidadActual / velocidad);
        cuerpo.localPosition = new Vector3(0f, rebote, 0f);
    }

    public Transform ObtenerObjetivoActual()
    {
        BaldosaPregunta masCercanaSinResponder = null;
        float distanciaMinima = float.MaxValue;

        foreach (BaldosaPregunta baldosa in baldosas)
        {
            if (baldosa == null || baldosa.EstaRespondida) continue;

            float distancia = Vector3.Distance(jugador.position, baldosa.transform.position);
            if (distancia < distanciaMinima)
            {
                distanciaMinima = distancia;
                masCercanaSinResponder = baldosa;
            }
        }

        if (masCercanaSinResponder != null) return masCercanaSinResponder.transform;

        if (GameManager.Instance != null && GameManager.Instance.puertaFinalOficina != null)
            return GameManager.Instance.puertaFinalOficina.transform;

        return null;
    }
}

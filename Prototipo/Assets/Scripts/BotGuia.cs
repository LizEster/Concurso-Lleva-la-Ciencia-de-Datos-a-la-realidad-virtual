using UnityEngine;

public class BotGuia : MonoBehaviour
{
    [Header("Seguimiento del jugador")]
    [Tooltip("Si lo dejas vacío, lo busca automáticamente por el tag 'Player'")]
    public Transform jugador;
    public float alturaBase = 1.6f;
    public float distanciaHaciaObjetivo = 1.2f;
    public float suavizadoMovimiento = 4f;

    [Header("Flotación")]
    public float amplitudFlote = 0.15f;
    public float velocidadFlote = 2f;

    [Header("Giro hacia el objetivo")]
    public float velocidadRotacion = 5f;

    [Header("Haz guía")]
    [Tooltip("Opcional: asigna un material propio (ej. uno neón). Si lo dejas vacío, se genera uno básico automáticamente.")]
    public Material materialHaz;
    public Color colorHaz = new Color(0f, 1f, 1f, 0.9f);
    public float anchoHaz = 0.04f;

    private BaldosaPregunta[] baldosas;
    private float tiempo;
    private LineRenderer haz;

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

        baldosas = FindObjectsOfType<BaldosaPregunta>();
        Debug.Log($"[BotGuia] Inicializado. Jugador encontrado: {jugador != null}. Baldosas detectadas: {baldosas.Length}.");

        ConfigurarHaz();
    }

    private void ConfigurarHaz()
    {
        haz = gameObject.AddComponent<LineRenderer>();
        haz.positionCount = 2;
        haz.startWidth = anchoHaz;
        haz.endWidth = anchoHaz * 0.4f;
        haz.useWorldSpace = true;
        haz.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        haz.receiveShadows = false;

        if (materialHaz != null)
        {
            haz.material = materialHaz;
        }
        else
        {
            Shader shaderHaz = Shader.Find("Universal Render Pipeline/Unlit");
            if (shaderHaz == null) shaderHaz = Shader.Find("Unlit/Color");
            haz.material = new Material(shaderHaz);
        }

        haz.material.color = colorHaz;
        haz.startColor = colorHaz;
        haz.endColor = new Color(colorHaz.r, colorHaz.g, colorHaz.b, 0.1f);
    }

    void Update()
    {
        if (jugador == null) return;

        tiempo += Time.deltaTime * velocidadFlote;

        Transform objetivo = ObtenerObjetivoActual();
        Vector3 direccionObjetivo = jugador.forward;

        if (objetivo != null)
        {
            Vector3 haciaObjetivo = objetivo.position - jugador.position;
            haciaObjetivo.y = 0f;
            if (haciaObjetivo.sqrMagnitude > 0.01f) direccionObjetivo = haciaObjetivo.normalized;
        }

        Vector3 posicionDeseada = jugador.position
            + direccionObjetivo * distanciaHaciaObjetivo
            + Vector3.up * (alturaBase + Mathf.Sin(tiempo) * amplitudFlote);

        transform.position = Vector3.Lerp(transform.position, posicionDeseada, suavizadoMovimiento * Time.deltaTime);

        if (direccionObjetivo.sqrMagnitude > 0.01f)
        {
            Quaternion rotacionDeseada = Quaternion.LookRotation(direccionObjetivo);
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime);
        }

        if (haz != null)
        {
            if (objetivo != null)
            {
                haz.enabled = true;
                haz.SetPosition(0, transform.position);
                haz.SetPosition(1, objetivo.position);
            }
            else
            {
                haz.enabled = false;
            }
        }
    }

    private Transform ObtenerObjetivoActual()
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

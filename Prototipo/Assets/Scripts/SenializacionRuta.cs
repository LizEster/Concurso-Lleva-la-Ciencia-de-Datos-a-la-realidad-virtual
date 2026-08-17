using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SenializacionRuta : MonoBehaviour
{
    [Header("Seguimiento del jugador")]
    [Tooltip("Si lo dejas vacío, lo busca automáticamente por el tag 'Player'")]
    public Transform jugador;

    [Header("Apariencia")]
    public Color colorFlecha = new Color(0f, 1f, 1f, 1f);
    public float alturaSobreSuelo = 0.05f;
    public float espaciadoFlechas = 1.2f;
    public float tamanoFlecha = 0.4f;

    [Header("Actualización de ruta")]
    [Tooltip("Cada cuántos segundos recalcula el camino (por rendimiento)")]
    public float intervaloRecalculo = 0.3f;

    private BaldosaPregunta[] baldosas;
    private Mesh mallaFlecha;
    private Material materialFlecha;
    private readonly List<GameObject> flechas = new List<GameObject>();
    private float tiempoDesdeUltimoCalculo;
    private NavMeshPath rutaCalculada;
    private bool avisoSinNavMesh;

    void Start()
    {
        if (jugador == null)
        {
            GameObject playerObjeto = GameObject.FindGameObjectWithTag("Player");
            if (playerObjeto != null) jugador = playerObjeto.transform;
        }

        if (jugador == null)
        {
            Debug.LogWarning("[SenializacionRuta] No se encontró ningún GameObject con el tag 'Player'.");
        }

        baldosas = FindObjectsOfType<BaldosaPregunta>();
        rutaCalculada = new NavMeshPath();

        mallaFlecha = CrearMallaFlecha();

        Shader shaderFlecha = Shader.Find("Universal Render Pipeline/Unlit");
        if (shaderFlecha == null) shaderFlecha = Shader.Find("Unlit/Color");
        materialFlecha = new Material(shaderFlecha);
        materialFlecha.color = colorFlecha;
        if (materialFlecha.HasProperty("_Cull")) materialFlecha.SetFloat("_Cull", 0f); // Doble cara, se ve desde cualquier ángulo

        Debug.Log($"[SenializacionRuta] Inicializado. Jugador encontrado: {jugador != null}. Baldosas detectadas: {baldosas.Length}.");

        ActualizarRuta();
    }

    void Update()
    {
        if (jugador == null) return;

        tiempoDesdeUltimoCalculo += Time.deltaTime;
        if (tiempoDesdeUltimoCalculo >= intervaloRecalculo)
        {
            tiempoDesdeUltimoCalculo = 0f;
            ActualizarRuta();
        }
    }

    private void ActualizarRuta()
    {
        Transform objetivo = ObtenerObjetivoActual();
        if (objetivo == null)
        {
            OcultarFlechas();
            return;
        }

        Vector3 origen = jugador.position;
        Vector3 destino = objetivo.position;

        bool huboRuta = NavMesh.CalculatePath(origen, destino, NavMesh.AllAreas, rutaCalculada)
            && rutaCalculada.status == NavMeshPathStatus.PathComplete
            && rutaCalculada.corners.Length > 1;

        if (huboRuta)
        {
            avisoSinNavMesh = false;
            ColocarFlechasEnRuta(new List<Vector3>(rutaCalculada.corners));
        }
        else
        {
            if (!avisoSinNavMesh)
            {
                avisoSinNavMesh = true;
                Debug.LogWarning("[SenializacionRuta] No hay un NavMesh horneado (o el jugador/objetivo no está sobre él). No se puede calcular un camino confiable, así que no se muestran flechas.");
            }
            OcultarFlechas();
        }
    }

    private void ColocarFlechasEnRuta(List<Vector3> puntos)
    {
        int indiceFlecha = 0;

        for (int i = 0; i < puntos.Count - 1; i++)
        {
            Vector3 inicio = puntos[i];
            Vector3 fin = puntos[i + 1];
            float largoSegmento = Vector3.Distance(inicio, fin);
            if (largoSegmento < 0.05f) continue;

            Vector3 direccion = (fin - inicio).normalized;
            int cantidad = Mathf.Max(1, Mathf.FloorToInt(largoSegmento / espaciadoFlechas));

            for (int j = 0; j < cantidad; j++)
            {
                float t = (j + 0.5f) / cantidad;
                Vector3 posicion = Vector3.Lerp(inicio, fin, t) + Vector3.up * alturaSobreSuelo;

                GameObject flecha = ObtenerFlechaPooled(indiceFlecha);
                flecha.transform.position = posicion;
                flecha.transform.rotation = Quaternion.LookRotation(direccion, Vector3.up);
                flecha.SetActive(true);
                indiceFlecha++;
            }
        }

        for (int k = indiceFlecha; k < flechas.Count; k++)
        {
            flechas[k].SetActive(false);
        }
    }

    private GameObject ObtenerFlechaPooled(int indice)
    {
        if (indice < flechas.Count) return flechas[indice];

        GameObject nueva = new GameObject($"Flecha_{indice}");
        nueva.transform.SetParent(transform);
        nueva.transform.localScale = Vector3.one * tamanoFlecha;

        MeshFilter mf = nueva.AddComponent<MeshFilter>();
        mf.mesh = mallaFlecha;

        MeshRenderer mr = nueva.AddComponent<MeshRenderer>();
        mr.material = materialFlecha;
        mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        mr.receiveShadows = false;

        flechas.Add(nueva);
        return nueva;
    }

    private void OcultarFlechas()
    {
        foreach (GameObject f in flechas) f.SetActive(false);
    }

    private Mesh CrearMallaFlecha()
    {
        Mesh malla = new Mesh();
        malla.name = "MallaFlechaGuia";

        Vector3[] vertices = new Vector3[]
        {
            new Vector3(0f, 0f, 0.5f),
            new Vector3(-0.3f, 0f, -0.3f),
            new Vector3(0.3f, 0f, -0.3f),
        };

        int[] triangulos = new int[] { 0, 1, 2, 0, 2, 1 }; // Ambos sentidos por si acaso, aunque el material ya es de doble cara

        malla.vertices = vertices;
        malla.triangles = triangulos;
        malla.RecalculateNormals();
        malla.RecalculateBounds();

        return malla;
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

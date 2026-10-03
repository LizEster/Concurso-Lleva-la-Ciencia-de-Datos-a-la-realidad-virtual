using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Fondo del menú: una "red neuronal / grafo" color turquesa que flota alrededor de la cabeza.
/// Los nodos se mueven despacio y se conectan con líneas cuando están cerca (los más conectados
/// se ven más grandes). Todo se dibuja en UN solo mesh y UN solo material (barato para el celular).
///
/// No hay que ponerlo en la escena: lo crea MenuPrincipal.
/// Se dibuja con orden 1450: encima del velo negro (1400) y debajo del panel del menú (1500).
/// </summary>
public class FondoGrafos : MonoBehaviour
{
    [Header("Red")]
    public int cantidadNodos = 55;
    public float radioMin = 2.5f;          // los nodos nunca se acercan más que esto a la cabeza
    public float radioMax = 7f;
    public float distanciaEnlace = 2.8f;   // dos nodos más cerca que esto se unen con una línea
    public float velocidadMin = 0.12f;     // m/s
    public float velocidadMax = 0.35f;

    [Header("Look")]
    public Color colorBase = new Color(0.2f, 0.9f, 1f, 1f);   // mismo cian del menú
    public float opacidadLineas = 0.55f;
    public float tamanoNodo = 0.022f;      // ~ tamaño en pantalla (proporcional a la distancia)
    public float grosorLinea = 0.0035f;    // idem
    public int ordenDibujo = 1450;

    // ---------------------------------------------------------------

    private struct Nodo
    {
        public Vector3 pos, vel;
        public float fase, tamano, mezclaBlanco;
        public int grado;
    }

    private Nodo[] nodos;
    private int[] enlaceA, enlaceB;
    private float[] enlaceAlpha;
    private int numEnlaces;

    private Mesh mesh;
    private Material material;
    private Texture2D texturaCirculo;

    private readonly List<Vector3> vertices = new List<Vector3>();
    private readonly List<Color> colores = new List<Color>();
    private readonly List<Vector2> uvs = new List<Vector2>();
    private readonly List<int> triangulos = new List<int>();

    private float opacidadGlobal;   // para aparecer / desaparecer
    private bool saliendo;

    /// <summary>Crea el fondo como hijo de 'padre'.</summary>
    public static FondoGrafos Crear(Transform padre)
    {
        GameObject go = new GameObject("FondoGrafos");
        go.transform.SetParent(padre, false);
        return go.AddComponent<FondoGrafos>();
    }

    /// <summary>Lo apaga con un fundido y se destruye solo al terminar.</summary>
    public void Desvanecer(float duracion)
    {
        if (!saliendo) StartCoroutine(Salir(duracion));
    }

    // ---------------------------------------------------------------

    void Start()
    {
        CrearTextura();
        CrearMaterialYMesh();
        CrearNodos();
        CentrarEnCabeza();
    }

    void OnDestroy()
    {
        if (mesh != null) Destroy(mesh);
        if (material != null) Destroy(material);
        if (texturaCirculo != null) Destroy(texturaCirculo);
    }

    void LateUpdate()
    {
        if (nodos == null) return;

        if (!saliendo) opacidadGlobal = Mathf.MoveTowards(opacidadGlobal, 1f, Time.deltaTime / 1.5f);

        CentrarEnCabeza();
        Mover(Time.deltaTime);
        CalcularEnlaces();
        ConstruirMesh();
    }

    // ---------------------------------------------------------------
    // SETUP
    // ---------------------------------------------------------------

    /// <summary>Textura de un punto con brillo suave (centro opaco, borde transparente).</summary>
    private void CrearTextura()
    {
        const int n = 64;
        texturaCirculo = new Texture2D(n, n, TextureFormat.RGBA32, false);
        texturaCirculo.wrapMode = TextureWrapMode.Clamp;
        texturaCirculo.filterMode = FilterMode.Bilinear;

        Color32[] px = new Color32[n * n];
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float dx = (x + 0.5f) / n * 2f - 1f;
                float dy = (y + 0.5f) / n * 2f - 1f;
                float r = Mathf.Sqrt(dx * dx + dy * dy);
                float nucleo = Mathf.SmoothStep(1f, 0.55f, r);           // punto sólido
                float halo = Mathf.Pow(Mathf.Clamp01(1f - r), 2f) * 0.5f; // brillo alrededor
                float a = Mathf.Clamp01(Mathf.Max(nucleo, halo));
                px[y * n + x] = new Color32(255, 255, 255, (byte)(a * 255f));
            }
        }
        texturaCirculo.SetPixels32(px);
        texturaCirculo.Apply(false, true);
    }

    private void CrearMaterialYMesh()
    {
        // Mismo material base que usa el menú (UI/Default): siempre está incluido en el build.
        material = new Material(Canvas.GetDefaultCanvasMaterial());
        material.mainTexture = texturaCirculo;
        material.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always); // se ve aunque haya paredes

        mesh = new Mesh { name = "FondoGrafos" };
        mesh.MarkDynamic();

        MeshFilter mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = gameObject.AddComponent<MeshRenderer>();
        mr.sharedMaterial = material;
        mr.sortingOrder = ordenDibujo;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;
        mr.lightProbeUsage = LightProbeUsage.Off;
        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    private void CrearNodos()
    {
        nodos = new Nodo[cantidadNodos];
        int maxEnlaces = cantidadNodos * (cantidadNodos - 1) / 2;
        enlaceA = new int[maxEnlaces];
        enlaceB = new int[maxEnlaces];
        enlaceAlpha = new float[maxEnlaces];

        for (int i = 0; i < cantidadNodos; i++)
        {
            float radio = Random.Range(radioMin, radioMax);
            nodos[i].pos = Random.onUnitSphere * radio;
            nodos[i].vel = Random.onUnitSphere * Random.Range(velocidadMin, velocidadMax);
            nodos[i].fase = Random.value * Mathf.PI * 2f;
            nodos[i].tamano = Random.Range(0.7f, 1.5f);
            nodos[i].mezclaBlanco = Random.Range(0f, 0.35f);
        }
    }

    // ---------------------------------------------------------------
    // SIMULACIÓN
    // ---------------------------------------------------------------

    /// <summary>La red va centrada en la cabeza (solo posición, no rotación).</summary>
    private void CentrarEnCabeza()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null && Camera.main != null) cabeza = Camera.main.transform;
        if (cabeza != null) transform.position = cabeza.position;
        transform.rotation = Quaternion.identity;
    }

    private void Mover(float dt)
    {
        for (int i = 0; i < nodos.Length; i++)
        {
            Nodo n = nodos[i];

            // Deriva suave: un empujoncito aleatorio y velocidad acotada.
            n.vel += Random.insideUnitSphere * (0.25f * dt);
            float rapidez = n.vel.magnitude;
            if (rapidez < 0.0001f) n.vel = Random.onUnitSphere * velocidadMin;
            else n.vel *= Mathf.Clamp(rapidez, velocidadMin, velocidadMax) / rapidez;

            n.pos += n.vel * dt;

            // Rebotan entre dos esferas (adentro y afuera) para quedar siempre a la vista.
            float r = n.pos.magnitude;
            if (r > radioMax)
            {
                Vector3 hacia = n.pos / r;
                n.pos = hacia * radioMax;
                if (Vector3.Dot(n.vel, hacia) > 0f) n.vel = Vector3.Reflect(n.vel, -hacia);
            }
            else if (r < radioMin && r > 0.0001f)
            {
                Vector3 hacia = n.pos / r;
                n.pos = hacia * radioMin;
                if (Vector3.Dot(n.vel, hacia) < 0f) n.vel = Vector3.Reflect(n.vel, hacia);
            }

            nodos[i] = n;
        }
    }

    private void CalcularEnlaces()
    {
        numEnlaces = 0;
        for (int i = 0; i < nodos.Length; i++) nodos[i].grado = 0;

        float limite2 = distanciaEnlace * distanciaEnlace;
        for (int i = 0; i < nodos.Length; i++)
        {
            for (int j = i + 1; j < nodos.Length; j++)
            {
                float d2 = (nodos[i].pos - nodos[j].pos).sqrMagnitude;
                if (d2 >= limite2) continue;

                float cercania = 1f - Mathf.Sqrt(d2) / distanciaEnlace; // 1 = pegados, 0 = lejos
                enlaceA[numEnlaces] = i;
                enlaceB[numEnlaces] = j;
                enlaceAlpha[numEnlaces] = cercania;
                numEnlaces++;
                nodos[i].grado++;
                nodos[j].grado++;
            }
        }
    }

    // ---------------------------------------------------------------
    // MESH (todo en un solo draw call)
    // ---------------------------------------------------------------

    private void ConstruirMesh()
    {
        vertices.Clear();
        colores.Clear();
        uvs.Clear();
        triangulos.Clear();

        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null && Camera.main != null) cabeza = Camera.main.transform;
        Vector3 posCamara = cabeza != null ? cabeza.position - transform.position : Vector3.zero;
        Vector3 derecha = cabeza != null ? cabeza.right : Vector3.right;
        Vector3 arriba = cabeza != null ? cabeza.up : Vector3.up;

        float t = Time.time;
        Vector2 centroTextura = new Vector2(0.5f, 0.5f); // zona opaca: para las líneas

        // --- Líneas (tiras que miran a la cámara) ---
        for (int e = 0; e < numEnlaces; e++)
        {
            Vector3 p0 = nodos[enlaceA[e]].pos;
            Vector3 p1 = nodos[enlaceB[e]].pos;
            Vector3 medio = (p0 + p1) * 0.5f;
            Vector3 dir = p1 - p0;
            Vector3 haciaCamara = posCamara - medio;

            Vector3 lado = Vector3.Cross(dir, haciaCamara);
            if (lado.sqrMagnitude < 1e-8f) continue;
            lado.Normalize();

            // Grosor proporcional a la distancia: se ve parecido de cerca y de lejos.
            float ancho = Mathf.Clamp(grosorLinea * haciaCamara.magnitude, 0.008f, 0.04f) * 0.5f;

            float a = enlaceAlpha[e] * opacidadLineas * opacidadGlobal;
            Color c = colorBase;
            c.a = a;

            int v = vertices.Count;
            vertices.Add(p0 - lado * ancho); vertices.Add(p0 + lado * ancho);
            vertices.Add(p1 + lado * ancho); vertices.Add(p1 - lado * ancho);
            for (int k = 0; k < 4; k++) { colores.Add(c); uvs.Add(centroTextura); }
            triangulos.Add(v); triangulos.Add(v + 1); triangulos.Add(v + 2);
            triangulos.Add(v); triangulos.Add(v + 2); triangulos.Add(v + 3);
        }

        // --- Nodos (puntos con brillo que miran a la cámara) ---
        for (int i = 0; i < nodos.Length; i++)
        {
            Nodo n = nodos[i];
            float dist = Mathf.Max(0.5f, (posCamara - n.pos).magnitude);
            float tam = tamanoNodo * dist * n.tamano * (1f + 0.12f * n.grado);
            if (tam > 0.35f) tam = 0.35f;

            float latido = 0.75f + 0.25f * Mathf.Sin(t * 1.6f + n.fase);
            Color c = Color.Lerp(colorBase, Color.white, n.mezclaBlanco);
            c.a = latido * opacidadGlobal;

            Vector3 d = derecha * tam;
            Vector3 u = arriba * tam;

            int v = vertices.Count;
            vertices.Add(n.pos - d - u); vertices.Add(n.pos + d - u);
            vertices.Add(n.pos + d + u); vertices.Add(n.pos - d + u);
            uvs.Add(new Vector2(0f, 0f)); uvs.Add(new Vector2(1f, 0f));
            uvs.Add(new Vector2(1f, 1f)); uvs.Add(new Vector2(0f, 1f));
            for (int k = 0; k < 4; k++) colores.Add(c);
            triangulos.Add(v); triangulos.Add(v + 1); triangulos.Add(v + 2);
            triangulos.Add(v); triangulos.Add(v + 2); triangulos.Add(v + 3);
        }

        mesh.Clear(false);
        mesh.SetVertices(vertices);
        mesh.SetColors(colores);
        mesh.SetUVs(0, uvs);
        mesh.SetTriangles(triangulos, 0, false);
        // Bounds grandes fijos: así Unity nunca lo descarta por estar "fuera de cámara".
        mesh.bounds = new Bounds(Vector3.zero, Vector3.one * (radioMax * 3f));
    }

    // ---------------------------------------------------------------

    private IEnumerator Salir(float duracion)
    {
        saliendo = true;
        float inicio = opacidadGlobal;
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            opacidadGlobal = Mathf.Lerp(inicio, 0f, Mathf.Clamp01(tiempo / duracion));
            yield return null;
        }
        Destroy(gameObject);
    }
}

using UnityEngine;

/// <summary>
/// Viñeta de confort para VR (anti-mareo).
///
/// Mientras el jugador se mueve con el joystick, cae o salta, los bordes de la vista se
/// oscurecen suavemente y el centro queda limpio. La visión periférica es la que más
/// "siente" el movimiento: si se tapa un poco, el cerebro recibe menos señal contradictoria
/// con el oído interno y la persona se marea menos. Quieto, la viñeta desaparece entera.
///
/// La crea PlayerMovement al armar el jugador VR (solo en VR; en PC no hace nada).
/// Es un anillo de malla pegado a la cámara, sin post-proceso, así que es barato en el celular.
/// </summary>
public class VinetaConfort : MonoBehaviour
{
    [Tooltip("Qué tan oscuro llega el borde moviéndose a máxima velocidad (0 = nada, 1 = negro total). Sutil: 0.6-0.8.")]
    [Range(0f, 1f)] public float intensidadMaxima = 0.7f;
    [Tooltip("Ángulo desde el centro de la vista que queda siempre limpio (grados). Más grande = más sutil.")]
    public float anguloLimpio = 38f;
    [Tooltip("Ancho del degradado entre lo limpio y lo oscuro (grados). Más grande = borde más suave.")]
    public float anguloDegradado = 24f;
    [Tooltip("Velocidad vertical (m/s) desde la que empieza a aparecer al caer o saltar.")]
    public float verticalMinima = 4f;
    [Tooltip("Velocidad vertical (m/s) en que llega al máximo al caer o saltar.")]
    public float verticalMaxima = 9f;
    [Tooltip("Qué tan rápido entra y sale la viñeta.")]
    public float suavizado = 5f;

    private const int Segmentos = 48;

    private PlayerMovement jugador;
    private Transform cabeza;
    private Mesh mesh;
    private MeshRenderer render;
    private Color32[] colores;
    private float[] alfas;
    private float intensidad;
    private float intensidadDibujada = -1f;

    public void Configurar(PlayerMovement jugador, Transform cabeza)
    {
        this.jugador = jugador;
        this.cabeza = cabeza;
        Construir();
    }

    void LateUpdate()
    {
        if (jugador == null || cabeza == null || mesh == null) return;

        float objetivo = 0f;
        if (jugador.enabled)
        {
            Vector3 v = jugador.VelocidadActual;
            float horizontal = new Vector2(v.x, v.z).magnitude / Mathf.Max(0.01f, jugador.velocidadVR);
            float vertical = Mathf.InverseLerp(verticalMinima, verticalMaxima, Mathf.Abs(v.y));
            objetivo = Mathf.Clamp01(Mathf.Max(horizontal, vertical)) * intensidadMaxima;
        }

        intensidad = Mathf.Lerp(intensidad, objetivo, 1f - Mathf.Exp(-suavizado * Time.deltaTime));
        if (Mathf.Abs(intensidad - intensidadDibujada) < 0.003f) return;

        intensidadDibujada = intensidad;
        render.enabled = intensidad > 0.01f;
        if (!render.enabled) return;

        for (int k = 0; k < colores.Length; k++)
            colores[k] = new Color32(0, 0, 0, (byte)(Mathf.Clamp01(alfas[k] * intensidad) * 255f));
        mesh.colors32 = colores;
    }

    /// <summary>Anillo plano frente a los ojos: centro transparente -> degradado -> negro hasta fuera de la vista.</summary>
    private void Construir()
    {
        Camera cam = cabeza.GetComponent<Camera>();
        float d = cam != null ? Mathf.Max(cam.nearClipPlane * 1.2f, 0.05f) : 0.3f;

        float a0 = Mathf.Clamp(anguloLimpio, 5f, 75f);
        float a1 = Mathf.Clamp(a0 + anguloDegradado, a0 + 1f, 82f);
        float[] radios = { d * Mathf.Tan(a0 * Mathf.Deg2Rad), d * Mathf.Tan(a1 * Mathf.Deg2Rad), d * Mathf.Tan(88f * Mathf.Deg2Rad) };
        float[] alfaAnillo = { 0f, 1f, 1f };

        int n = Segmentos;
        Vector3[] vertices = new Vector3[n * 3];
        colores = new Color32[n * 3];
        alfas = new float[n * 3];
        for (int anillo = 0; anillo < 3; anillo++)
        {
            for (int i = 0; i < n; i++)
            {
                float ang = i * Mathf.PI * 2f / n;
                int k = anillo * n + i;
                vertices[k] = new Vector3(Mathf.Cos(ang) * radios[anillo], Mathf.Sin(ang) * radios[anillo], d);
                alfas[k] = alfaAnillo[anillo];
                colores[k] = new Color32(0, 0, 0, 0);
            }
        }

        int[] triangulos = new int[n * 2 * 6];
        int t = 0;
        for (int anillo = 0; anillo < 2; anillo++)
        {
            for (int i = 0; i < n; i++)
            {
                int a = anillo * n + i;
                int b = anillo * n + (i + 1) % n;
                int c = (anillo + 1) * n + i;
                int e = (anillo + 1) * n + (i + 1) % n;
                triangulos[t++] = a; triangulos[t++] = c; triangulos[t++] = b;
                triangulos[t++] = b; triangulos[t++] = c; triangulos[t++] = e;
            }
        }

        mesh = new Mesh { name = "VinetaConfort" };
        mesh.vertices = vertices;
        mesh.triangles = triangulos;
        mesh.colors32 = colores;
        mesh.bounds = new Bounds(Vector3.forward * d, Vector3.one * 1000f); // que nunca la descarte el frustum culling

        GameObject go = new GameObject("[VinetaConfort]");
        go.layer = cabeza.gameObject.layer;
        go.transform.SetParent(cabeza, false);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        render = go.AddComponent<MeshRenderer>();
        render.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        render.receiveShadows = false;

        Shader shader = Shader.Find("Sprites/Default"); // usa el color de los vértices y es transparente
        Material mat = new Material(shader) { renderQueue = 4000 }; // encima de todo lo demás
        render.sharedMaterial = mat;
        render.enabled = false;
    }
}

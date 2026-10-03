#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// SOLO PARA PROBAR EN EL EDITOR con Unity Remote 5 (no se incluye en el APK).
/// Mueve la cámara del jugador con el giroscopio del celular conectado por cable,
/// para probar "mirar alrededor" sin compilar. Se activa solo: no hay que arrastrarlo a nada.
///
/// Teclas (en el Mac, con la ventana Game enfocada):
///   R = recentrar y re-detectar la orientación (sostén el celular derecho, en horizontal, mirando al frente)
///   T = cambiar a mano la corrección de giro (0°, 90°, -90°, 180°) si la detección automática falla
///   G = apagar/prender el giroscopio (vuelve al mouse)
/// </summary>
[DefaultExecutionOrder(1000)] // después de MouseLook, para que el giroscopio mande
public class GiroscopioEditor : MonoBehaviour
{
    // Corrección por tener el celular en horizontal. Se detecta sola al recentrar (R);
    // la tecla T la cambia a mano.
    private static readonly float[] CorreccionesZ = { 0f, 90f, -90f, 180f };
    private int indiceCorreccion = 1;

    private bool activo = true;
    private float offsetYaw;
    private bool necesitaRecentrar = true;

    private Transform cuerpo;
    private Transform camara;
    private MouseLook mouseLook;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AlIniciar()
    {
        var go = new GameObject("[GiroscopioEditor]");
        DontDestroyOnLoad(go);
        go.AddComponent<GiroscopioEditor>();
    }

    private void OnEnable()  => SceneManager.sceneLoaded += AlCargarEscena;
    private void OnDisable() => SceneManager.sceneLoaded -= AlCargarEscena;

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        cuerpo = null;
        camara = null;
        necesitaRecentrar = true;
    }

    private void Start()
    {
        if (AttitudeSensor.current != null) InputSystem.EnableDevice(AttitudeSensor.current);
#if ENABLE_LEGACY_INPUT_MANAGER
        Input.gyro.enabled = true; // respaldo con el Input antiguo
#endif
    }

    private bool LeerActitud(out Quaternion actitud)
    {
        actitud = Quaternion.identity;

        var sensor = AttitudeSensor.current;
        if (sensor != null)
        {
            if (!sensor.enabled) InputSystem.EnableDevice(sensor);
            actitud = sensor.attitude.ReadValue();
            if (actitud != Quaternion.identity && actitud != default) return true;
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        if (Input.gyro.enabled)
        {
            actitud = Input.gyro.attitude;
            if (actitud != Quaternion.identity && actitud != default) return true;
        }
#endif
        return false;
    }

    private void BuscarJugador()
    {
        var movimiento = Object.FindAnyObjectByType<PlayerMovement>();
        if (movimiento == null) return;
        var cam = movimiento.GetComponentInChildren<Camera>();
        if (cam == null) return;

        cuerpo = movimiento.transform;
        camara = cam.transform;
        mouseLook = cam.GetComponent<MouseLook>();
    }

    private void LateUpdate()
    {
        if (EntradaVR.VRActivo) return; // en el APK con Cardboard esto no aplica

        var teclado = Keyboard.current;
        if (teclado != null)
        {
            if (teclado.gKey.wasPressedThisFrame)
            {
                activo = !activo;
                if (mouseLook != null) mouseLook.enabled = !activo;
                Debug.Log($"GiroscopioEditor: {(activo ? "giroscopio" : "mouse")}");
            }
            if (teclado.rKey.wasPressedThisFrame) necesitaRecentrar = true;
            if (teclado.tKey.wasPressedThisFrame)
            {
                indiceCorreccion = (indiceCorreccion + 1) % CorreccionesZ.Length;
                Debug.Log($"GiroscopioEditor: corrección horizontal = {CorreccionesZ[indiceCorreccion]}°");
            }
        }

        if (!activo) return;
        if (cuerpo == null || camara == null) BuscarJugador();
        if (cuerpo == null || camara == null) return;
        if (!LeerActitud(out Quaternion q)) return; // Unity Remote no conectado o sin datos

        if (mouseLook != null && mouseLook.enabled) mouseLook.enabled = false;

        // Del sistema del celular (mano derecha) al de Unity (mano izquierda)
        Quaternion baseRot = Quaternion.Euler(90f, 0f, 0f) * new Quaternion(q.x, q.y, -q.z, -q.w);

        if (necesitaRecentrar)
        {
            // Elige la corrección que deja el "arriba" de la cámara más cerca del "arriba" real.
            // Así da igual hacia qué lado pusiste el celular en horizontal.
            float mejor = float.NegativeInfinity;
            for (int i = 0; i < CorreccionesZ.Length; i++)
            {
                Quaternion prueba = baseRot * Quaternion.Euler(0f, 0f, CorreccionesZ[i]);
                float puntaje = Vector3.Dot(prueba * Vector3.up, Vector3.up);
                if (puntaje > mejor) { mejor = puntaje; indiceCorreccion = i; }
            }
            Debug.Log($"GiroscopioEditor: orientación detectada, corrección = {CorreccionesZ[indiceCorreccion]}°");
        }

        Quaternion rot = baseRot * Quaternion.Euler(0f, 0f, CorreccionesZ[indiceCorreccion]);

        float yawCelular = rot.eulerAngles.y;
        if (necesitaRecentrar)
        {
            offsetYaw = cuerpo.eulerAngles.y - yawCelular;
            necesitaRecentrar = false;
        }

        // Giro a los lados -> cuerpo (así caminar sigue hacia donde miras)
        float yaw = yawCelular + offsetYaw;
        cuerpo.rotation = Quaternion.Euler(0f, yaw, 0f);

        // Arriba/abajo e inclinación -> cámara
        Quaternion soloYaw = Quaternion.Euler(0f, yawCelular, 0f);
        camara.localRotation = Quaternion.Inverse(soloYaw) * rot;
    }
}
#endif

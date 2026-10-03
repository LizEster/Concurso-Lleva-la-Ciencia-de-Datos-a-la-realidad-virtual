#if DEVELOPMENT_BUILD && !UNITY_EDITOR
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

/// <summary>
/// SOLO EN "Development Build" (no va en el APK final). Cada 2 segundos escribe en la
/// consola qué está pasando con el VR y el seguimiento de cabeza, para ver por qué la
/// cámara no se mueve. Se lee en Unity: Console → desplegable "Editor" → elegir el celular.
/// </summary>
public class DiagnosticoVR : MonoBehaviour
{
    private float siguiente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        var go = new GameObject("[DiagnosticoVR]");
        DontDestroyOnLoad(go);
        go.AddComponent<DiagnosticoVR>();
    }

    private void Update()
    {
        if (Time.unscaledTime < siguiente) return;
        siguiente = Time.unscaledTime + 2f;

        var sb = new System.Text.StringBuilder("[DiagVR] ");
        sb.Append($"VRActivo={XRSettings.isDeviceActive} dispositivo='{XRSettings.loadedDeviceName}' ");

        var hmd = InputSystem.GetDevice<XRHMD>();
        if (hmd == null)
        {
            sb.Append("XRHMD=NO ENCONTRADO. Dispositivos: ");
            foreach (var d in InputSystem.devices) sb.Append($"[{d.layout}:{d.name}] ");
        }
        else
        {
            sb.Append($"XRHMD='{hmd.name}' rotOjos={hmd.centerEyeRotation.ReadValue().eulerAngles} ");
            sb.Append($"trackingState={hmd.trackingState.ReadValue()} ");
        }

        Transform cabeza = RigVR.Cabeza;
        if (cabeza == null)
        {
            sb.Append("RigVR.Cabeza=null ");
        }
        else
        {
            var tpd = cabeza.GetComponent<TrackedPoseDriver>();
            sb.Append($"camara='{cabeza.name}' rotLocal={cabeza.localEulerAngles} ");
            sb.Append(tpd == null ? "TPD=NO " : $"TPD activo={tpd.isActiveAndEnabled} accionRot={tpd.rotationInput.action?.enabled} controles={tpd.rotationInput.action?.controls.Count} ");
        }

        var cams = Camera.allCameras;
        sb.Append($"camaras={cams.Length}");
        foreach (var c in cams) sb.Append($" [{c.name} prof={c.depth} rot={c.transform.eulerAngles}]");

        Debug.Log(sb.ToString());
    }
}
#endif

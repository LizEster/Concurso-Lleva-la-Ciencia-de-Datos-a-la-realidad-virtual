using Google.XR.Cardboard;
using UnityEngine;

/// <summary>
/// Lo que necesita Google Cardboard para funcionar en el visor Shinecon (basado en el
/// CardboardStartup del ejemplo oficial). Se crea solo al arrancar y sobrevive entre escenas:
///  - La primera vez pide escanear el código QR del visor (viene impreso en la caja o en el
///    visor; si no hay, se puede saltar y usa los parámetros de un Cardboard estándar).
///  - El engranaje de la pantalla vuelve a escanear y la X cierra el juego.
///  - Mantener el dedo en la pantalla recentra la vista.
/// Fuera del celular (en el editor) no hace nada.
/// </summary>
public class ArranqueCardboard : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (Application.platform != RuntimePlatform.Android) return;

        GameObject go = new GameObject("ArranqueCardboard");
        DontDestroyOnLoad(go);
        go.AddComponent<ArranqueCardboard>();
    }

    void Start()
    {
        // Dentro del visor nadie toca la pantalla: que no se apague sola.
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.brightness = 1f;

        if (!Api.HasDeviceParams())
        {
            Api.ScanDeviceParams();
        }
    }

    void Update()
    {
        if (Api.IsGearButtonPressed)
        {
            Api.ScanDeviceParams();
        }

        if (Api.IsCloseButtonPressed)
        {
            Application.Quit();
        }

        if (Api.IsTriggerHeldPressed)
        {
            Api.Recenter();
        }

        if (Api.HasNewDeviceParams())
        {
            Api.ReloadDeviceParams();
        }

        Api.UpdateScreenParams();
    }
}

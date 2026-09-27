using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Convierte al jugador FPS de cada escena en un jugador VR, sin tener que tocar las escenas:
/// se ejecuta solo al cargar cualquier escena (no hay que arrastrarlo a ningún objeto).
///  - La cámara del Player pasa a seguir la cabeza del visor (TrackedPoseDriver) y cuelga de
///    un "OrigenVR" a la altura de los pies, así la altura real del jugador es la del juego.
///  - Aparecen dos manos simples que siguen a los mandos.
///  - Los Canvas en modo pantalla (HUD, fundidos, pantallas finales) no se ven en un visor,
///    así que se pasan a paneles en el mundo que flotan delante de los ojos.
/// Si no hay visor activo (por ejemplo, Play en el editor sin Quest Link) no hace nada y el
/// juego se comporta como la versión de PC.
/// </summary>
public static class RigVR
{
    /// <summary>La cámara del jugador (la cabeza) en la escena actual; null si no hay rig VR.</summary>
    public static Transform Cabeza { get; private set; }

    private const float AlturaOjosSinSuelo = 1.6f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AlIniciar()
    {
        SceneManager.sceneLoaded += (escena, modo) => Configurar();
        Configurar();
    }

    private static void Configurar()
    {
        Cabeza = null;
        if (!EntradaVR.VRActivo) return;

        UsarSueloComoOrigen();

        PlayerMovement movimiento = Object.FindAnyObjectByType<PlayerMovement>();
        if (movimiento == null)
        {
            Debug.LogWarning("RigVR: no hay PlayerMovement en la escena, no se puede armar el jugador VR.");
            return;
        }
        if (movimiento.EsVR)
        {
            Cabeza = movimiento.CabezaVR;
            return;
        }

        Camera camara = movimiento.GetComponentInChildren<Camera>();
        if (camara == null)
        {
            Debug.LogWarning("RigVR: el Player no tiene una cámara hija.");
            return;
        }

        ArmarJugador(movimiento, camara);
        ConvertirCanvasDePantalla();
    }

    private static void ArmarJugador(PlayerMovement movimiento, Camera camara)
    {
        Transform jugador = movimiento.transform;
        CharacterController controller = movimiento.GetComponent<CharacterController>();

        // La cámara venía etiquetada "Player": eso hacía que FindGameObjectWithTag("Player")
        // a veces devolviera la cámara en vez del cuerpo, y que Camera.main fuera null.
        camara.tag = "MainCamera";
        camara.nearClipPlane = 0.05f;

        // El mouse y el PlayerInput (WASD, saltar con B/Y del mando) ya no se usan en VR.
        MouseLook mouseLook = camara.GetComponent<MouseLook>();
        if (mouseLook != null) mouseLook.enabled = false;
        PlayerInput playerInput = jugador.GetComponent<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;

        // La cápsula del Player se vería desde dentro y taparía las manos.
        MeshRenderer cuerpoVisible = jugador.GetComponent<MeshRenderer>();
        if (cuerpoVisible != null) cuerpoVisible.enabled = false;

        // El origen del tracking va en los pies de la cápsula: el visor aporta la altura.
        float alturaPies = controller != null ? controller.center.y - controller.height * 0.5f : -1f;
        Transform origen = new GameObject("OrigenVR").transform;
        origen.SetParent(jugador, false);
        origen.localPosition = new Vector3(0f, alturaPies + (SueloDisponible() ? 0f : AlturaOjosSinSuelo), 0f);
        origen.localRotation = Quaternion.identity;

        camara.transform.SetParent(origen, false);
        camara.transform.localPosition = Vector3.zero;
        camara.transform.localRotation = Quaternion.identity;
        AgregarSeguimiento(camara.gameObject, "<XRHMD>/centerEyePosition", "<XRHMD>/centerEyeRotation", "<XRHMD>/trackingState");

        CrearMano(origen, "ManoIzquierda", "{LeftHand}");
        CrearMano(origen, "ManoDerecha", "{RightHand}");

        Cabeza = camara.transform;
        movimiento.ConfigurarVR(Cabeza, origen);
    }

    private static void CrearMano(Transform origen, string nombre, string mano)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(origen, false);
        AgregarSeguimiento(go,
            $"<XRController>{mano}/devicePosition",
            $"<XRController>{mano}/deviceRotation",
            $"<XRController>{mano}/trackingState");

        // Mano simple: un "mando" alargado de color cian, a tono con los hologramas.
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        Object.Destroy(visual.GetComponent<Collider>());
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = new Vector3(0f, -0.01f, 0.03f);
        visual.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        visual.transform.localScale = new Vector3(0.04f, 0.06f, 0.04f);

        // Se parte del material por defecto de la primitiva (URP/Lit), que siempre viene en la build.
        Renderer renderer = visual.GetComponent<Renderer>();
        Material material = renderer.material;
        material.color = new Color(0.1f, 0.25f, 0.3f);
        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", new Color(0f, 0.8f, 1f) * 0.8f);
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static void AgregarSeguimiento(GameObject go, string posicion, string rotacion, string estado)
    {
        TrackedPoseDriver driver = go.AddComponent<TrackedPoseDriver>();
        driver.trackingType = TrackedPoseDriver.TrackingType.RotationAndPosition;
        driver.updateType = TrackedPoseDriver.UpdateType.UpdateAndBeforeRender;
        driver.positionInput = new InputActionProperty(new InputAction(binding: posicion, expectedControlType: "Vector3"));
        driver.rotationInput = new InputActionProperty(new InputAction(binding: rotacion, expectedControlType: "Quaternion"));
        driver.trackingStateInput = new InputActionProperty(new InputAction(binding: estado, expectedControlType: "Integer"));
    }

    private static readonly List<XRInputSubsystem> subsistemas = new List<XRInputSubsystem>();

    private static void UsarSueloComoOrigen()
    {
        SubsystemManager.GetSubsystems(subsistemas);
        foreach (XRInputSubsystem subsistema in subsistemas)
        {
            if ((subsistema.GetSupportedTrackingOriginModes() & TrackingOriginModeFlags.Floor) != 0)
                subsistema.TrySetTrackingOriginMode(TrackingOriginModeFlags.Floor);
        }
    }

    private static bool SueloDisponible()
    {
        SubsystemManager.GetSubsystems(subsistemas);
        foreach (XRInputSubsystem subsistema in subsistemas)
        {
            if (subsistema.GetTrackingOriginMode() == TrackingOriginModeFlags.Floor) return true;
        }
        return false;
    }

    /// <summary>
    /// Pasa a espacio de mundo todos los Canvas raíz que estaban en modo pantalla. El
    /// fundido blanco de ScreenFader queda pegado a los ojos (tapa toda la vista); el
    /// resto flota a un metro y sigue la mirada con suavidad para poder leerlo.
    /// </summary>
    private static void ConvertirCanvasDePantalla()
    {
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (!canvas.isRootCanvas || canvas.renderMode == RenderMode.WorldSpace) continue;
            if (canvas.GetComponent<SeguirCabezaVR>() != null) continue;

            bool esFundido = canvas.GetComponent<ScreenFader>() != null;
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            Vector2 tamano = scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize
                ? scaler.referenceResolution
                : new Vector2(1920f, 1080f);

            canvas.renderMode = RenderMode.WorldSpace;
            RectTransform rt = (RectTransform)canvas.transform;
            rt.sizeDelta = tamano;

            SeguirCabezaVR seguir = canvas.gameObject.AddComponent<SeguirCabezaVR>();
            if (esFundido)
            {
                seguir.distancia = 0.12f;
                seguir.anchoMetros = 0.6f;
                seguir.suavizado = 0f;
            }
            else
            {
                AgrandarFondosDePantallaCompleta(rt, tamano);
            }
            seguir.AplicarEscala();
        }
    }

    /// <summary>
    /// Los paneles que tapaban toda la pantalla (fundido a negro del final, fundido blanco
    /// de entrada) quedarían como un rectángulo flotando: los estiramos para que llenen la vista.
    /// </summary>
    private static void AgrandarFondosDePantallaCompleta(RectTransform canvas, Vector2 tamano)
    {
        foreach (Transform hijo in canvas)
        {
            if (!(hijo is RectTransform rt) || hijo.GetComponent<Image>() == null) continue;
            if (rt.anchorMin != Vector2.zero || rt.anchorMax != Vector2.one) continue;

            rt.offsetMin = -tamano * 3f;
            rt.offsetMax = tamano * 3f;
        }
    }
}

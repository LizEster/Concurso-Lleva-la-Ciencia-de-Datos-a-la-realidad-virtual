using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Punto único de entrada del juego en VR. Pensado para el visor Shinecon con su control
/// Bluetooth, que el celular reconoce como gamepad (o como joystick genérico, según el modo
/// del control). Reemplaza las teclas del prototipo de PC:
///   Elegir opciones      -> se apunta con la mirada y se aprieta A, B, X o Y (PunteroMirada)
///   E, Enter             -> botón A
///   Espacio (saltar)     -> gatillos / bumpers del control (L1, R1, L2, R2)
///   WASD                 -> joystick (o la cruceta)
///   Mouse (girar)        -> girar la cabeza; el stick derecho, si hay, gira por pasos
///                           (ver PlayerMovement). Flechas ←/→ en el editor.
/// Cada acción conserva también su tecla original, así se puede probar en el editor con el
/// teclado, y los mandos de visores con seguimiento (Quest, etc.) también funcionan.
/// </summary>
public static class EntradaVR
{
    public const string NombreInteractuar = "A";
    public const string NombreSaltar = "Gatillo";

    private static InputAction interactuar;
    private static InputAction confirmar;
    private static InputAction saltar;
    private static InputAction mover;
    private static InputAction girar;

    /// <summary>True si hay un visor activo renderizando (en el Shinecon: Cardboard corriendo).</summary>
    public static bool VRActivo => XRSettings.isDeviceActive;

    public static Vector2 Mover { get { Inicializar(); return mover.ReadValue<Vector2>(); } }
    public static Vector2 Girar { get { Inicializar(); return girar.ReadValue<Vector2>(); } }

    /// <summary>Botón A (o E / Enter) presionado este frame.</summary>
    public static bool InteractuarPresionado()
    {
        Inicializar();
        return interactuar.WasPressedThisFrame();
    }

    /// <summary>
    /// Cualquiera de los botones A, B, X o Y (o el gatillo de un joystick genérico) presionado
    /// este frame; en PC también E, Enter o clic. Es lo que confirma la opción que estás mirando.
    /// </summary>
    public static bool ConfirmarPresionado()
    {
        Inicializar();
        return confirmar.WasPressedThisFrame();
    }

    /// <summary>Gatillo o bumper del control (o Espacio) presionado este frame.</summary>
    public static bool SaltarPresionado()
    {
        Inicializar();
        return saltar.WasPressedThisFrame();
    }

    /// <summary>Cambia las teclas de PC que hayan quedado escritas en textos del Inspector por su botón del control.</summary>
    public static string TraducirTeclas(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return texto.Replace("[E]", $"[{NombreInteractuar}]").Replace("[Gatillo]", $"[{NombreInteractuar}]");
    }

    private static void Inicializar()
    {
        if (interactuar != null) return;

        interactuar = CrearBoton(
            "<Gamepad>/buttonSouth", "<Joystick>/trigger",
            "<XRController>{RightHand}/{TriggerButton}",
            "<Keyboard>/e", "<Keyboard>/enter");

        confirmar = CrearBoton(
            "<Gamepad>/buttonSouth", "<Gamepad>/buttonEast", "<Gamepad>/buttonWest", "<Gamepad>/buttonNorth",
            "<Joystick>/trigger", "<Joystick>/button2", "<Joystick>/button3", "<Joystick>/button4", "<Joystick>/button5",
            "<XRController>{RightHand}/{TriggerButton}", "<XRController>{RightHand}/{PrimaryButton}",
            "<Keyboard>/e", "<Keyboard>/enter", "<Mouse>/leftButton");

        saltar = CrearBoton(
            "<Gamepad>/rightTrigger", "<Gamepad>/leftTrigger", "<Gamepad>/rightShoulder", "<Gamepad>/leftShoulder",
            "<XRController>{LeftHand}/{TriggerButton}",
            "<Keyboard>/space");

        // Algunos controles de VR mandan el joystick como cruceta, por eso se leen los dos.
        mover = new InputAction("Mover", InputActionType.Value);
        mover.AddBinding("<Gamepad>/leftStick");
        mover.AddBinding("<Gamepad>/dpad");
        mover.AddBinding("<Joystick>/stick");
        mover.AddBinding("<XRController>{LeftHand}/{Primary2DAxis}");
        mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        mover.Enable();

        girar = new InputAction("Girar", InputActionType.Value);
        girar.AddBinding("<Gamepad>/rightStick");
        girar.AddBinding("<XRController>{RightHand}/{Primary2DAxis}");
        girar.AddCompositeBinding("2DVector")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        girar.Enable();
    }

    private static InputAction CrearBoton(params string[] rutas)
    {
        var accion = new InputAction(type: InputActionType.Button);
        foreach (string ruta in rutas) accion.AddBinding(ruta);
        accion.Enable();
        return accion;
    }
}

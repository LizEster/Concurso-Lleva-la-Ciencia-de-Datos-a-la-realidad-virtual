using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR;

/// <summary>
/// Punto único de entrada del juego en VR (mandos de Meta Quest vía OpenXR).
/// Reemplaza las teclas del prototipo de PC:
///   1 / 2 / 3 / 4        -> botones A / B / X / Y
///   E, Espacio, Enter    -> gatillo derecho
///   WASD                 -> stick izquierdo
///   Mouse (girar)        -> stick derecho (giro por pasos, ver PlayerMovement); flechas ←/→ en el editor
/// Cada acción conserva también su tecla original, así se puede probar en el editor
/// con el teclado sin tener el visor puesto.
/// </summary>
public static class EntradaVR
{
    /// <summary>Nombres de los botones tal como se muestran en pantalla, en el orden de las opciones 1..4.</summary>
    public static readonly string[] NombresOpciones = { "A", "B", "X", "Y" };
    public const string NombreInteractuar = "Gatillo";

    private static InputAction[] opciones;
    private static InputAction interactuar;
    private static InputAction mover;
    private static InputAction girar;

    /// <summary>True si hay un visor conectado y renderizando.</summary>
    public static bool VRActivo => XRSettings.isDeviceActive;

    public static Vector2 Mover { get { Inicializar(); return mover.ReadValue<Vector2>(); } }
    public static Vector2 Girar { get { Inicializar(); return girar.ReadValue<Vector2>(); } }

    /// <summary>Opción 0..3 presionada este frame (A, B, X, Y o las teclas 1..4).</summary>
    public static bool OpcionPresionada(int indice)
    {
        Inicializar();
        return indice >= 0 && indice < opciones.Length && opciones[indice].WasPressedThisFrame();
    }

    /// <summary>Gatillo derecho (o E / Espacio / Enter) presionado este frame.</summary>
    public static bool InteractuarPresionado()
    {
        Inicializar();
        return interactuar.WasPressedThisFrame();
    }

    /// <summary>Texto "[A]", "[B]"... para mostrar junto a la opción 'indice'.</summary>
    public static string EtiquetaOpcion(int indice)
    {
        return indice >= 0 && indice < NombresOpciones.Length ? $"[{NombresOpciones[indice]}]" : $"[{indice + 1}]";
    }

    /// <summary>Cambia las teclas de PC que hayan quedado escritas en textos del Inspector por su botón del mando.</summary>
    public static string TraducirTeclas(string texto)
    {
        if (string.IsNullOrEmpty(texto)) return texto;
        return texto.Replace("[E]", $"[{NombreInteractuar}]");
    }

    private static void Inicializar()
    {
        if (opciones != null) return;

        opciones = new[]
        {
            CrearBoton("<XRController>{RightHand}/{PrimaryButton}", "<Keyboard>/1", "<Keyboard>/numpad1"),
            CrearBoton("<XRController>{RightHand}/{SecondaryButton}", "<Keyboard>/2", "<Keyboard>/numpad2"),
            CrearBoton("<XRController>{LeftHand}/{PrimaryButton}", "<Keyboard>/3", "<Keyboard>/numpad3"),
            CrearBoton("<XRController>{LeftHand}/{SecondaryButton}", "<Keyboard>/4", "<Keyboard>/numpad4"),
        };

        interactuar = CrearBoton("<XRController>{RightHand}/{TriggerButton}", "<Keyboard>/e", "<Keyboard>/space", "<Keyboard>/enter");

        mover = new InputAction("Mover", InputActionType.Value, "<XRController>{LeftHand}/{Primary2DAxis}");
        mover.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        mover.Enable();

        girar = new InputAction("Girar", InputActionType.Value, "<XRController>{RightHand}/{Primary2DAxis}");
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

#if UNITY_EDITOR && ENABLE_LEGACY_INPUT_MANAGER // necesita el Input antiguo (Active Input Handling = Both)
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// SOLO PARA PROBAR EN EL EDITOR con Unity Remote 5 (no se incluye en el APK).
/// Unity Remote manda el control Bluetooth conectado al celular al Input antiguo
/// (Project Settings → Editor → Unity Remote → Joystick Source = Remote), pero el juego
/// usa el Input System nuevo. Este script lee el control por el Input antiguo y lo
/// copia a un gamepad virtual del Input System, así EntradaVR y el PlayerInput lo ven
/// como si estuviera conectado al Mac.
///
/// Arriba a la izquierda de la ventana Game aparece un cuadrito con lo que llega del
/// control (tecla H para esconderlo). Si un botón hace otra cosa, mira qué número
/// muestra ahí y cámbialo en la tabla de abajo.
/// </summary>
public class ControlRemotoEditor : MonoBehaviour
{
    // Número de botón (KeyCode.JoystickButtonN) de cada botón del control.
    // Valores típicos de Android; ajústalos con lo que muestre el cuadrito.
    private const int BotonA = 0;
    private const int BotonB = 1;
    private const int BotonX = 2;
    private const int BotonY = 3;
    private const int BotonL1 = 4;
    private const int BotonR1 = 5;
    private const int BotonL2 = 6;
    private const int BotonR2 = 7;

    private const float ZonaMuerta = 0.2f;

    private Gamepad gamepadVirtual;
    private bool mostrarInfo = true;
    private string ultimoBoton = "-";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AlIniciar()
    {
        var go = new GameObject("[ControlRemotoEditor]");
        DontDestroyOnLoad(go);
        go.AddComponent<ControlRemotoEditor>();
    }

    private void OnEnable()
    {
        gamepadVirtual = InputSystem.AddDevice<Gamepad>("ControlShineconRemoto");
    }

    private void OnDisable()
    {
        if (gamepadVirtual != null && gamepadVirtual.added) InputSystem.RemoveDevice(gamepadVirtual);
        gamepadVirtual = null;
    }

    private static bool Boton(int n) => Input.GetKey(KeyCode.JoystickButton0 + n);

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) mostrarInfo = !mostrarInfo;
        if (gamepadVirtual == null) return;

        // Para el cuadrito: qué botón se está apretando
        for (int n = 0; n < 20; n++)
            if (Input.GetKeyDown(KeyCode.JoystickButton0 + n)) ultimoBoton = n.ToString();

        // Stick izquierdo. "Horizontal"/"Vertical" también responden a WASD; si estás usando
        // el teclado del Mac lo ignoramos para no mover doble.
        Vector2 stick = Vector2.zero;
        bool usandoTeclado = Keyboard.current != null && Keyboard.current.anyKey.isPressed;
        if (!usandoTeclado)
        {
            stick = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (stick.magnitude < ZonaMuerta) stick = Vector2.zero;
            stick = Vector2.ClampMagnitude(stick, 1f);
        }

        var estado = new GamepadState
        {
            leftStick = stick,
            leftTrigger = Boton(BotonL2) ? 1f : 0f,
            rightTrigger = Boton(BotonR2) ? 1f : 0f,
        }
        .WithButton(GamepadButton.South, Boton(BotonA))
        .WithButton(GamepadButton.East, Boton(BotonB))
        .WithButton(GamepadButton.West, Boton(BotonX))
        .WithButton(GamepadButton.North, Boton(BotonY))
        .WithButton(GamepadButton.LeftShoulder, Boton(BotonL1))
        .WithButton(GamepadButton.RightShoulder, Boton(BotonR1));

        InputSystem.QueueStateEvent(gamepadVirtual, estado);
    }

    private void OnGUI()
    {
        if (!mostrarInfo) return;

        string[] nombres = Input.GetJoystickNames();
        string lista = "ninguno";
        foreach (string n in nombres)
            if (!string.IsNullOrEmpty(n)) lista = lista == "ninguno" ? n : lista + ", " + n;

        string texto =
            $"Control remoto (H para esconder)\n" +
            $"Detectados: {lista}\n" +
            $"Último botón: {ultimoBoton}\n" +
            $"Stick: {Input.GetAxisRaw("Horizontal"):0.00}, {Input.GetAxisRaw("Vertical"):0.00}";

        GUI.Box(new Rect(10, 10, 320, 80), GUIContent.none);
        GUI.Label(new Rect(18, 14, 310, 76), texto);
    }
}
#endif

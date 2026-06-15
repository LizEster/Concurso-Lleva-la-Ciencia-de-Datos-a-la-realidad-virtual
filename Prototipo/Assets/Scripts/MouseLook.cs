using UnityEngine;
using UnityEngine.InputSystem; // Necesario para el nuevo sistema

public class MouseLook : MonoBehaviour
{
    public float mouseSensitivity = 300f; // Subida por defecto para trackpad
    public Transform playerBody;

    private float xRotation = 0f;

    void Start()
    {
        // Bloquea el cursor en el centro de la pantalla
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        // LEER EL MOUSE USANDO EL NUEVO INPUT SYSTEM
        // Esto soluciona el error de compatibilidad directamente por código
        Vector2 mouseDelta = Mouse.current.delta.ReadValue() * (mouseSensitivity / 10f) * Time.deltaTime;

        float mouseX = mouseDelta.x;
        float mouseY = mouseDelta.y;

        // Calcular la rotación vertical (mirar arriba y abajo) y limitarla a 90 grados
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Aplicar la rotación a la cámara (eje X)
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // Rotar el cuerpo completo del jugador hacia los lados (eje Y)
        if (playerBody != null)
        {
            playerBody.Rotate(Vector3.up * mouseX);
        }
    }
}

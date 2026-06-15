using UnityEngine;

public class PuertaDataCenter : MonoBehaviour
{
    [Header("Configuración de Movimiento")]
    [Tooltip("Cuánto se va a mover la puerta desde su posición original. Cambia el eje Y para subirla.")]
    public Vector3 offsetMovimiento = new Vector3(0f, 6f, 0f); // Subirá 6 metros en el eje Y por defecto
    public float velocidad = 3f; 

    private Vector3 posicionInicial;
    private Vector3 posicionObjetivo;
    private bool abrir = false;

    void Start()
    {
        // Guarda el punto exacto donde pusiste la puerta en tu cuarto oscuro
        posicionInicial = transform.position;
        
        // Calcula el punto final en el aire sumando el offset
        posicionObjetivo = posicionInicial + offsetMovimiento;
    }

    void Update()
    {
        // Mueve la puerta suavemente usando interpolación (Lerp)
        if (abrir)
        {
            transform.position = Vector3.Lerp(transform.position, posicionObjetivo, velocidad * Time.deltaTime);
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, posicionInicial, velocidad * Time.deltaTime);
        }
    }

    // Esta es la función que el ControladorActo1 llamará al terminar los diálogos
    public void AbrirPuerta()
    {
        abrir = true;
        Debug.Log($"> [{gameObject.name}] Comandando apertura suave hacia arriba.");
    }

    public void CerrarPuerta()
    {
        abrir = false;
    }
}
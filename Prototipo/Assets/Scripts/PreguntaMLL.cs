using UnityEngine;

[System.Serializable]
public class OpcionRespuesta
{
    public string textoOpcion;         // El concepto (ej: "REINA")
    public float costoRefrigeracion;  // Cuánto baja la barra (ej: 2 para correctas, 5 para errores)
    public float costoAgua;          // Cuánto consume (ej: 0.15 para correctas, 0.4 para errores)
    [TextArea(2, 3)]
    public string mensajeResultado;   // El texto que va a la terminal
}

[System.Serializable]
public class PreguntaMLL
{
    [TextArea(3, 5)]
    public string enunciadoPregunta;  // La ecuación (ej: "Rey - Hombre + Mujer ≈ [¿?]")
    public OpcionRespuesta[] opciones = new OpcionRespuesta[3]; // Las 3 opciones fijas
}
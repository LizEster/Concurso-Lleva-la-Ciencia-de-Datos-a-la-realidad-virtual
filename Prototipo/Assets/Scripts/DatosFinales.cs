// DatosFinales.cs
// Guarda los valores del juego para usarlos en la escena final.
// No necesita estar en ningún objeto, funciona solo.

public static class DatosFinales
{
    public static float aguaConsumida = 0f;
    public static float refrigeracionRestante = 100f;
    /// <summary>True si el juego terminó porque se acabaron las reservas (el jugador cayó al vacío).</summary>
    public static bool colapsoTermico = false;
    /// <summary>True si el juego terminó porque el jugador se cayó demasiadas veces.</summary>
    public static bool demasiadasCaidas = false;
}

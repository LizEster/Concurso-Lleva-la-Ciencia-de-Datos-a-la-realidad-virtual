/// <summary>
/// Qué escena se carga para mostrar el Mapa de decisiones, según cómo terminó el jugador.
/// Cambia los nombres de abajo por los de tus escenas (y agrégalas en Build Profiles >
/// Scene List). Si una escena no existe en el build, el mapa se muestra en la escena actual
/// (final1), así que nada se rompe mientras no las tengas hechas.
/// </summary>
public static class EscenaFinalSegunResultado
{
    public const string Naturaleza = "Final_Naturaleza";   // verde  (ÓPTIMO): mundo vivo, naturaleza
    public const string Basura = "Final_Basura";           // amarillo / naranja (MODERADO, CRÍTICO): mundo entre basura, gente enferma
    public const string Seca = "Final_Seca";               // rojo (COLAPSO): todo seco, sin agua, sin gente viva

    // CAÍDAS (morado): atrapado dentro de la IA = se queda en esta misma escena (final1).
    public const string AtrapadoEnLaIA = "";

    public static string Nombre(MapaDecisiones.Final final)
    {
        switch (final)
        {
            case MapaDecisiones.Final.Optimo: return Naturaleza;
            case MapaDecisiones.Final.Moderado:
            case MapaDecisiones.Final.Critico: return Basura;
            case MapaDecisiones.Final.Colapso: return Seca;
            default: return AtrapadoEnLaIA;
        }
    }
}

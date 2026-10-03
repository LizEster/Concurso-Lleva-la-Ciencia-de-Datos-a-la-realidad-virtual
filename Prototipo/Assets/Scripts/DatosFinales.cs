// DatosFinales.cs
// Guarda los valores del juego para usarlos en la escena final.
// No necesita estar en ningún objeto, funciona solo.

using System.Collections.Generic;

public static class DatosFinales
{
    public static float aguaConsumida = 0f;
    public static float refrigeracionRestante = 100f;
    /// <summary>True si el juego terminó porque se acabaron las reservas (el jugador cayó al vacío).</summary>
    public static bool colapsoTermico = false;
    /// <summary>True si el juego terminó porque el jugador se cayó demasiadas veces.</summary>
    public static bool demasiadasCaidas = false;

    // ------------------------------------------------------------------
    // REGISTRO DE DECISIONES (para el Mapa de decisiones del final)
    // ------------------------------------------------------------------

    /// <summary>Lo que cuesta "Responder con IA" (lo usa GameManager.UsarBotonIA).</summary>
    public const float CostoIARefri = 35f;
    public const float CostoIAAgua = 2.5f;
    /// <summary>Índice que se usa para "Responder con IA" en 'elegida'.</summary>
    public const int OpcionIA = 3;

    public class Decision
    {
        public string enunciado;
        public string[] opciones;
        public int elegida;      // 0..2 = opción, 3 = Responder con IA
        public int correcta;
        public float[] costoRefri;
        public float[] costoAgua;

        public float CostoRefri(int opcion) => opcion == OpcionIA ? CostoIARefri : costoRefri[opcion];
        public float CostoAgua(int opcion) => opcion == OpcionIA ? CostoIAAgua : costoAgua[opcion];
        public string Texto(int opcion) => opcion == OpcionIA ? "Responder con IA" : opciones[opcion];
    }

    public static readonly List<Decision> decisiones = new List<Decision>();
    public static float refrigeracionInicial = 100f;
    public static int caidas = 0;
    public static float refriPerdidaPorCaidas = 0f;

    /// <summary>Una caída al vacío: cuántas preguntas llevaba respondidas y lo que costó.</summary>
    public class Caida
    {
        public int decisionesPrevias;   // preguntas ya respondidas cuando te caíste
        public float refri;
        public float agua;
    }

    /// <summary>Todas las caídas de la partida, en orden (el Mapa de decisiones las dibuja en morado).</summary>
    public static readonly List<Caida> registroCaidas = new List<Caida>();

    /// <summary>Lo llama BaldosaPregunta cada vez que el jugador se cae al vacío.</summary>
    public static void RegistrarCaida(float refri, float agua)
    {
        registroCaidas.Add(new Caida { decisionesPrevias = decisiones.Count, refri = refri, agua = agua });
    }

    /// <summary>Lo llama GameManager al empezar una partida.</summary>
    public static void Reiniciar(float refrigeracionDeInicio)
    {
        decisiones.Clear();
        refrigeracionInicial = refrigeracionDeInicio;
        caidas = 0;
        refriPerdidaPorCaidas = 0f;
        registroCaidas.Clear();
        colapsoTermico = false;
        demasiadasCaidas = false;
    }

    /// <summary>Lo llama BaldosaPregunta cada vez que se responde una pregunta.</summary>
    public static void RegistrarDecision(string enunciado, string[] opciones, int elegida, int correcta, float[] costoRefri, float[] costoAgua)
    {
        decisiones.Add(new Decision
        {
            enunciado = enunciado,
            opciones = opciones,
            elegida = elegida,
            correcta = correcta,
            costoRefri = costoRefri,
            costoAgua = costoAgua
        });
    }
}

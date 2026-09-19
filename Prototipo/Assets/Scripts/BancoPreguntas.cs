using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Banco compartido de las 15 preguntas de analogía vectorial. Al cargar la escena se baraja
/// una sola vez; cada <see cref="BaldosaPregunta"/> saca la siguiente pregunta disponible del
/// mazo en su Start(), así nunca se repiten entre los pisos de una misma partida y el orden
/// es distinto cada vez que se juega.
/// </summary>
public static class BancoPreguntas
{
    private static List<PreguntaVectorial> colaBarajada;

    // Se ejecuta automáticamente cada vez que se carga la escena (incluso si "Reload Domain"
    // está desactivado en Project Settings), para que una partida nueva nunca arrastre
    // preguntas ya gastadas de una partida anterior en el Editor.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ReiniciarAlCargarEscena()
    {
        colaBarajada = null;
    }

    /// <summary>Saca y elimina la siguiente pregunta del mazo barajado (rebaraja si hiciera falta).</summary>
    public static PreguntaVectorial SacarSiguiente()
    {
        if (colaBarajada == null || colaBarajada.Count == 0)
        {
            colaBarajada = ConstruirBancoCompleto();
            Barajar(colaBarajada);
        }

        PreguntaVectorial pregunta = colaBarajada[0];
        colaBarajada.RemoveAt(0);
        return pregunta;
    }

    private static void Barajar(List<PreguntaVectorial> lista)
    {
        for (int i = lista.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (lista[i], lista[j]) = (lista[j], lista[i]);
        }
    }

    private static List<PreguntaVectorial> ConstruirBancoCompleto()
    {
        return new List<PreguntaVectorial>
        {
            new PreguntaVectorial(
                "Rey - Hombre + Mujer ≈ [¿?]",
                new[] { "Princesa", "Reina", "Castillo" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Procesamiento correcto.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta."
                }),

            new PreguntaVectorial(
                "París - Francia + Italia ≈ [¿?]",
                new[] { "Roma", "Venecia", "Europa" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento correcto.",
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua."
                }),

            new PreguntaVectorial(
                "Médico - Hospital + Colegio ≈ [¿?]",
                new[] { "Alumno", "Pizarra", "Profesor" },
                2,
                new[] { 50f, 45f, 5f },
                new[] { 2.0f, 1.5f, 0.15f },
                new[]
                {
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua.",
                    "> Procesamiento correcto."
                }),

            new PreguntaVectorial(
                "Ojo - vista + oído ≈ [¿?]",
                new[] { "escuchar", "ondas", "oreja" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento correcto.",
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta. "
                }),

            new PreguntaVectorial(
                "Fuego - quemar + hielo ≈ [¿?]",
                new[] { "frío", "congelar", "agua" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia. Los sistemas de enfriamiento entran en alerta",
                    "> Procesamiento  correcto.",
                    "> Error grave de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua."
                }),

            new PreguntaVectorial(
                "Panadero - pan + zapatero ≈ [¿?]",
                new[] { "cuero", "harina", "zapato" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia. Los sistemas de enfriamiento entran el alerta.",
                    "> Error grave de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Procesamiento correcto."
                }),

            new PreguntaVectorial(
                "Barco - agua + cielo ≈ [¿?]",
                new[] { "Avión", "nube", "pájaro" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento correcto.",
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta."
                }),

            new PreguntaVectorial(
                "Tristeza - llorar + alegría ≈ [¿?]",
                new[] { "fiesta", "reír", "sonrisa" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Procesamiento correcto.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nNieve - invierno + arena ≈ [¿?]",
                new[] { "verano", "mar", "playa" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta.",
                    "> Procesamiento correcto."
                }),

            new PreguntaVectorial(
                "Avión - pasajeros + animal ≈ [¿?]",
                new[] { "pájaro", "isla", "aeropuerto" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento correcto.",
                    "> Error de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Error grave de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nAbeja - enjambre + lobos ≈ [¿?]",
                new[] { "bosque", "manada", "luna" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua.",
                    "> Procesamiento correcto.",
                    "> Error grave de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento."
                }),

            new PreguntaVectorial(
                "Gaviota - playa + pingüinos ≈ [¿?]",
                new[] { "hielo", "pez", "Antártica" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua.",
                    "> Error grave de coherencia. El datacenter evapora más agua para bajar el sobrecalentamiento.",
                    "> Procesamiento correcto."
                }),

            new PreguntaVectorial(
                "Vaca - ternero + oveja ≈ [¿?]",
                new[] { "cordero", "lana", "cabra" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento correcto.",
                    "> Error de coherencia. Los sistemas de enfriamiento entran en alerta.",
                    "> Error grave de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua."
                }),

            new PreguntaVectorial(
                "Día - luz + noche ≈ [¿?]",
                new[] { "luna", "oscuridad", "sombra" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia. Los sistemas de enfriamiento entran en alerta.",
                    "> Procesamiento correcto.",
                    "> Error grave de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua."
                }),

            new PreguntaVectorial(
                "Pájaro - nido + oso ≈ [¿?]",
                new[] { "pez", "bosque", "cueva" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia. El sobrecalentamiento del datacenter reduce drásticamente las reservas de agua.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta.",
                    "> Procesamiento correcto."
                }),
        };
    }
}

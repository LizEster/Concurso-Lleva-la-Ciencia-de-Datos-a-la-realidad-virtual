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
            // ---- Las 3 preguntas originales, sin cambios ----
            new PreguntaVectorial(
                "Un panel holográfico se enciende frente al precipicio mostrando una ecuación de vectores incompleta:\n\nRey - Hombre + Mujer ≈ [¿?]",
                new[] { "Princesa", "Reina", "Castillo" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia vectorial. El datacenter disipa calor crítico.",
                    "> Procesamiento eficiente. El puente de luz se forma en verde brillante.",
                    "> Error grave de coherencia. Los sistemas de enfriamiento entran en alerta."
                }),

            new PreguntaVectorial(
                "El panel holográfico se mantiene activo mostrando una nueva relación semántica:\n\nParís - Francia + Italia ≈ [¿?]",
                new[] { "Roma", "Venecia", "Europa" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento eficiente. Las conexiones vectoriales se estabilizan.",
                    "> Error de coherencia vectorial. Evaporación de agua acelerada en las celdas fucsias.",
                    "> Error de coherencia. La sobrecarga térmica reduce drásticamente las reservas."
                }),

            new PreguntaVectorial(
                "Último cruce vectorial antes de la salida del túnel. Resuelve la incógnita:\n\nMédico - Hospital + Colegio ≈ [¿?]",
                new[] { "Alumno", "Pizarra", "Profesor" },
                2,
                new[] { 50f, 45f, 5f },
                new[] { 2.0f, 1.5f, 0.15f },
                new[]
                {
                    "> Error de coherencia. Inestabilidad computacional crítica en el último tramo.",
                    "> Error de coherencia. Los ventiladores no logran mitigar el impacto térmico.",
                    "> OUTPUT GENERADO. La gran compuerta del final se ilumina."
                }),

            // ---- Las 12 preguntas nuevas ----
            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nOjo - vista + oído ≈ [¿?]",
                new[] { "escuchar", "ondas", "oreja" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento eficiente. El vector semántico convierte percepción en acción.",
                    "> Error de coherencia vectorial. Confundiste el medio con el sentido.",
                    "> Error grave de coherencia. Confundiste el órgano con la función."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nFuego - quemar + hielo ≈ [¿?]",
                new[] { "frío", "congelar", "agua" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia vectorial. Es un estado, no una acción.",
                    "> Procesamiento eficiente. La operación inversa se resuelve con precisión.",
                    "> Error grave de coherencia. Confundiste el resultado con el proceso."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nPanadero - pan + zapatero ≈ [¿?]",
                new[] { "cuero", "harina", "zapato" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia vectorial. Es la materia prima, no el producto.",
                    "> Error grave de coherencia. Mezclaste insumos de vectores distintos.",
                    "> Procesamiento eficiente. La relación oficio-producto se mantiene estable."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nBarco - agua + cielo ≈ [¿?]",
                new[] { "Avión", "nube", "pájaro" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento eficiente. El vehículo se adapta correctamente al nuevo medio.",
                    "> Error de coherencia vectorial. Es parte del medio, no un vehículo.",
                    "> Error grave de coherencia. Confundiste un ser vivo con una máquina."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nTristeza - llorar + alegría ≈ [¿?]",
                new[] { "fiesta", "reír", "sonrisa" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia vectorial. Es un contexto, no una expresión.",
                    "> Procesamiento eficiente. La emoción y su expresión física quedan alineadas.",
                    "> Error grave de coherencia. Te acercaste, pero no es la acción equivalente a llorar."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nNieve - invierno + arena ≈ [¿?]",
                new[] { "verano", "mar", "playa" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia vectorial. Repetiste la categoría de estación.",
                    "> Error grave de coherencia. Confundiste el terreno con lo que lo rodea.",
                    "> Procesamiento eficiente. El terreno y su estación se corresponden bien."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nAvión - pasajeros + animal ≈ [¿?]",
                new[] { "pájaro", "isla", "aeropuerto" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento eficiente. El vehículo se convierte correctamente en su equivalente animal.",
                    "> Error de coherencia vectorial. No es un ser vivo.",
                    "> Error grave de coherencia. Confundiste el vehículo con su infraestructura."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nAbeja - enjambre + lobos ≈ [¿?]",
                new[] { "bosque", "manada", "luna" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia vectorial. Es un hábitat, no un colectivo.",
                    "> Procesamiento eficiente. El colectivo se ajusta correctamente a la nueva especie.",
                    "> Error grave de coherencia. El vector se desvió por completo del contexto."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nGaviota - playa + pingüinos ≈ [¿?]",
                new[] { "hielo", "pez", "Antártica" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia vectorial. Es un elemento del lugar, no el lugar.",
                    "> Error grave de coherencia. Confundiste el hábitat con una presa.",
                    "> Procesamiento eficiente. El hábitat se actualiza correctamente según la especie."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nVaca - ternero + oveja ≈ [¿?]",
                new[] { "cordero", "lana", "cabra" },
                0,
                new[] { 5f, 45f, 50f },
                new[] { 0.15f, 1.5f, 2.0f },
                new[]
                {
                    "> Procesamiento eficiente. La cría corresponde correctamente a la nueva especie.",
                    "> Error de coherencia vectorial. Es un producto, no una cría.",
                    "> Error grave de coherencia. Cambiaste de especie en vez de mantenerla."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nDía - luz + noche ≈ [¿?]",
                new[] { "luna", "oscuridad", "sombra" },
                1,
                new[] { 45f, 5f, 50f },
                new[] { 1.5f, 0.15f, 2.0f },
                new[]
                {
                    "> Error de coherencia vectorial. Es un objeto asociado, no el concepto opuesto.",
                    "> Procesamiento eficiente. El opuesto conceptual se resuelve con precisión.",
                    "> Error grave de coherencia. Te acercaste, pero no es la ausencia total de luz."
                }),

            new PreguntaVectorial(
                "El panel holográfico proyecta una nueva ecuación de vectores incompleta:\n\nPájaro - nido + oso ≈ [¿?]",
                new[] { "pez", "bosque", "cueva" },
                2,
                new[] { 45f, 50f, 5f },
                new[] { 1.5f, 2.0f, 0.15f },
                new[]
                {
                    "> Error de coherencia vectorial. El vector se desvió por completo del contexto.",
                    "> Error grave de coherencia. Es un hábitat general, no un refugio específico.",
                    "> Procesamiento eficiente. El refugio se actualiza correctamente según la especie."
                }),
        };
    }
}

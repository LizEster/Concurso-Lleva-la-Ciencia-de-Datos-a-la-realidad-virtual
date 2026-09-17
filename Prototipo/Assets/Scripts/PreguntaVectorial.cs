/// <summary>
/// Datos de una sola pregunta de analogía vectorial (tipo "Rey - Hombre + Mujer ≈ Reina").
/// Es una clase de datos pura, sin lógica de juego: la arma <see cref="BancoPreguntas"/> y la
/// consume <see cref="BaldosaPregunta"/>.
/// </summary>
public class PreguntaVectorial
{
    public readonly string enunciado;
    public readonly string[] textoOpciones;
    public readonly int indiceCorrecta;
    public readonly float[] costoRefri;
    public readonly float[] costoAgua;
    public readonly string[] mensajeResultado;

    public PreguntaVectorial(
        string enunciado,
        string[] textoOpciones,
        int indiceCorrecta,
        float[] costoRefri,
        float[] costoAgua,
        string[] mensajeResultado)
    {
        this.enunciado = enunciado;
        this.textoOpciones = textoOpciones;
        this.indiceCorrecta = indiceCorrecta;
        this.costoRefri = costoRefri;
        this.costoAgua = costoAgua;
        this.mensajeResultado = mensajeResultado;
    }
}

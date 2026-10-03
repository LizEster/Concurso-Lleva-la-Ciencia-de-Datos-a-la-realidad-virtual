using System.Collections;
using UnityEngine;

/// <summary>
/// Ponlo en un objeto vacío de cada escena de final (Naturaleza, Basura, Seca...). Al entrar,
/// la pantalla viene en negro desde el informe, se aclara de a poco y aparece el Mapa de
/// decisiones interactivo (con VOLVER A JUGAR y SALIR) dentro de esa escena.
/// La escena necesita lo mismo que final1 para el visor: Main Camera, Player y EventSystem
/// (lo más fácil es duplicar final1 y cambiarle el ambiente).
/// </summary>
public class MapaAlEntrar : MonoBehaviour
{
    [Tooltip("Segundos que se ve la escena sola antes de que aparezca el mapa")]
    public float segundosAntesDelMapa = 2f;

    [Tooltip("Cuánto tarda en aclararse la pantalla")]
    public float duracionFundido = 1.5f;

    IEnumerator Start()
    {
        // Tapa todo de inmediato (el informe dejó la pantalla en negro) y deja que el rig VR se arme.
        yield return VeloNegro.Fundir(1f, 1f, 0.1f);
        yield return null;
        yield return null;

        yield return VeloNegro.Fundir(1f, 0f, duracionFundido);
        yield return new WaitForSeconds(segundosAntesDelMapa);

        MapaDecisiones.Mostrar();
    }
}

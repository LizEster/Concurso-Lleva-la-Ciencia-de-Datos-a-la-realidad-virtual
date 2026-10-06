using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fundido a negro que funciona igual en el visor y en PC: un panel negro pegado a los ojos
/// que se dibuja encima de todo (menos del puntero de mirada y del menú).
/// Uso desde una corrutina:
///     yield return VeloNegro.Fundir(0f, 1f, 1f);   // a negro en 1 s
///     yield return VeloNegro.Fundir(1f, 0f, 1.5f); // vuelve a verse el juego
/// Se crea solo la primera vez que se usa.
/// </summary>
public class VeloNegro : MonoBehaviour
{
    private static VeloNegro instancia;

    private RectTransform raiz;
    private Image imagen;
    private Color colorVelo = Color.black;
    private float alfaActual;

    /// <summary>Opacidad actual del velo (0 = no se ve).</summary>
    public static float AlfaActual => instancia != null ? instancia.alfaActual : 0f;

    public static IEnumerator Fundir(float desde, float hasta, float duracion)
    {
        return FundirColor(Color.black, desde, hasta, duracion);
    }

    /// <summary>Igual que Fundir, pero de cualquier color (blanco = encandilamiento, rojo = alarma...).</summary>
    public static IEnumerator FundirColor(Color color, float desde, float hasta, float duracion)
    {
        VeloNegro velo = Obtener();
        velo.colorVelo = color;
        float tiempo = 0f;
        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            velo.PonerOpacidad(Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, tiempo / duracion)));
            yield return null;
        }
        velo.PonerOpacidad(hasta);
    }

    private static VeloNegro Obtener()
    {
        if (instancia == null)
        {
            GameObject go = new GameObject("[VeloNegro]");
            instancia = go.AddComponent<VeloNegro>();

            instancia.raiz = EstiloUI.CrearCanvas("Velo", 1400, new Vector2(100f, 100f), 0.1f); // 10 m a 50 cm: tapa toda la vista
            instancia.raiz.SetParent(go.transform, false);
            instancia.imagen = EstiloUI.CrearImagen(instancia.raiz, "Negro", Vector2.zero, Vector2.zero, new Color(0f, 0f, 0f, 0f));
            EstiloUI.Estirar(instancia.imagen.rectTransform);
            instancia.raiz.gameObject.SetActive(false);
        }
        return instancia;
    }

    /// <summary>Pone el velo de un color y opacidad al tiro (para efectos que laten cuadro a cuadro).</summary>
    public static void Poner(Color color, float alfa)
    {
        VeloNegro velo = Obtener();
        velo.colorVelo = color;
        velo.PonerOpacidad(alfa);
    }

    private void PonerOpacidad(float alfa)
    {
        alfaActual = alfa;
        imagen.color = new Color(colorVelo.r, colorVelo.g, colorVelo.b, alfa);
        raiz.gameObject.SetActive(alfa > 0.001f);
        Seguir();
    }

    void OnEnable() { Application.onBeforeRender += Seguir; }
    void OnDisable() { Application.onBeforeRender -= Seguir; }
    void LateUpdate() { Seguir(); }

    private void Seguir()
    {
        Transform cabeza = PunteroMirada.Cabeza();
        if (cabeza == null || raiz == null) return;
        raiz.SetPositionAndRotation(cabeza.position + cabeza.forward * 0.5f, cabeza.rotation);
    }
}

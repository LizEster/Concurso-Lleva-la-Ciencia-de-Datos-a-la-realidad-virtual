using UnityEngine;
using System.Collections;

public class NivelAgua : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("El objeto 'AnclaAgua' en la base de la tubería (el agua es su hijo)")]
    public Transform anclaAgua;

    [Header("Configuración")]
    public float nivelMaximo = 100f;
    public float duracionTransicion = 1f;

    [Header("Cambio de color (opcional)")]
    public bool cambiarColor = false;
    public Renderer rendererAgua;
    public Color colorLleno = new Color(0.2f, 0.6f, 1f);
    public Color colorVacio = new Color(0.8f, 0.1f, 0.1f);

    private Coroutine corrutinaActual;

    public void ActualizarNivel(float nivelActual)
    {
        float porcentajeObjetivo = Mathf.Clamp01(nivelActual / nivelMaximo);

        if (corrutinaActual != null)
        {
            StopCoroutine(corrutinaActual);
        }
        corrutinaActual = StartCoroutine(TransicionNivel(porcentajeObjetivo));
    }

    private IEnumerator TransicionNivel(float porcentajeObjetivo)
    {
        float porcentajeInicial = anclaAgua.localScale.y;
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionTransicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionTransicion;
            float porcentajeActual = Mathf.Lerp(porcentajeInicial, porcentajeObjetivo, t);

            Vector3 escala = anclaAgua.localScale;
            escala.y = porcentajeActual;
            anclaAgua.localScale = escala;

            if (cambiarColor && rendererAgua != null)
            {
                rendererAgua.material.color = Color.Lerp(colorVacio, colorLleno, porcentajeActual);
            }

            yield return null;
        }

        Vector3 escalaFinal = anclaAgua.localScale;
        escalaFinal.y = porcentajeObjetivo;
        anclaAgua.localScale = escalaFinal;
    }
}
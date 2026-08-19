using UnityEngine;
using System.Collections;

public class NivelAgua : MonoBehaviour
{
    [Header("Referencias")]
    public Transform agua;

    [Header("Configuración")]
    public float nivelMaximo = 100f;
    public float duracionTransicion = 1f;

    [Header("Cambio de color (opcional)")]
    public bool cambiarColor = false;
    public Renderer rendererAgua;
    public Color colorLleno = new Color(0.2f, 0.6f, 1f);
    public Color colorVacio = new Color(0.8f, 0.1f, 0.1f);

    private Vector3 escalaOriginal;
    private Vector3 posicionOriginal;
    private int ejeAplastamiento; // 0 = X, 1 = Y, 2 = Z (eje local que apunta hacia "arriba" en el mundo)
    private float signo;          // +1 o -1, según hacia dónde apunte ese eje

    private Coroutine corrutinaActual;

    void Awake()
    {
        escalaOriginal = agua.localScale;
        posicionOriginal = agua.localPosition;

        // Detecta automáticamente qué eje LOCAL de "agua" coincide con el "arriba" del MUNDO real,
        // sin importar cómo esté rotado el tubo (horizontal, vertical, diagonal, lo que sea)
        float dotX = Vector3.Dot(agua.right, Vector3.up);
        float dotY = Vector3.Dot(agua.up, Vector3.up);
        float dotZ = Vector3.Dot(agua.forward, Vector3.up);

        float absX = Mathf.Abs(dotX);
        float absY = Mathf.Abs(dotY);
        float absZ = Mathf.Abs(dotZ);

        if (absX >= absY && absX >= absZ) { ejeAplastamiento = 0; signo = Mathf.Sign(dotX); }
        else if (absY >= absX && absY >= absZ) { ejeAplastamiento = 1; signo = Mathf.Sign(dotY); }
        else { ejeAplastamiento = 2; signo = Mathf.Sign(dotZ); }
    }

    public void ActualizarNivel(float nivelActual)
    {
        float porcentajeObjetivo = Mathf.Clamp01(nivelActual / nivelMaximo);

        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        corrutinaActual = StartCoroutine(TransicionNivel(porcentajeObjetivo));
    }

    private IEnumerator TransicionNivel(float porcentajeObjetivo)
    {
        float porcentajeInicial = GetComponente(agua.localScale) / GetComponente(escalaOriginal);
        float tiempoTranscurrido = 0f;

        while (tiempoTranscurrido < duracionTransicion)
        {
            tiempoTranscurrido += Time.deltaTime;
            float t = tiempoTranscurrido / duracionTransicion;
            float pct = Mathf.Lerp(porcentajeInicial, porcentajeObjetivo, t);

            AplicarNivel(pct);

            if (cambiarColor && rendererAgua != null)
                rendererAgua.material.color = Color.Lerp(colorVacio, colorLleno, pct);

            yield return null;
        }

        AplicarNivel(porcentajeObjetivo);
    }

    private void AplicarNivel(float pct)
    {
        // Escala: solo se aplasta en el eje que apunta hacia arriba; el resto queda igual (no se acorta a lo largo)
        Vector3 nuevaEscala = escalaOriginal;
        nuevaEscala = SetComponente(nuevaEscala, GetComponente(escalaOriginal) * pct);
        agua.localScale = nuevaEscala;

        // Posición: se ajusta para que la parte de ABAJO quede fija mientras se aplasta desde ARRIBA
        float alturaOriginalLocal = 2f * GetComponente(escalaOriginal); // 2f = altura del Cylinder default de Unity
        float diferenciaAltura = alturaOriginalLocal * (1f - pct);

        Vector3 nuevaPosicion = posicionOriginal;
        nuevaPosicion = SetComponente(nuevaPosicion, GetComponente(posicionOriginal) - signo * diferenciaAltura / 2f);
        agua.localPosition = nuevaPosicion;
    }

    private float GetComponente(Vector3 v)
    {
        if (ejeAplastamiento == 0) return v.x;
        if (ejeAplastamiento == 1) return v.y;
        return v.z;
    }

    private Vector3 SetComponente(Vector3 v, float valor)
    {
        if (ejeAplastamiento == 0) v.x = valor;
        else if (ejeAplastamiento == 1) v.y = valor;
        else v.z = valor;
        return v;
    }
}
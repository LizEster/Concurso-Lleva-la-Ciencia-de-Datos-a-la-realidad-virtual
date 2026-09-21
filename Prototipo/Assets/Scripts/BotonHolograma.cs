using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Una fila de opción dentro del PanelHolografico (ej: "[1] Reina"). OJO: no es un
/// Button real, no recibe clicks (elegimos mantener el input por teclado). Solo
/// cambia de color y hace un pequeño "pulso" de escala cuando PanelHolografico.Resaltar()
/// la elige, para dar feedback visual al presionar 1/2/3/4.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class BotonHolograma : MonoBehaviour
{
    public Image fondo;
    public TextMeshProUGUI texto;

    [Tooltip("Cuánto crece el botón al ser elegido, como fracción de su escala normal.")]
    public float escalaPulso = 1.08f;
    public float duracionPulso = 0.15f;

    private Vector3 escalaOriginal;
    private Coroutine corrutinaPulso;

    void Awake()
    {
        escalaOriginal = transform.localScale;
    }

    public void SetTexto(string valor)
    {
        if (texto != null) texto.text = valor;
    }

    public void SetColor(Color color)
    {
        if (fondo != null) fondo.color = color;
    }

    public void Pulso()
    {
        if (corrutinaPulso != null) StopCoroutine(corrutinaPulso);
        corrutinaPulso = StartCoroutine(PulsoCorrutina());
    }

    private IEnumerator PulsoCorrutina()
    {
        float t = 0f;
        Vector3 escalaGrande = escalaOriginal * escalaPulso;

        while (t < duracionPulso)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaOriginal, escalaGrande, t / duracionPulso);
            yield return null;
        }

        t = 0f;
        while (t < duracionPulso)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaGrande, escalaOriginal, t / duracionPulso);
            yield return null;
        }

        transform.localScale = escalaOriginal;
    }
}

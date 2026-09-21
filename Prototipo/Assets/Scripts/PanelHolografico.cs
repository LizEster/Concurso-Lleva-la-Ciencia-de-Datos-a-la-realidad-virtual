using System;
using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla el panel holográfico de preguntas que cuelga sobre cada BaldosaPregunta.
/// Este panel NO recibe clicks de mouse: BaldosaPregunta sigue leyendo las teclas
/// 1/2/3/4 como hasta ahora, y simplemente le avisa a este panel qué mostrar y qué
/// botón resaltar antes de resolver la respuesta de verdad.
/// </summary>
public class PanelHolografico : MonoBehaviour
{
    [Header("Textos")]
    public TextMeshProUGUI textoEnunciado;

    [Tooltip("Arrastra aquí los 4 BotonHolograma en orden: [0]=opción 1, [1]=opción 2, [2]=opción 3, [3]=Responder con IA.")]
    public BotonHolograma[] botones = new BotonHolograma[4];

    [Header("Animación (opcional)")]
    [Tooltip("Si tienes un Animator con estados 'Aparecer'/'Desaparecer', arrástralo aquí. Si lo dejas vacío, el panel igual aparece/desaparece con un fade simple hecho por código.")]
    public Animator animator;
    public string triggerAparecer = "Aparecer";
    public string triggerDesaparecer = "Desaparecer";
    public float duracionFadeSimple = 0.25f;

    [Header("Resaltado al elegir")]
    public Color colorNormal = new Color(0.3f, 0.85f, 1f, 0.85f);
    public Color colorElegido = new Color(1f, 1f, 1f, 1f);
    [Tooltip("Cuánto se queda iluminado el botón elegido antes de resolver la respuesta de verdad.")]
    public float duracionResaltado = 0.35f;

    private CanvasGroup canvasGroup;
    private Coroutine corrutinaActual;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        gameObject.SetActive(false);
    }

    /// <summary>Llamado por BaldosaPregunta cuando el jugador llega y hay que mostrar la pregunta.</summary>
    public void Mostrar(string enunciado, string[] opciones)
    {
        gameObject.SetActive(true);
        if (canvasGroup != null) canvasGroup.alpha = 1f;

        if (textoEnunciado != null) textoEnunciado.text = enunciado;

        for (int i = 0; i < botones.Length; i++)
        {
            if (botones[i] == null) continue;

            bool esBotonIA = i == 3;
            bool tieneOpcion = i < opciones.Length;

            if (esBotonIA)
            {
                botones[i].gameObject.SetActive(true);
                botones[i].SetTexto("[4] Responder con IA");
            }
            else if (tieneOpcion)
            {
                botones[i].gameObject.SetActive(true);
                botones[i].SetTexto($"[{i + 1}] {opciones[i]}");
            }
            else
            {
                botones[i].gameObject.SetActive(false);
            }

            botones[i].SetColor(colorNormal);
        }

        if (animator != null)
        {
            animator.ResetTrigger(triggerDesaparecer);
            animator.SetTrigger(triggerAparecer);
        }
        else
        {
            ReiniciarCorrutina(FadeCorrutina(0f, 1f));
        }
    }

    /// <summary>Llamado por BaldosaPregunta apenas se resuelve la respuesta.</summary>
    public void Ocultar()
    {
        if (animator != null)
        {
            animator.ResetTrigger(triggerAparecer);
            animator.SetTrigger(triggerDesaparecer);
            // OcultarDeVerdad() se llama solo desde un Animation Event al final del clip
            // "Desaparecer" (ver instrucciones). Así el panel se desactiva justo cuando
            // termina la animación, no antes.
        }
        else
        {
            ReiniciarCorrutina(FadeCorrutina(1f, 0f, () => gameObject.SetActive(false)));
        }
    }

    /// <summary>No la llames a mano: la dispara un Animation Event al final de "Desaparecer".</summary>
    public void OcultarDeVerdad()
    {
        gameObject.SetActive(false);
    }

    /// <summary>
    /// BaldosaPregunta llama esto apenas detecta la tecla 1/2/3/4: ilumina el botón
    /// elegido y, cuando termina esa animación de resaltado, ejecuta 'alTerminar'
    /// (ahí es cuando BaldosaPregunta recién llama a EvaluarRespuesta o UsarIA).
    /// </summary>
    public void Resaltar(int indice, Action alTerminar)
    {
        ReiniciarCorrutina(ResaltarCorrutina(indice, alTerminar));
    }

    private void ReiniciarCorrutina(IEnumerator nueva)
    {
        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        corrutinaActual = StartCoroutine(nueva);
    }

    private IEnumerator ResaltarCorrutina(int indice, Action alTerminar)
    {
        if (indice >= 0 && indice < botones.Length && botones[indice] != null)
        {
            botones[indice].SetColor(colorElegido);
            botones[indice].Pulso();
        }

        yield return new WaitForSeconds(duracionResaltado);
        alTerminar?.Invoke();
    }

    private IEnumerator FadeCorrutina(float desde, float hasta, Action alTerminar = null)
    {
        float t = 0f;
        if (canvasGroup != null) canvasGroup.alpha = desde;

        while (t < duracionFadeSimple)
        {
            t += Time.deltaTime;
            if (canvasGroup != null) canvasGroup.alpha = Mathf.Lerp(desde, hasta, t / duracionFadeSimple);
            yield return null;
        }

        if (canvasGroup != null) canvasGroup.alpha = hasta;
        alTerminar?.Invoke();
    }
}

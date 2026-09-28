// TransicionLuz.cs
// Crea un efecto de luz blanca que encandila la pantalla y después carga la escena final.
// Se coloca en un objeto vacío en la escena principal del datacenter.
// Necesita un Canvas con un Panel/Image blanco que cubra toda la pantalla.

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class TransicionLuz : MonoBehaviour
{
    [Header("Configuración")]
    [Tooltip("Arrastra aquí el Image/Panel blanco que cubre toda la pantalla")]
    public Image panelBlanco;

    [Tooltip("Nombre exacto de tu escena final como aparece en Build Settings")]
    public string nombreEscenaFinal = "final1";

    [Tooltip("Cuántos segundos dura el encandilamiento")]
    public float duracionFade = 2f;

    void Start()
    {
        if (panelBlanco != null)
        {
            panelBlanco.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    public void IniciarTransicion(float agua, float refrigeracion)
    {
        DatosFinales.aguaConsumida = agua;
        DatosFinales.refrigeracionRestante = refrigeracion;
        StartCoroutine(Encandilar());
    }

    private IEnumerator Encandilar()
    {
        float tiempo = 0f;

        while (tiempo < duracionFade)
        {
            tiempo += Time.deltaTime;
            float alpha = Mathf.Clamp01(tiempo / duracionFade);
            panelBlanco.color = new Color(1f, 1f, 1f, alpha);
            yield return null;
        }

        SceneManager.LoadScene(nombreEscenaFinal);
    }
}

using UnityEngine;

/// <summary>
/// Mantiene un Canvas (que antes era de pantalla) flotando delante de los ojos del jugador VR.
/// Lo agrega RigVR solo; no hace falta ponerlo a mano.
/// </summary>
public class SeguirCabezaVR : MonoBehaviour
{
    [Tooltip("Metros delante de los ojos.")]
    public float distancia = 1f;
    [Tooltip("Ancho del panel en metros (el alto sale de la proporción del Canvas).")]
    public float anchoMetros = 1.2f;
    [Tooltip("0 = pegado a la cabeza. Mayor que 0 = sigue la mirada con retraso, más cómodo para leer.")]
    public float suavizado = 4f;

    private Transform cabeza;
    private bool colocarDeGolpe = true;

    public void AplicarEscala()
    {
        RectTransform rt = (RectTransform)transform;
        if (rt.sizeDelta.x > 0f) transform.localScale = Vector3.one * (anchoMetros / rt.sizeDelta.x);
    }

    void OnEnable()
    {
        colocarDeGolpe = true;
        Application.onBeforeRender += Seguir;
    }

    void OnDisable()
    {
        Application.onBeforeRender -= Seguir;
    }

    void Update()
    {
        if (cabeza == RigVR.Cabeza) return;

        // Cambió la escena (el fundido de ScreenFader sobrevive entre escenas): nos volvemos
        // a suscribir para quedar DESPUÉS del TrackedPoseDriver de la nueva cámara y no ir
        // un frame atrasados respecto de la cabeza.
        cabeza = RigVR.Cabeza;
        Application.onBeforeRender -= Seguir;
        Application.onBeforeRender += Seguir;
        colocarDeGolpe = true;
    }

    private void Seguir()
    {
        if (cabeza == null) return;

        Vector3 posicion = cabeza.position + cabeza.forward * distancia;
        Quaternion rotacion = Quaternion.LookRotation(cabeza.forward, Vector3.up);

        if (suavizado <= 0f || colocarDeGolpe)
        {
            transform.SetPositionAndRotation(posicion, rotacion);
            colocarDeGolpe = false;
            return;
        }

        float t = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        transform.SetPositionAndRotation(
            Vector3.Lerp(transform.position, posicion, t),
            Quaternion.Slerp(transform.rotation, rotacion, t));
    }
}

using UnityEngine;

/// <summary>
/// Rota el panel sobre el eje Y para que siempre "mire" hacia la cámara del jugador,
/// sin inclinarse hacia arriba/abajo (así el texto nunca queda de cabeza ni torcido
/// aunque el jugador se acerque desde cualquier ángulo). Colócalo en el mismo
/// GameObject donde está el componente Canvas del panel.
/// </summary>
public class BillboardHaciaCamara : MonoBehaviour
{
    private Transform camara;

    void Start()
    {
        if (Camera.main != null) camara = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (camara == null)
        {
            if (Camera.main == null) return;
            camara = Camera.main.transform;
        }

        Vector3 direccion = transform.position - camara.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.0001f) return;

        transform.rotation = Quaternion.LookRotation(direccion);
    }
}

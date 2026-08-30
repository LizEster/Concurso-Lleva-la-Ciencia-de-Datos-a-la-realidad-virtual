using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Se crea solo (no hay que arrastrarlo a ninguna escena) y sobrevive entre escenas.
// Sirve para tapar la pantalla de blanco (como una luz fuerte en los ojos) antes de
// cargar otra escena, y luego desvanecerse de nuevo al llegar.
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    [Header("Apariencia")]
    public Color colorFundido = Color.white;

    private Image imagenFundido;
    private Coroutine corrutinaActual;
    private bool debeAparecerDesdeBlanco = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearInstancia()
    {
        if (Instance != null) return;

        GameObject go = new GameObject("ScreenFader");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<ScreenFader>();
        Instance.ConstruirUI();
    }

    private void ConstruirUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // siempre por encima de todo lo demás

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        gameObject.AddComponent<GraphicRaycaster>();

        GameObject imgGO = new GameObject("FundidoBlanco");
        imgGO.transform.SetParent(transform, false);
        imagenFundido = imgGO.AddComponent<Image>();
        imagenFundido.color = new Color(colorFundido.r, colorFundido.g, colorFundido.b, 0f);
        imagenFundido.raycastTarget = false;

        RectTransform rt = imagenFundido.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        if (!debeAparecerDesdeBlanco) return;
        debeAparecerDesdeBlanco = false;

        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        corrutinaActual = StartCoroutine(Fundido(1f, 0f, 1f));
    }

    // Llamar a esto para hacer el flash blanco y luego cargar otra escena por nombre.
    public void TransicionarAEscena(string nombreEscena, float duracionFundido = 1f, float esperaEnBlanco = 0.3f)
    {
        if (corrutinaActual != null) StopCoroutine(corrutinaActual);
        corrutinaActual = StartCoroutine(FundirYCargar(nombreEscena, duracionFundido, esperaEnBlanco));
    }

    private IEnumerator FundirYCargar(string nombreEscena, float duracion, float espera)
    {
        imagenFundido.raycastTarget = true; // bloquea clics mientras está la transición
        yield return Fundido(0f, 1f, duracion);
        yield return new WaitForSeconds(espera);
        debeAparecerDesdeBlanco = true;
        SceneManager.LoadScene(nombreEscena);
    }

    private IEnumerator Fundido(float desde, float hasta, float duracion)
    {
        float t = 0f;
        Color c = imagenFundido.color;
        while (t < duracion)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(desde, hasta, Mathf.Clamp01(t / duracion));
            imagenFundido.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
        imagenFundido.color = new Color(c.r, c.g, c.b, hasta);
        if (Mathf.Approximately(hasta, 0f)) imagenFundido.raycastTarget = false;
    }
}

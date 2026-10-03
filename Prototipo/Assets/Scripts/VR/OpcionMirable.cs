using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Una opción que se elige APUNTÁNDOLA CON LA MIRADA y apretando A, B, X o Y (ver
/// PunteroMirada). Va sobre un RectTransform de UI (una fila de opciones del diálogo, un botón
/// del panel holográfico, del menú...). Mientras el puntero está encima se ilumina.
/// Funciona igual en el visor (la mirada es la cabeza) y en PC (la mirada es el mouse).
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class OpcionMirable : MonoBehaviour
{
    /// <summary>Todas las opciones activas en este momento; PunteroMirada las recorre.</summary>
    public static readonly List<OpcionMirable> Activas = new List<OpcionMirable>();

    [Tooltip("Cuánto crece la opción mientras la miras.")]
    public float escalaMirada = 1.06f;

    /// <summary>Qué hacer cuando se elige. Lo asigna quien crea la opción.</summary>
    public Action alElegir;

    /// <summary>Se llama con true al empezar a mirarla y con false al dejar de mirarla.</summary>
    public Action<bool> alMirar;

    /// <summary>Si es true, sólo reacciona a la mirada (se ilumina, avisa con 'alMirar') y los botones no la "gastan".</summary>
    public bool soloMirar;

    /// <summary>False mientras no se pueda elegir (ya se eligió, el panel se está ocultando...).</summary>
    public bool Habilitada { get; private set; } = true;

    private Graphic fondo;
    private Color colorFondoNormal;
    private Color colorFondoMirada;
    private TMP_Text texto;
    private Color colorTextoNormal;
    private Color colorTextoMirada;
    private Vector3 escalaNormal = Vector3.one;
    private bool configurada;

    private RectTransform rt;
    private Canvas canvas;
    private CanvasGroup[] grupos = new CanvasGroup[0];

    /// <summary>
    /// Deja la opción lista para elegirse. 'fondo' y 'texto' pueden ser null: se ilumina
    /// lo que haya. Llamarlo cada vez que se vuelve a mostrar la opción.
    /// </summary>
    public void Configurar(Graphic fondo, Color colorFondoNormal, Color colorFondoMirada,
                           TMP_Text texto, Color colorTextoMirada, Action alElegir)
    {
        if (configurada) AplicarAspecto(false); // vuelve al aspecto anterior antes de recapturarlo

        this.fondo = fondo;
        this.colorFondoNormal = colorFondoNormal;
        this.colorFondoMirada = colorFondoMirada;
        this.texto = texto;
        this.colorTextoNormal = texto != null ? texto.color : Color.white;
        this.colorTextoMirada = colorTextoMirada;
        this.alElegir = alElegir;
        escalaNormal = transform.localScale;
        configurada = true;

        Habilitada = true;
        AplicarAspecto(false);
    }

    void Awake()
    {
        rt = (RectTransform)transform;
    }

    void OnEnable()
    {
        if (!Activas.Contains(this)) Activas.Add(this);
        canvas = GetComponentInParent<Canvas>();
        grupos = GetComponentsInParent<CanvasGroup>();
    }

    void OnDisable()
    {
        Activas.Remove(this);
    }

    /// <summary>True si se ve y se puede elegir ahora mismo.</summary>
    public bool Visible
    {
        get
        {
            if (!Habilitada || !isActiveAndEnabled || canvas == null || !canvas.enabled) return false;
            foreach (CanvasGroup grupo in grupos)
            {
                if (grupo != null && grupo.alpha < 0.5f) return false;
            }
            return true;
        }
    }

    /// <summary>¿El rayo de la mirada pasa por esta opción? 'distancia' en metros desde los ojos.</summary>
    public bool Intersecta(Ray rayo, out float distancia)
    {
        distancia = 0f;
        if (canvas == null) return false;

        Canvas raiz = canvas.rootCanvas;
        if (raiz.renderMode == RenderMode.WorldSpace)
        {
            Plane plano = new Plane(rt.forward, rt.position);
            if (!plano.Raycast(rayo, out distancia)) return false;

            Vector2 local = rt.InverseTransformPoint(rayo.GetPoint(distancia));
            return rt.rect.Contains(local);
        }

        // Canvas de pantalla (PC): la mirada es el centro de la pantalla.
        Camera camaraUI = raiz.renderMode == RenderMode.ScreenSpaceCamera ? raiz.worldCamera : null;
        Vector2 centro = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        distancia = 1f;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, centro, camaraUI);
    }

    /// <summary>Lo llama PunteroMirada al entrar/salir la mirada.</summary>
    public void SetMirada(bool mirando)
    {
        if (!Habilitada) return;
        AplicarAspecto(mirando);
        alMirar?.Invoke(mirando);
    }

    /// <summary>Lo llama PunteroMirada cuando se aprieta un botón mientras se mira la opción.</summary>
    public void Elegir()
    {
        if (!Habilitada || soloMirar) return;
        Habilitada = false; // queda iluminada como "elegida" y ya no se puede volver a elegir
        alElegir?.Invoke();
    }

    /// <summary>Deja de poder elegirse y vuelve a su aspecto normal.</summary>
    public void Deshabilitar()
    {
        if (Habilitada) AplicarAspecto(false);
        Habilitada = false;
    }

    private void AplicarAspecto(bool mirando)
    {
        if (!configurada) return;
        if (fondo != null) fondo.color = mirando ? colorFondoMirada : colorFondoNormal;
        if (texto != null) texto.color = mirando ? colorTextoMirada : colorTextoNormal;
        transform.localScale = mirando ? escalaNormal * escalaMirada : escalaNormal;
    }
}

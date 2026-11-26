using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Aviso de interacción estilo videojuego en la parte inferior de la pantalla:
/// una tecla dibujada ([E]) y el texto de la acción. Se construye solo en tiempo de ejecución y
/// no bloquea clics. Uso: InteraccionUI.Mostrar("Entrar al laboratorio"); InteraccionUI.Ocultar();
/// </summary>
public class InteraccionUI : MonoBehaviour
{
    private static InteraccionUI instancia;

    private CanvasGroup grupo;
    private RectTransform panel;
    private RectTransform tecla;
    private TextMeshProUGUI txtTecla;
    private TextMeshProUGUI txtAccion;
    private float alphaObjetivo;
    private Coroutine rutinaPulso;

    private static readonly Color ColorAcento = new Color32(0x2E, 0xCC, 0x71, 0xFF);

    private static InteraccionUI Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = FindAnyObjectByType<InteraccionUI>();
                if (instancia == null) instancia = new GameObject("[InteraccionUI]").AddComponent<InteraccionUI>();
            }
            return instancia;
        }
    }

    public static void Mostrar(string accion, string teclaTexto = "E")
    {
        var ui = Instancia;
        ui.txtAccion.text = accion;
        ui.txtTecla.text = teclaTexto;
        ui.alphaObjetivo = 1f;
    }

    public static void Ocultar()
    {
        if (instancia != null) instancia.alphaObjetivo = 0f;
    }

    /// <summary>Pequeño "golpe" visual al pulsar la tecla.</summary>
    public static void Pulsar()
    {
        if (instancia == null) return;
        if (instancia.rutinaPulso != null) instancia.StopCoroutine(instancia.rutinaPulso);
        instancia.rutinaPulso = instancia.StartCoroutine(instancia.AnimarPulso());
    }

    private void Awake()
    {
        if (instancia != null && instancia != this) { Destroy(gameObject); return; }
        instancia = this;
        Construir();
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void Update()
    {
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, alphaObjetivo, Time.unscaledDeltaTime * 6f);
        float y = Mathf.Lerp(70f, 90f, Mathf.SmoothStep(0f, 1f, grupo.alpha));
        panel.anchoredPosition = new Vector2(0f, y);
    }

    private IEnumerator AnimarPulso()
    {
        for (float t = 0f; t < 0.18f; t += Time.unscaledDeltaTime)
        {
            float s = 1f - 0.15f * Mathf.Sin(t / 0.18f * Mathf.PI);
            tecla.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        tecla.localScale = Vector3.one;
        rutinaPulso = null;
    }

    private void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 890;
        var escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;

        var goPanel = new GameObject("Aviso", typeof(RectTransform), typeof(CanvasGroup), typeof(Image),
            typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
        goPanel.transform.SetParent(transform, false);
        panel = (RectTransform)goPanel.transform;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 0f);
        panel.pivot = new Vector2(0.5f, 0f);
        var fondo = goPanel.GetComponent<Image>();
        fondo.color = new Color(0.06f, 0.07f, 0.09f, 0.88f);
        fondo.raycastTarget = false;
        var layout = goPanel.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(18, 30, 14, 14);
        layout.spacing = 20f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = false;
        var ajuste = goPanel.GetComponent<ContentSizeFitter>();
        ajuste.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        ajuste.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        grupo = goPanel.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        // Tecla
        var goTecla = new GameObject("Tecla", typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        goTecla.transform.SetParent(panel, false);
        tecla = (RectTransform)goTecla.transform;
        var le = goTecla.GetComponent<LayoutElement>();
        le.preferredWidth = le.preferredHeight = 64f;
        var imgTecla = goTecla.GetComponent<Image>();
        imgTecla.color = ColorAcento;
        imgTecla.raycastTarget = false;
        var borde = goTecla.AddComponent<Outline>();
        borde.effectColor = new Color(1f, 1f, 1f, 0.55f);
        borde.effectDistance = new Vector2(2f, -2f);
        txtTecla = CrearTexto(tecla, "Letra", 38, FontStyles.Bold, TextAlignmentOptions.Center);
        txtTecla.color = new Color(0.05f, 0.08f, 0.06f);
        var rtLetra = txtTecla.rectTransform;
        rtLetra.anchorMin = Vector2.zero; rtLetra.anchorMax = Vector2.one;
        rtLetra.offsetMin = rtLetra.offsetMax = Vector2.zero;

        // Texto de la acción
        txtAccion = CrearTexto(panel, "Accion", 32, FontStyles.Normal, TextAlignmentOptions.MidlineLeft);
        txtAccion.color = new Color(0.95f, 0.95f, 0.95f);
        txtAccion.textWrappingMode = TextWrappingModes.NoWrap;
    }

    private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, float tam, FontStyles estilo, TextAlignmentOptions alin)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = tam;
        txt.fontStyle = estilo;
        txt.alignment = alin;
        txt.raycastTarget = false;
        return txt;
    }
}

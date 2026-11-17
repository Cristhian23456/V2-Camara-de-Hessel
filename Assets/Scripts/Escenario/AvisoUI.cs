using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum TipoAviso
{
    Info,
    Advertencia,
    Restringido,
    Error
}

/// <summary>
/// Avisos tipo "toast" en la parte superior de la pantalla. Se construye solo en tiempo de
/// ejecución (no requiere nada en la escena) y no bloquea los clics del jugador.
/// Uso: AvisoUI.Mostrar("Área restringida", "Solo personal autorizado.", TipoAviso.Restringido);
/// </summary>
public class AvisoUI : MonoBehaviour
{
    private static AvisoUI instancia;

    private CanvasGroup grupo;
    private Image franja;
    private TextMeshProUGUI txtTitulo;
    private TextMeshProUGUI txtMensaje;
    private Coroutine rutina;

    public static AvisoUI Instancia
    {
        get
        {
            if (instancia == null)
            {
                instancia = FindAnyObjectByType<AvisoUI>();
                if (instancia == null) instancia = new GameObject("[AvisoUI]").AddComponent<AvisoUI>();
            }
            return instancia;
        }
    }

    /// <summary>Muestra un aviso. Si ya hay uno visible, lo reemplaza.</summary>
    public static void Mostrar(string titulo, string mensaje, TipoAviso tipo = TipoAviso.Info, float duracion = 3.5f)
    {
        Instancia.MostrarInterno(titulo, mensaje, tipo, duracion);
    }

    public static Color ColorDe(TipoAviso tipo)
    {
        switch (tipo)
        {
            case TipoAviso.Advertencia: return new Color32(0xF9, 0xA8, 0x25, 0xFF);
            case TipoAviso.Restringido: return new Color32(0xD3, 0x2F, 0x2F, 0xFF);
            case TipoAviso.Error: return new Color32(0xB7, 0x1C, 0x1C, 0xFF);
            default: return new Color32(0x1E, 0x88, 0xE5, 0xFF);
        }
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        Construir();
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    private void MostrarInterno(string titulo, string mensaje, TipoAviso tipo, float duracion)
    {
        txtTitulo.text = titulo;
        txtTitulo.color = ColorDe(tipo);
        txtMensaje.text = mensaje;
        franja.color = ColorDe(tipo);

        if (rutina != null) StopCoroutine(rutina);
        rutina = StartCoroutine(Animar(Mathf.Max(0.5f, duracion)));
    }

    private IEnumerator Animar(float duracion)
    {
        const float fundido = 0.25f;
        for (float t = grupo.alpha * fundido; t < fundido; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = t / fundido;
            yield return null;
        }
        grupo.alpha = 1f;

        yield return new WaitForSecondsRealtime(duracion);

        for (float t = 0f; t < fundido; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = 1f - t / fundido;
            yield return null;
        }
        grupo.alpha = 0f;
        rutina = null;
    }

    private void Construir()
    {
        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 900;
        var escalador = gameObject.AddComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;
        // Sin GraphicRaycaster: el aviso nunca bloquea clics.

        var panel = new GameObject("Aviso", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
        panel.transform.SetParent(transform, false);
        var rt = (RectTransform)panel.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0f, -40f);
        rt.sizeDelta = new Vector2(820f, 130f);
        var fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
        fondo.raycastTarget = false;
        grupo = panel.GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
        grupo.interactable = false;

        var goFranja = new GameObject("Franja", typeof(RectTransform), typeof(Image));
        goFranja.transform.SetParent(panel.transform, false);
        var rf = (RectTransform)goFranja.transform;
        rf.anchorMin = new Vector2(0f, 0f);
        rf.anchorMax = new Vector2(0f, 1f);
        rf.pivot = new Vector2(0f, 0.5f);
        rf.sizeDelta = new Vector2(14f, 0f);
        franja = goFranja.GetComponent<Image>();
        franja.raycastTarget = false;

        txtTitulo = CrearTexto(panel.transform, "Titulo", 34, FontStyles.Bold, new Vector2(36f, -16f), 44f);
        txtMensaje = CrearTexto(panel.transform, "Mensaje", 26, FontStyles.Normal, new Vector2(36f, -62f), 60f);
        txtMensaje.color = new Color(0.93f, 0.93f, 0.93f);
    }

    private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, float tam, FontStyles estilo, Vector2 pos, float alto)
    {
        var go = new GameObject(nombre, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(-pos.x - 24f, alto);
        var txt = go.GetComponent<TextMeshProUGUI>();
        txt.fontSize = tam;
        txt.fontStyle = estilo;
        txt.textWrappingMode = TextWrappingModes.Normal;
        txt.raycastTarget = false;
        return txt;
    }
}

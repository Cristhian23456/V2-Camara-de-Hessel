using TMPro;
using UnityEngine;

/// <summary>
/// Cámara virtual de vigilancia: renderiza la sala de entrevista en una RenderTexture
/// que se muestra en una pantalla del escenario (monitor de la sala de control).
/// La cámara se renderiza "a mano" para controlar los FPS y solo cuando la pantalla
/// es visible, así no cuesta rendimiento mientras nadie la mira.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CamaraEnVivo : MonoBehaviour
{
    [Header("Salida")]
    [Tooltip("Textura donde se dibuja la imagen. Si se deja vacía se crea una de 640x360 en tiempo de ejecución.")]
    [SerializeField] private RenderTexture renderTexture;
    [Tooltip("Renderer de la pantalla que muestra la imagen (su material debe usar _MainTex).")]
    [SerializeField] private Renderer pantalla;

    [Header("Rendimiento")]
    [SerializeField, Range(1f, 60f)] private float fps = 20f;
    [Tooltip("Solo renderiza cuando alguna cámara está viendo la pantalla.")]
    [SerializeField] private bool soloSiPantallaVisible = true;

    [Header("Indicador EN VIVO (opcional)")]
    [SerializeField] private TMP_Text indicador;
    [SerializeField, Min(0.1f)] private float periodoParpadeo = 0.8f;

    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");

    private Camera cam;
    private RenderTexture rtCreada;
    private float siguienteRender;

    public RenderTexture Textura => renderTexture;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        // Desactivada: no participa en el render normal de la escena ni compite con la cámara principal.
        cam.enabled = false;

        if (renderTexture == null)
        {
            rtCreada = new RenderTexture(640, 360, 16) { name = "RT_CamaraEnVivo (runtime)" };
            renderTexture = rtCreada;
        }
        cam.targetTexture = renderTexture;

        if (pantalla != null)
        {
            var bloque = new MaterialPropertyBlock();
            pantalla.GetPropertyBlock(bloque);
            bloque.SetTexture(MainTexId, renderTexture);
            pantalla.SetPropertyBlock(bloque);
        }
    }

    private void LateUpdate()
    {
        if (indicador != null)
        {
            bool encendido = Mathf.Repeat(Time.unscaledTime, periodoParpadeo * 2f) < periodoParpadeo;
            indicador.alpha = encendido ? 1f : 0.35f;
        }

        if (soloSiPantallaVisible && pantalla != null && !pantalla.isVisible) return;
        if (Time.unscaledTime < siguienteRender) return;

        siguienteRender = Time.unscaledTime + 1f / fps;
        cam.Render();
    }

    private void OnDestroy()
    {
        if (cam != null) cam.targetTexture = null;
        if (rtCreada != null)
        {
            rtCreada.Release();
            Destroy(rtCreada);
        }
    }
}

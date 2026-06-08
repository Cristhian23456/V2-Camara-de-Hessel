using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Panel de Mensajes Estándar")]
    [SerializeField] private GameObject panelMensajeTemplate;
    [SerializeField] private TextMeshProUGUI txtTitulo;
    [SerializeField] private TextMeshProUGUI txtContenido;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Button btnPrimario;
    [SerializeField] private Button btnSecundario;

    [Header("Mensaje Flotante")]
    [SerializeField] private GameObject panelMensajeFlotante;
    [SerializeField] private TextMeshProUGUI txtFlotante;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void MostrarMensaje(
        string titulo, 
        string contenido, 
        string textoBtnPrimario = "Continuar",
        string textoBtnSecundario = "",
        Action accionPrimaria = null,
        Action accionSecundaria = null,
        bool permitirCerrar = true)
    {
        if (panelMensajeTemplate == null) return;

        panelMensajeTemplate.SetActive(true);
        if (txtTitulo != null) txtTitulo.text = titulo;
        if (txtContenido != null) txtContenido.text = contenido;
        
        // Ajustar el scroll al tope de forma asíncrona tras actualizar UI
        Canvas.ForceUpdateCanvases();
        if (scrollRect != null) scrollRect.verticalNormalizedPosition = 1f;
        
        // Configurar botón primario (Aceptar / Continuar)
        if (btnPrimario != null)
        {
            if (!string.IsNullOrEmpty(textoBtnPrimario))
            {
                btnPrimario.gameObject.SetActive(true);
                var btnText = btnPrimario.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = textoBtnPrimario;

                btnPrimario.onClick.RemoveAllListeners();
                btnPrimario.onClick.AddListener(() =>
                {
                    accionPrimaria?.Invoke();
                    if (permitirCerrar) panelMensajeTemplate.SetActive(false);
                });
            }
            else
            {
                btnPrimario.gameObject.SetActive(false);
            }
        }
        
        // Configurar botón secundario (Volver / Cancelar)
        if (btnSecundario != null)
        {
            if (!string.IsNullOrEmpty(textoBtnSecundario))
            {
                btnSecundario.gameObject.SetActive(true);
                var btnText = btnSecundario.GetComponentInChildren<TextMeshProUGUI>();
                if (btnText != null) btnText.text = textoBtnSecundario;

                btnSecundario.onClick.RemoveAllListeners();
                btnSecundario.onClick.AddListener(() =>
                {
                    accionSecundaria?.Invoke();
                    if (permitirCerrar) panelMensajeTemplate.SetActive(false);
                });
            }
            else
            {
                btnSecundario.gameObject.SetActive(false);
            }
        }
    }

    public void MostrarMensajeFlotante(string mensaje, float duracion = 3f)
    {
        if (panelMensajeFlotante == null || txtFlotante == null) return;
        
        StopAllCoroutines();
        StartCoroutine(MensajeFlotanteCoroutine(mensaje, duracion));
    }

    private System.Collections.IEnumerator MensajeFlotanteCoroutine(string mensaje, float duracion)
    {
        txtFlotante.text = mensaje;
        panelMensajeFlotante.SetActive(true);
        yield return new WaitForSecondsRealtime(duracion);
        panelMensajeFlotante.SetActive(false);
    }
}

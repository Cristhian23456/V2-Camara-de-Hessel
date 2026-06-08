using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ControladorCinematica : MonoBehaviour
{
    [Header("Animaciones de recorrido")]
    [SerializeField] private Animator recorridoController;
    [SerializeField] private string nombreBoolRecorrido = "recorrer";
    [SerializeField] private string nombreEstadoRecorrido = "Recorrido"; // Estado de la animaci�n en el Animator
    [Header("C�maras")]
    [SerializeField] private Camera camaraRecorrido;
    [SerializeField] private GameObject player;
    [Header("Puertas para abrir cerrar")]
    [SerializeField] private GameObject[] listPuertas;
    [SerializeField] private GameObject puertaControlAbierta;
    [SerializeField] private GameObject puertaControlCerrada;
    [Header("Panel de indicaciones")]
    [SerializeField] private GameObject panelIndicacion;
    private bool isSkipping = false;
    [Header("Inidicaciones juego")]
    [SerializeField] private GameObject btnContinuar;
    [SerializeField] private Button btnSaltar;
    [SerializeField] private GameObject panelInstruccioneJuego;
    [SerializeField] private AudioClip audioRecorrido;
    [SerializeField] private AudioSource audioSorce;
    [SerializeField] private AudioSource audioSourceFondo;

    void Start()
    {
        // Asegurar que el tiempo del juego esté activo y no pausado por una sesión anterior
        Time.timeScale = 1f;

        if (btnContinuar != null)
        {
            btnContinuar.SetActive(false);
        }

        if (btnSaltar != null)
        {
            btnSaltar.onClick.AddListener(() =>
            {
                darFuncionalidaBotonSC();
            });
        }

        // Auto-cargue directo y automático del juego en 2 segundos reales
        StartCoroutine(AutoStartCinematicCoroutine());
    }

    private IEnumerator AutoStartCinematicCoroutine()
    {
        yield return new WaitForSecondsRealtime(2.0f);
        darFuncionalidaBotonSC();
    }

    void Update()
    {
        if (recorridoController.GetBool(nombreBoolRecorrido))
        {
            

            if (Input.GetKeyDown(KeyCode.Space))
            {
                FinalizarCinematica();
                Debug.Log("Cinem�tica saltada");
            }
        }

        // Verifica si la animaci�n ha terminado
        if (!isSkipping && IsAnimationFinished())
        {
            FinalizarCinematica();
            Debug.Log("Cinemtica finalizada");
        }
    }

    // Método para verificar si la animación ha terminado
    private bool IsAnimationFinished()
    {
        var animStateInfo = recorridoController.GetCurrentAnimatorStateInfo(0);
        return animStateInfo.IsName(nombreEstadoRecorrido) && animStateInfo.normalizedTime >= 1.0f;
    }

    // Método para finalizar la cinemática
    private void FinalizarCinematica()
    {
        audioSorce.Stop();
        isSkipping = true;
        recorridoController.SetBool(nombreBoolRecorrido, false); // Detener la animación
        player.SetActive(true);
        abrirPuertas(true);
        camaraRecorrido.gameObject.SetActive(false);
        Debug.Log("Cinemática finalizada");
        panelIndicacion.SetActive(false);
        audioSourceFondo.Play();
        audioSourceFondo.loop = true;
        puertaControlCerrada.SetActive(false);
        puertaControlAbierta.SetActive(true);

        DisableScreenshotInPanel(panelInstruccioneJuego);
        panelInstruccioneJuego.SetActive(true);
    }

    private void DisableScreenshotInPanel(GameObject panel)
    {
        if (panel == null) return;
        
        RawImage[] rawImages = panel.GetComponentsInChildren<RawImage>(true);
        foreach (var img in rawImages)
        {
            if (img.gameObject.name.Contains("RawImage") || img.gameObject.name.Contains("captura") || img.gameObject.name.Contains("Screen"))
            {
                img.gameObject.SetActive(false);
            }
        }
        
        Image[] images = panel.GetComponentsInChildren<Image>(true);
        foreach (var img in images)
        {
            if (img.gameObject.name.Contains("RawImage") || img.gameObject.name.Contains("captura") || img.gameObject.name.Contains("Screen"))
            {
                img.gameObject.SetActive(false);
            }
        }
    }


    public void darFuncionalidaBotonSC()
    {
        //panelInstruccioneJuego.SetActive(true);
        audioSorce.clip = audioRecorrido;
        audioSorce.Play();
        panelIndicacion.SetActive(true);
        player.SetActive(false);
        abrirPuertas(false);
        recorridoController.SetBool(nombreBoolRecorrido, true); // Inicia la animaci�n
    }

    // M�todo para abrir o cerrar puertas
    void abrirPuertas(bool estado)
    {
        foreach (GameObject puerta in listPuertas)
        {
            puerta.SetActive(estado);
        }
    }
}

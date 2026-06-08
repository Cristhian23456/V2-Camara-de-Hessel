using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class PresentarInfoSalas : MonoBehaviour
{
    [SerializeField]
    private GameObject panelInformacion;
    [SerializeField]
    private TextMeshProUGUI txtTituloSala;
    [SerializeField]
    private TextMeshProUGUI txtCuerpoSala;
    [SerializeField]
    private GameObject btnAceptar;
    [SerializeField]
    private GameObject btnComenzar;
    [SerializeField]
    private string titulo;
    [SerializeField]
    [TextArea(3,2)]
    private string descripcion;
    [SerializeField]
    private GameObject player;
    [SerializeField]
    private Camera mainCamera;
    [SerializeField]
    private GameObject introController;
    [Header("Animaciones")]
    [SerializeField]
    private GameObject panelAnimacion, cmaraAnimacion;
    [SerializeField]
    private TextMeshProUGUI txtAnimaciones;
    [SerializeField]
    private GameObject PerAbriPuertaC1, PerAbriPuertaC4;
    [SerializeField]
    private AnimationClip animAbrir;
    [SerializeField]
    private Animator animTerapeutaC1,animTerapeutaC4;
    [SerializeField]
    private GameObject[] abriendoPuerta;
    [SerializeField] private AudioClip audioPuerta;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private ApiManager apiManager;
    [SerializeField]
    private DialogosManager dialogosManager; // Referencia al DialogoManager


    // Start is called before the first frame update
    void Start()
    {
        // Acortar descripciones dinámicamente para dejar solo lo importante y legible (Screen 4)
        if (!string.IsNullOrEmpty(titulo))
        {
            if (titulo.Contains("Archivo"))
            {
                descripcion = "Área destinada al almacenamiento seguro de archivos físicos y digitales confidenciales, además de una biblioteca de instrumentos psicológicos de acceso restringido.";
            }
            else if (titulo.Contains("Gesell"))
            {
                descripcion = "Simulador de la Cámara de Gesell compuesto por dos espacios separados por un espejo unidireccional, diseñado para la observación y análisis ético del comportamiento humano.";
            }
            else if (titulo.Contains("control"))
            {
                descripcion = "Sala dedicada al control técnico, registro de audio/video y conmutación de señales para las sesiones en la Cámara de Gesell y consultorios.";
            }
            else if (titulo.Contains("Laboratorio"))
            {
                descripcion = "Laboratorio con equipamiento y multimedia para prácticas de psicología experimental en un entorno profesional controlado.";
            }
            else if (titulo.Contains("observación") || titulo.Contains("observacion"))
            {
                descripcion = "Área oculta destinada a la observación y registro directo de conductas en tiempo real durante las entrevistas de la Cámara de Gesell.";
            }
            else if (titulo.Contains("Consultorio"))
            {
                descripcion = "Espacio fundamental equipado con audio y video para el desarrollo de habilidades terapéuticas e intervenciones psicológicas individuales o de pareja bajo supervisión.";
            }
        }

        // Aplicar márgenes/padding a los cuadros de texto para que se vean limpios
        if (txtCuerpoSala != null)
        {
            txtCuerpoSala.margin = new Vector4(20f, 15f, 20f, 50f); // 50f abajo para dejar espacio al botón
        }
        if (txtTituloSala != null)
        {
            txtTituloSala.margin = new Vector4(20f, 10f, 20f, 5f);
        }

        // Posicionar de manera fija los botones de Aceptar en la parte inferior central del panel
        if (btnAceptar != null)
        {
            RectTransform btnRect = btnAceptar.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchorMin = new Vector2(0.5f, 0f);
                btnRect.anchorMax = new Vector2(0.5f, 0f);
                btnRect.pivot = new Vector2(0.5f, 0f);
                btnRect.anchoredPosition = new Vector2(0f, 20f);
            }
        }

        // Posicionar de manera fija los botones de Comenzar en la parte inferior central del panel
        if (btnComenzar != null)
        {
            RectTransform btnRect = btnComenzar.GetComponent<RectTransform>();
            if (btnRect != null)
            {
                btnRect.anchorMin = new Vector2(0.5f, 0f);
                btnRect.anchorMax = new Vector2(0.5f, 0f);
                btnRect.pivot = new Vector2(0.5f, 0f);
                btnRect.anchoredPosition = new Vector2(0f, 20f);
            }
        }

        btnAceptar.gameObject.SetActive(false);
        if (gameObject.name == "Entrada3")
        {
            btnComenzar.SetActive(false);
            PerAbriPuertaC1.SetActive(false);
            PerAbriPuertaC4.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private Coroutine exitCoroutine;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            if (exitCoroutine != null)
            {
                StopCoroutine(exitCoroutine);
                exitCoroutine = null;
            }

            if (!panelInformacion.activeSelf || txtTituloSala.text != titulo)
            {
                btnAceptar.gameObject.SetActive(false);
                panelInformacion.SetActive(true);
                txtTituloSala.text = titulo;
                StopAllCoroutines();
                StartCoroutine(escribirInformacion(descripcion, btnAceptar));
            }

            // Cargar recursos del caso en segundo plano al llegar a la puerta del consultorio
            if (gameObject.name == "Entrada3" && apiManager != null)
            {
                apiManager.CargarRecursosCaso();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            if (exitCoroutine != null)
            {
                StopCoroutine(exitCoroutine);
            }
            exitCoroutine = StartCoroutine(DebounceExitCoroutine());
        }
    }

    private IEnumerator DebounceExitCoroutine()
    {
        yield return new WaitForSecondsRealtime(0.5f);
        btnAceptar.gameObject.SetActive(false);
        panelInformacion.SetActive(false);
        StopAllCoroutines();
        exitCoroutine = null;
    }

    IEnumerator escribirInformacion(string texto,GameObject button)
    {
        txtCuerpoSala.maxVisibleCharacters = 0;
        txtCuerpoSala.text = texto;
        txtCuerpoSala.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txtCuerpoSala.maxVisibleCharacters++;
            yield return new WaitForSeconds(2f / 500);
        }
        if(gameObject.name== "Entrada3")
        {
            btnComenzar.SetActive(true);
        }
        else
        {
            button.GetComponent<Button>().onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
            });
            button.SetActive(true);
        }
    }

    public void fnBtnEmpezar()
    {
        player.SetActive(false);
        panelInformacion.SetActive(false);

        if (gameObject.name == "Entrada3")
        {
            StartCoroutine(esperarIntro());
        }
    }

    IEnumerator esperarIntro()
    {
        yield return new WaitForSeconds(0.1f);
        introController.gameObject.SetActive(true);
        mainCamera.gameObject.SetActive(true);
    }

    public IEnumerator ejecutarAnimacion()
    {
        audioSource.clip = audioPuerta;
        audioSource.Play();
        txtAnimaciones.text = "(Paciente toca la puerta) \n" +
              "   (Terapeuta abre la puerta e invita a pasar a la paciente)";
        
        cmaraAnimacion.SetActive(true);
        panelAnimacion.SetActive(true);
        if (apiManager.getNroCaso() == 4)
        {
            PerAbriPuertaC4.SetActive(true);
            animTerapeutaC4.SetBool("abrir", true);
        }else if(apiManager.getNroCaso() == 1)
        {
            PerAbriPuertaC1.SetActive(true);
            animTerapeutaC1.SetBool("abrir", true);
        }
        
        yield return new WaitForSeconds(animAbrir.length/2);
        if (apiManager.getNroCaso() == 4)
        {
            PerAbriPuertaC4.SetActive(true);
            animTerapeutaC4.SetBool("abrir", false);
        }
        else if(apiManager.getNroCaso() == 1)
        { 
            PerAbriPuertaC1.SetActive(true);
            animTerapeutaC1.SetBool("abrir", false);
        }
    
        abriendoPuerta[0].SetActive(false);
        abriendoPuerta[1].SetActive(true);
       
        yield return new WaitForSeconds(3.2f);
        abriendoPuerta[1].SetActive(false);
        abriendoPuerta[0].SetActive(true);
        cmaraAnimacion.SetActive(false);
        panelAnimacion.SetActive(false);
        mainCamera.gameObject.SetActive(true);
        PerAbriPuertaC1.SetActive(false);
        PerAbriPuertaC4.SetActive(false);
        dialogosManager.darFuncionBtnAceptar();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class BeckInventory : MonoBehaviour
{
   [SerializeField]
    private TMP_InputField resultText;
    [SerializeField]
    private Button btnEnviar;
    [SerializeField]
    private int resultado;
    [SerializeField]
    private int nroCaso;
    [SerializeField]
    private DialogosManager dialogosManager;
    [SerializeField]
    private ApiManager apiManager;
    [SerializeField]
    private GameObject panelBeck, btnAceptar, objectGuia;
    [SerializeField]
    private int [] listResultados;
    [SerializeField]
    [TextArea(5,8)]
    private string notaBeck;
  
    [SerializeField]
    private TextMeshProUGUI txtNota;
    [SerializeField]
    private GameObject [] uiCuestionarioBeck;

    [SerializeField]
    private TextMeshProUGUI txtError;
    [SerializeField]
    private GameObject panelAlerta, btnAceptarAlert, panelRetroalimentacionFase;
    [SerializeField]
    private Button btnContinuar;
    [SerializeField]
    private TextMeshProUGUI lblTitulo;
    [SerializeField]
    private TextMeshProUGUI [] lblEncabezado;
    [SerializeField]
    private AudioClip audioNotaBeck;
    [SerializeField]
    private Calificacion calificacion;
    private bool estado = true;
    [Header("Escenario Trabajo")]
    [SerializeField]
    private GameObject escenarioTrabPsicologo;
    [SerializeField]
    private GameObject escenarioTrabPsicologo4;
    [Header("Propiedades de indicacacion de tiempo")]
    [SerializeField]
    private GameObject panelIndicacionTiempo;
    [SerializeField]
    private TextMeshProUGUI txtIndicaciones;
    [SerializeField]
    [TextArea(4, 2)]
    private string [] indicacionesSesion;
    [SerializeField]
    private Button btnContinuarFase;
    [SerializeField]
    private AudioClip audioIndicaciones;
    [SerializeField]
    private AudioSource audioSource;
    [Header("Finalizar el caso")]
    [SerializeField]
    private FinalizarCaso fnCaso;
    [Header("Animaciones")]
    [SerializeField]
    private GameObject panelAnimaciones, camaraAnimacion;
    [SerializeField]
    private TextMeshProUGUI txtAnimaciones;
    [SerializeField]
    private GameObject PerAbriPuerta;
    [SerializeField]
    private GameObject PerAbriPuertaC4;
    [SerializeField]
    private AnimationClip animAbrir;
    [SerializeField]
    private Animator animTerapeuta;
    [SerializeField]
    private Animator animTerapeutaC4;
    [SerializeField]
    private GameObject[] abriendoPuerta;
    [SerializeField]
    private SaveData saveData;
    [SerializeField]
    private AudioSource audioPuerta;
    void Start()
    {
        saveData = GameObject.Find("LoginController").GetComponent<SaveData>();

        if (btnEnviar != null)
        {
            btnEnviar.onClick.RemoveAllListeners();
            btnEnviar.onClick.AddListener(fnBtnEnviar);
        }

        // Programmatically deactivate any duplicate scene alert panel to avoid overlaps
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        foreach (var obj in allObjects)
        {
            if (obj.name == "uiAlerta (1)" && obj != panelAlerta)
            {
                obj.SetActive(false);
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void fnBtnEnviar() {
        // 1. Validación segura
        if (string.IsNullOrWhiteSpace(resultText.text) || 
            !int.TryParse(resultText.text, out int resultadoIngresado))
        {
            MostrarAlerta("Por favor, ingresa un puntaje numérico válido.", false);
            return;
        }

        CaseSetupSO config = apiManager.GetCaseSetup(apiManager.nroCaso);
        int puntajeCorrecto = config != null ? config.puntajeCorrectoBeck : (listResultados != null && listResultados.Length >= apiManager.nroCaso ? listResultados[apiManager.nroCaso - 1] : 32);

        if (resultadoIngresado != puntajeCorrecto)
        {
            MostrarAlerta("Los criterios no coinciden, realiza bien el conteo.", false);
            calificacion.decrementar(calificacion.valorIncorrecto);
        }
        else
        {
            if (estado == true)
            {
                calificacion.incrementar(calificacion.valorPregunta);
                calificacion.incrementarFinal(calificacion.valorPregunta);
                calificacion.incrementarContador();
                estado = false;
            }

            MostrarAlerta("Felicitaciones, has calculado correctamente los puntajes del inventario de Beck.", true);
        }
    }

    private void MostrarAlerta(string mensaje, bool success)
    {
        panelAlerta.SetActive(true);
        txtError.text = mensaje;

        if (success)
        {
            if (btnAceptarAlert != null) btnAceptarAlert.SetActive(false);
            StartCoroutine(AutoCloseAlertCoroutine());
        }
        else
        {
            if (btnAceptarAlert != null)
            {
                btnAceptarAlert.SetActive(true);
                btnAceptarAlert.GetComponent<Button>().onClick.RemoveAllListeners();
                btnAceptarAlert.GetComponent<Button>().onClick.AddListener(() =>
                {
                    panelAlerta.SetActive(false);
                });
            }
        }
    }

    private IEnumerator AutoCloseAlertCoroutine()
    {
        yield return new WaitForSecondsRealtime(1.8f);
        panelAlerta.SetActive(false);
        panelBeck.SetActive(false);

        int caso = apiManager.nroCaso;
        if (escenarioTrabPsicologo != null) escenarioTrabPsicologo.SetActive(caso == 1);
        if (escenarioTrabPsicologo4 != null) escenarioTrabPsicologo4.SetActive(caso == 4);

        if (fnCaso != null) fnCaso.activarPreguntaBeck();
        if (panelRetroalimentacionFase != null) panelRetroalimentacionFase.SetActive(false);
    }
    public void notaInventarioBecker()
    {
        btnAceptar.SetActive(false);
        objectGuia.SetActive(true);
       
     StartCoroutine(escribirTexto(notaBeck, txtNota, btnAceptar));

        nroCaso = apiManager.nroCaso;
        if (nroCaso == 1)
        {
            escenarioTrabPsicologo.SetActive(true);

        }
        else if (nroCaso == 4)
        {
            escenarioTrabPsicologo4.SetActive(true);
        }
        audioSource.clip = audioNotaBeck;
        audioSource.Play();
        btnAceptar.GetComponent<Button>().onClick.RemoveAllListeners();
        btnAceptar.GetComponent<Button>().onClick.AddListener(() => {
            objectGuia.SetActive(false);
            panelBeck.SetActive(true);
            if (nroCaso == 1)
            {
                escenarioTrabPsicologo.SetActive(false);

            }
            else if (nroCaso == 4)
            {
                escenarioTrabPsicologo4.SetActive(false);
            }


        });
       
        for (int i = 0; i < uiCuestionarioBeck.Length; i++)
        {
            uiCuestionarioBeck[i].SetActive(false);
            lblEncabezado[i].gameObject.SetActive(false);

        }
        uiCuestionarioBeck[nroCaso - 1].SetActive(true);
        lblEncabezado[nroCaso - 1].gameObject.SetActive(true);
    }

    IEnumerator escribirTexto(string texto, TextMeshProUGUI txt, GameObject btn)
    {
        txt.maxVisibleCharacters = 0;
        txt.text = texto;
        txt.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txt.maxVisibleCharacters++;
            yield return new WaitForSeconds(35f / 500);

        }
        if (btn != null)
        {
            btn.gameObject.SetActive(true);
        }
    }

    public void funcionBtnContinuar()
    {
      
       
        btnContinuarFase.onClick.RemoveAllListeners();
        btnContinuarFase.onClick.AddListener(() =>
        {
            panelIndicacionTiempo.SetActive(false);
            StopAllCoroutines();
            StartCoroutine(esperarAnimaciones());
          

        });
    }
    public void continuarSesion()
    {
      
        escenarioTrabPsicologo.SetActive(false);
        escenarioTrabPsicologo4.SetActive(false);
        panelRetroalimentacionFase.SetActive(true);
        lblTitulo.text = "Felicidades, has terminado la fase de desarrollo.";
        btnContinuar.onClick.RemoveAllListeners();
        btnContinuar.onClick.AddListener(() =>
        {
            if (saveData.modo != "Evaluacion")
            {
                saveData.updateUserIntentEntry(System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"), calificacion.ValorPorcentaje + "%", calificacion.puntuacionActual);

            }
            panelRetroalimentacionFase.SetActive(false);
            panelIndicacionTiempo.SetActive(true);
            StopAllCoroutines();
            StartCoroutine(escribirTexto(indicacionesSesion[0], txtIndicaciones, btnContinuarFase.gameObject));

            btnContinuarFase.onClick.RemoveAllListeners();
            btnContinuarFase.onClick.AddListener(() =>
            {
                panelIndicacionTiempo.SetActive(false);
                StopAllCoroutines();
                StartCoroutine(esperarAnimaciones());
            });

        });

    }
    IEnumerator esperarAnimaciones()
    {
        audioPuerta.Play();
        txtAnimaciones.text = "(Paciente toca la puerta) \n" +
           "(Terapeuta abre la puerta e invita a pasar a la paciente)";
        if (nroCaso == 1)
        {
            PerAbriPuerta.SetActive(true);
            animTerapeuta.SetBool("abrir", true);
        }else if (nroCaso == 4)
        {
            PerAbriPuertaC4.SetActive(true);
            animTerapeutaC4.SetBool("abrir", true);
        }
        
        panelAnimaciones.SetActive(true);
        camaraAnimacion.SetActive(true);

        
        yield return new WaitForSeconds(animAbrir.length / 2);
       
        if (nroCaso == 1)
        {
           
            animTerapeuta.SetBool("abrir", false);
        }
        else if (nroCaso == 4)
        {
            animTerapeutaC4.SetBool("abrir", false);
        }
        abriendoPuerta[0].SetActive(false);
        abriendoPuerta[1].SetActive(true);

        yield return new WaitForSeconds(3.0f);
        abriendoPuerta[1].SetActive(false);
        abriendoPuerta[0].SetActive(true);
        dialogosManager.iniciarFase("Final");
        dialogosManager.ubicarPersonajeCentro();
        dialogosManager.darFuncionBtnAceptar();
        camaraAnimacion.SetActive(false);
        panelAnimaciones.SetActive(false);
        if (nroCaso == 1)
        {

            PerAbriPuerta.SetActive(false);
        }
        else if (nroCaso == 4)
        {
            PerAbriPuertaC4.SetActive(false);
        }
      
    }

}

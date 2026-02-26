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
    }

    // Update is called once per frame
    void Update()
    {
        
    }
  public void fnBtnEnviar() {
       
        resultado = int.Parse(resultText.text);
             if (resultado != listResultados[nroCaso - 1])
                {
                    Debug.Log("Los criterios no coinciden, realiza bien el conteo");
                    panelAlerta.SetActive(true);
                    txtError.text = "Los criterios no coinciden, realiza bien el conteo";
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
            Debug.Log("Los puntaje coiniciden");
            panelAlerta.SetActive(true);
            txtError.text = "Felicitaciones, has calculado correctamente los puntajes del inventario de Beck.";

            btnAceptarAlert.GetComponent<Button>().onClick.RemoveAllListeners();
            btnAceptarAlert.GetComponent<Button>().onClick.AddListener(() =>
            {
                panelBeck.SetActive(false);
                if (nroCaso == 1)
                {
                    escenarioTrabPsicologo.SetActive(true);

                }
                else if(nroCaso == 4)
                {
                    escenarioTrabPsicologo4.SetActive(true);
                }
              

                fnCaso.activarPreguntaBeck();
                panelRetroalimentacionFase.SetActive(false);
                    
                    btnContinuarFase.onClick.RemoveAllListeners();
                    btnContinuarFase.onClick.AddListener(() =>
                    {
                        panelIndicacionTiempo.SetActive(true);
                        StopAllCoroutines();
                        StartCoroutine(escribirTexto(indicacionesSesion[0], txtIndicaciones, btnContinuarFase.gameObject));
                        estado = true;
                        funcionBtnContinuar();
                    });

                });
            
             }
              
        
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinalizarCaso : MonoBehaviour
{
    [SerializeField]
    private GameObject panelRetroalimentacionFase;
    [SerializeField]
    private Button btnContinuar;
    [SerializeField]
    private TextMeshProUGUI lblTitulo;
    [SerializeField]
    private SceneChange sceneChange;
    [SerializeField]
    private BeckInventory beckInventory;
    [SerializeField]
    private ApiManager apiManager;
    [Header("Retroalimentacion")]
    [SerializeField]
    private GameObject panelRetrolimentacion;
    [SerializeField]
    private TextMeshProUGUI txtRetroalimentacion;
    [SerializeField]
    private GameObject btnAceptarRetro;
    [SerializeField]
    [TextArea(4,2)]
    private string textoRetro;
    [SerializeField]
    private GameObject panelOpciones;
    [SerializeField]
    private AudioClip audioUltimo;
    [SerializeField]
    private AudioSource audioSource;
    [SerializeField]
    private Toggle[] listToggle;

    [Header("Alerta")]
    [SerializeField]
    private GameObject panelAlerta;
    [SerializeField]
    private TextMeshProUGUI txtAlerta;
    [SerializeField]
    private Button btnAceptar;
    [SerializeField]
    private Calificacion calificacion;
    bool estado=true;
    [SerializeField]
    private SaveData saveData;
    
    // Start is called before the first frame update
    void Start()
    {

        saveData = GameObject.Find("LoginController").GetComponent<SaveData>();

      


       
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    public void activarRetroFinal()
    {
        panelRetroalimentacionFase.SetActive(true);
        lblTitulo.text = "Felicidades, has terminado este caso clinico";
        btnContinuar.onClick.RemoveAllListeners();
        btnContinuar.onClick.AddListener(() => {
            if (saveData.modo != "Evaluacion")
            {
                saveData.updateUserIntentEntry(System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"), calificacion.ValorPorcentaje + "%", calificacion.puntuacionActual);
            }
            
            panelRetroalimentacionFase.SetActive(false);
            sceneChange.changeScena("Iniciar Sesion");
        });
    }

    public void activarPreguntaBeck()
    {
        Debug.Log("Entre a este metodo");
        if (apiManager.getNroCaso() == 1)
        {
            listToggle[0].onValueChanged.AddListener(delegate
            {
                if (listToggle[0].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionCorrecta();

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[1].onValueChanged.AddListener(delegate
            {
                if (listToggle[1].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[1]);

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[2].onValueChanged.AddListener(delegate
            {
                if (listToggle[2].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[2]);
                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[3].onValueChanged.AddListener(delegate
            {
                if (listToggle[3].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[3]);

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
        }
        else if (apiManager.getNroCaso() == 4)
        {
            listToggle[0].onValueChanged.AddListener(delegate
            {
                if (listToggle[0].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[0]);

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[1].onValueChanged.AddListener(delegate
            {
                if (listToggle[1].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[1]);

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[2].onValueChanged.AddListener(delegate
            {
                if (listToggle[2].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[2]);
                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
            listToggle[3].onValueChanged.AddListener(delegate
            {
                if (listToggle[3].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");

                    opcionCorrecta();
                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
        }

        panelRetrolimentacion.SetActive(true);
        btnAceptarRetro.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(escribirTexto(textoRetro, txtRetroalimentacion,panelOpciones));
        audioSource.clip = audioUltimo;
        audioSource.Play();
    }


    IEnumerator escribirTexto(string texto, TextMeshProUGUI txt,GameObject panelOpciones)
    {
        txt.maxVisibleCharacters = 0;
        txt.text = texto;
        txt.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txt.maxVisibleCharacters++;
            yield return new WaitForSeconds(35f / 500);

        }
        panelOpciones.SetActive(true);

   


    }

    public void opcionCorrecta()
    {
        
        panelAlerta.SetActive(true);
        panelOpciones.SetActive(false);
        txtAlerta.text = "El rango de la puntuación seleccionada es la correcta";
        if (estado == true)
        {
            calificacion.incrementar(calificacion.valorPregunta);
            calificacion.incrementarFinal(calificacion.valorPregunta);
            calificacion.incrementarContador();
            estado = false;
        }
        btnAceptar.onClick.RemoveAllListeners();
        btnAceptar.onClick.AddListener(() =>
        {
            estado = true;
            panelAlerta.SetActive(false);
            panelRetrolimentacion.SetActive(false);
            beckInventory.continuarSesion();
        });
    }

    public void opcionIncorrecta(Toggle toggle)
    {
        panelAlerta.SetActive(true);
        panelOpciones.SetActive(false);
        panelRetrolimentacion.SetActive(false);
        txtAlerta.text = "Incorrecto revisa los rangos de la tabla";
        if (estado == true)
        {
            calificacion.decrementar(calificacion.valorIncorrecto);
            estado = true;
        }
        btnAceptar.onClick.RemoveAllListeners();
        btnAceptar.onClick.AddListener(() =>
        {
            panelAlerta.SetActive(false);
            panelRetrolimentacion.SetActive(false);
            toggle.isOn = false;
            activarPreguntaBeck();
        });
    }

    // Este es el m�todo que se ejecutar� cuando el Toggle cambie de estado
 public void asignarMetodos()
    {
        for(int i=1; i <listToggle.Length-1; i++)
        {
            print(listToggle[i].gameObject.name);

            listToggle[i].onValueChanged.AddListener(delegate
            {
                print(listToggle[i].gameObject.name);
                if (listToggle[i].isOn)
                {
                    // L�gica cuando el Toggle est� activado (true)
                    Debug.Log("Toggle is On");
                    opcionIncorrecta(listToggle[i]);

                }
                else
                {
                    // L�gica cuando el Toggle est� desactivado (false)
                    Debug.Log("Toggle is Off");
                }
            });
        }



    }

    public void FuncionEnviarSalir()
    {
        if (saveData.modo != "Evaluacion")
        {
            saveData.updateUserIntentEntry(System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"), calificacion.ValorPorcentaje + "%", calificacion.puntuacionActual);
        }

        
        sceneChange.changeScena("Iniciar Sesion");
    }
}

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
        // 1. LIMPIAR listeners previos para evitar duplicados
        foreach (var toggle in listToggle)
        {
            toggle.onValueChanged.RemoveAllListeners();
        }

        // 2. Determinar cuál es la respuesta correcta según el caso
        int indexCorrecto = (apiManager.getNroCaso() == 1) ? 0 : 3; // Caso 1: Indice 0 es correcto. Caso 4: Indice 3 es correcto.

        // 3. Asignar lógica a TODOS los toggles de forma dinámica
        for (int i = 0; i < listToggle.Length; i++)
        {
            int currentIndex = i; // Captura local para el closure
            listToggle[i].onValueChanged.AddListener(delegate(bool isOn)
            {
                if (isOn)
                {
                    // Si el toggle activado es el correcto
                    if (currentIndex == indexCorrecto)
                    {
                        opcionCorrecta();
                    }
                    else
                    {
                        opcionIncorrecta(listToggle[currentIndex]);
                    }
                }
            });
        }

        // 4. Mostrar UI
        panelRetrolimentacion.SetActive(true);
        btnAceptarRetro.SetActive(false);
        StopAllCoroutines();
        StartCoroutine(escribirTexto(textoRetro, txtRetroalimentacion, panelOpciones));
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
            yield return new WaitForSeconds(2f / 500);

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

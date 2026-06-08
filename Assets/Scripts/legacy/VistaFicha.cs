using System.Collections;
using System.Collections.Generic;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VistaFicha : MonoBehaviour
{
    [SerializeField]
    private GameObject uiResFicha;
    [SerializeField]
    private GameObject ScrollViewContent;
    [SerializeField] 
    private GameObject Ficha;
    [SerializeField]
    private GameObject vista;
    [SerializeField]
    private GameObject btnContinuar;
    private bool estado= true;
    public GameObject alerta;
    public string[] FichaC1 =
    {
        "el paciente durante la entrevista mencionó que se ha sentido desanimado regularmente por no poder manejar todo por su cuenta.",
        "el paciente mencionó que ha perdido el interés en las actividades que solía realizar.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con la pérdida de peso.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con problemas de sueño.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con problemas psicomotores.",
        "el paciente manifestó que se ha sentido exhausto y frustrado.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con sentimientos de culpabilidad.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con su concentración.",
        "el paciente durante la entrevista no mencionó algún síntoma relacionado con intentos suicidas."
    };
    public List<PreguntasResFinal> preguntasList = new List<PreguntasResFinal>();
    [SerializeField]
    private GameObject uiRes;
    [SerializeField]
    private GameObject scrollViewContentPre;
    [SerializeField]
    private GameObject vistaPre;

    public List<GameObject> elements = new List<GameObject>();
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void presentarLista(bool[] array, bool[] arrayRespuestas)
    {
        uiResFicha.SetActive(true);
        for (int i = 0; i < array.Length; i++)
        {
            string opcion = "No";
            string esCorrecto = "Incorrecto, ";
            Color colorP = new Color();
            colorP = Color.red;
            colorP.a = 184F;

            int valor = i + 1;
            if (array[i] == arrayRespuestas[i])
            {
                colorP = Color.green;
                esCorrecto = "Correcto, ";
            }

            vista.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = valor.ToString();
            
            if (arrayRespuestas[i]==true)
            {
                opcion = "Si";
                
            }
            vista.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = opcion;
            vista.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = esCorrecto + FichaC1[i];
            vista.gameObject.GetComponent<RawImage>().color = colorP;

            GameObject panel = (GameObject)Instantiate(vista);
            panel.transform.SetParent(ScrollViewContent.transform);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = Vector3.one;

        }
        btnContinuar.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() =>
        {
            ActivarDesactivarPantalla();
        }) ;
        estado = false;
        GameObject panel1 = (GameObject)Instantiate(btnContinuar);
        panel1.transform.SetParent(ScrollViewContent.transform);
        panel1.transform.localPosition = Vector3.zero;
        panel1.transform.localScale = Vector3.one;
    }
    public void ActivarDesactivarPantalla()
    {
        uiResFicha.SetActive(estado);
        estado = !estado;
    }

    public void addPregunta(string pregunta, string respuesta, string retro)

    {
        PreguntasResFinal pre = new PreguntasResFinal(pregunta, respuesta, retro);
        preguntasList.Add(pre);
        //Debug.LogError(preguntasList[0].pregunta);
    }

    public void presentarListaPreguntas()
    {
        LImpiar();
        for (int i = 0; i < preguntasList.Count; i++)
        {
            int valor = i + 1;

            vistaPre.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = valor.ToString()+".";

            vistaPre.transform.GetChild(1).GetComponent<TextMeshProUGUI>().text = preguntasList[i].pregunta;
            vistaPre.transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = preguntasList[i].respuesta;
            vistaPre.transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = preguntasList[i].retroalimentacion;

            GameObject panel = (GameObject)Instantiate(vistaPre);
            panel.transform.SetParent(scrollViewContentPre.transform);
            panel.transform.localPosition = Vector3.zero;
            panel.transform.localScale = Vector3.one;

        }
    }
    void obtenerLista()
    {
        for (int i = 0; i < scrollViewContentPre.transform.childCount; i++)
        {
            elements.Add(scrollViewContentPre.transform.GetChild(i).gameObject);
        }
    }
    void LImpiar()
    {
        obtenerLista();
        foreach (GameObject element in elements)
        {
            Destroy(element);
        }
    }
}

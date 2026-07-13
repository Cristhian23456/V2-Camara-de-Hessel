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
    private bool estado = true;
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
            
            // Colores suaves para respuestas incorrectas (Fondo coral suave, texto rojo oscuro)
            Color colorBg = new Color32(254, 226, 226, 255); 
            Color colorTxt = new Color32(153, 27, 27, 255);

            int valor = i + 1;
            if (array[i] == arrayRespuestas[i])
            {
                // Colores suaves para respuestas correctas (Fondo verde menta suave, texto verde bosque)
                colorBg = new Color32(220, 252, 231, 255);
                colorTxt = new Color32(22, 101, 52, 255);
                esCorrecto = "Correcto, ";
            }

            var text0 = vista.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
            text0.text = valor.ToString();
            text0.color = colorTxt;
            
            if (arrayRespuestas[i] == true)
            {
                opcion = "Si";
            }

            var text1 = vista.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
            text1.text = opcion;
            text1.color = colorTxt;

            var text2 = vista.transform.GetChild(2).GetComponent<TextMeshProUGUI>();
            text2.text = esCorrecto + FichaC1[i];
            text2.color = colorTxt;

            vista.gameObject.GetComponent<RawImage>().color = colorBg;

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
    }

    public void presentarListaPreguntas()
    {
        LImpiar();
        for (int i = 0; i < preguntasList.Count; i++)
        {
            int valor = i + 1;

            vistaPre.transform.GetChild(0).GetComponent<TextMeshProUGUI>().text = valor.ToString() + ".";

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

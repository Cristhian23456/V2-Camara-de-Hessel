using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class IntroController : MonoBehaviour
{
   
    [SerializeField]
    [TextArea(5,6)]
    private List<string> listIntros = new List<string>();
    [SerializeField]
    [TextArea(3,4)]
    private string[] introCaso1,introCaso4;
    [SerializeField]
    private TextMeshProUGUI txtIntroduccion;
    [SerializeField]
    private GameObject panelIntroduccion;
    [SerializeField]
    private ApiManager api; // Referencia al ApiManager
    [SerializeField]
    private DialogosManager dialogosManager; // Referencia al DialogoManager
    [SerializeField]
    private GameObject btnAceptar;
    [SerializeField]
    private GameObject panelAnimaciones,camaraAnima;
    [SerializeField]
    private PresentarInfoSalas infoSala;
    [SerializeField]
    private AudioClip audioBienvenidaGeneral,audioIntroC1,audioIntroC2;
    [SerializeField]
    private AudioSource audioSource;

    void Start()
    {
        Debug.Log("Vamos a inicializar el intro controller");
         btnAceptar.SetActive(false);
        if (api != null)
        {
         Debug.Log("Obtenermos el NumeroAleatorioGeneradoEvent.");
            OnNumeroAleatorioGenerado(api.getNroCaso());
        }
        else
        {
            Debug.LogError("ApiManager no asignado en el Inspector.");
        }
    }

 

    private void OnNumeroAleatorioGenerado(int numeroAleatorio)
    {
        panelIntroduccion.SetActive(true);
        Debug.Log("Número aleatorio recibido en IntroController: " + numeroAleatorio);

        if (numeroAleatorio == 1)
        {
            StartCoroutine(escribirIntro(introCaso1[0]));
            audioSource.clip = audioBienvenidaGeneral;
            audioSource.Play();
        }
        else if (numeroAleatorio == 4)
        {
            audioSource.clip = audioBienvenidaGeneral;
            audioSource.Play();
            StartCoroutine(escribirIntro(introCaso4[0]));
        }
      
        btnAceptar.GetComponent<Button>().onClick.RemoveAllListeners();
        btnAceptar.GetComponent<Button>().onClick.AddListener(() =>
        {
            cambiarDialgo(numeroAleatorio);


        });
    }

    IEnumerator escribirIntro(string texto)
    {
        txtIntroduccion.maxVisibleCharacters = 0;
        txtIntroduccion.text = texto;
        txtIntroduccion.richText = true;
        for (int i = 0; i < texto.ToCharArray().Length; i++)
        {
            txtIntroduccion.maxVisibleCharacters++;
            yield return new WaitForSeconds(15f / 500);
        }
        btnAceptar.SetActive(true);
      
    }

    public void cambiarDialgo(int numeroAleatorio)
    {
        btnAceptar.gameObject.SetActive(false);
        if (numeroAleatorio == 1)
        {
            audioSource.clip = audioIntroC1;
            audioSource.Play();
            StartCoroutine(escribirIntro(introCaso1[1]));
        }
        else if (numeroAleatorio == 4)
        {
            audioSource.clip = audioIntroC2;
            audioSource.Play();
            StartCoroutine(escribirIntro(introCaso4[1]));
        }
        btnAceptar.GetComponent<Button>().onClick.RemoveAllListeners();
        btnAceptar.GetComponent<Button>().onClick.AddListener(() =>
        {
          //  dialogosManager.darFuncionBtnAceptar();
            panelIntroduccion.SetActive(false);
            StartCoroutine(infoSala.ejecutarAnimacion());

        });
    }
}

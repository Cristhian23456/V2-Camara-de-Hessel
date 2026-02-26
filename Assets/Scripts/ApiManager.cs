using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System;

public class ApiManager : MonoBehaviour
{
    [SerializeField]
    private string apiUrl = "https://api-labpsicologia.onrender.com/api";
    [SerializeField]
    private TextMeshProUGUI txtDatos;
    public List<Dialogos> dialogosList = new List<Dialogos>();
    public List<Dialogos> dialogosListDes = new List<Dialogos>();
    public List<Dialogos> dialogosListFin = new List<Dialogos>();
    public int nroCaso;
    [SerializeField]
    private GameObject [] personajesCasos;
    

    // Evento para indicar que los di�logos se han cargado fase inicial
    public event Action<List<Dialogos>> DialogosCargadosEvent;
    // Evento para indicar que los di�logos se han cargado fase desarrollo
    public event Action<List<Dialogos>> DialogosCargadosDesarrolladoEvent;
    // Evento para indicar que los di�logos se han cargado
    public event Action<List<Dialogos>> DialogosCargadosFinalEvent;
   

    void Start()
    {
        //Consumimos la api mediante corrutinas por proceso asincrono
     
            // Generar un n�mero aleatorio entre 1 y 2 o 4,5
            nroCaso = new System.Random().Next(2) == 0 ? 1:4;
            //nroCaso=4;
           // nroCaso = UnityEngine.Random.Range(4, 5);
            Debug.Log("N�mero de caso generado: " + nroCaso);
        
        


        if (nroCaso == 1)
        {
            personajesCasos[0].SetActive(true);
            personajesCasos[1].SetActive(true);
            personajesCasos[2].SetActive(false);
            personajesCasos[3].SetActive(false);
        }
        else if (nroCaso == 4)
        {
            personajesCasos[0].SetActive(false);
            personajesCasos[1].SetActive(false);
            personajesCasos[2].SetActive(true);
            personajesCasos[3].SetActive(true);
        }

        StartCoroutine(GetDialogosFromApi(nroCaso, "Inicial", (dialogos) => {
            dialogosList = dialogos;
            Debug.Log("Di�logos Inicial cargados: " + dialogosList.Count);
            DialogosCargadosEvent?.Invoke(dialogosList);
        }));

       StartCoroutine(GetDialogosFromApi(nroCaso, "Desarrollo", (dialogos) => {
            dialogosListDes = dialogos;
            Debug.Log("Di�logos Desarrollo cargados: " + dialogosListDes.Count);
            DialogosCargadosDesarrolladoEvent?.Invoke(dialogosListDes);
        }));
        StartCoroutine(GetDialogosFromApi(nroCaso, "Final", (dialogos) => {
            dialogosListFin = dialogos;
            Debug.Log("Di�logos final cargados: " + dialogosListFin.Count);
            DialogosCargadosFinalEvent?.Invoke(dialogosListFin);
        }));
    }

    IEnumerator GetDialogosFromApi(int caso, string fase, Action<List<Dialogos>> callback)
    {
        string url = apiUrl + "/get-dialogos?caso=" + caso + "&fase=" + fase;
        Debug.Log(url);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al obtener los dialogos: " + request.error);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
               // Debug.Log("Dialogos obtenidos correctamente para fase " + fase + ": " + jsonData);

                // Deserializar el JSON en la clase contenedora
                DialogosContainer dialogosContainer = JsonUtility.FromJson<DialogosContainer>("{\"dialogos\":" + jsonData + "}");

                // Acceder a la lista de dialogos
                List<Dialogos> listDialogos = dialogosContainer.dialogos;

                // Procesar preguntas si existen
                foreach (Dialogos dialogo in listDialogos)
                {
                    if (dialogo.tienePregunta)
                    {
                        yield return StartCoroutine(GetPreguntaById(dialogo.preguntaId, dialogo));
                    }
                }

                // Callback para asignar la lista
                callback?.Invoke(listDialogos);
            }
        }
    }

    IEnumerator GetPreguntaById(string preguntaId, Dialogos dialogo)
    {
        string url = apiUrl + "/get-questionsId/" + preguntaId;
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al obtener la pregunta con ID " + preguntaId + ": " + request.error);
            }
            else
            {
                string preguntaData = request.downloadHandler.text;
                
                Debug.Log("Pregunta obtenida correctamente.");

                // Deserializamos el JSON en un objeto Preguntas
                Preguntas pregunta = JsonUtility.FromJson<Preguntas>(preguntaData);
                // Carga el audio desde Resources si se ha especificado un audioPath
                if (!string.IsNullOrEmpty(pregunta.srcAudio))
                {
                    string audioResourcePath = pregunta.srcAudio.Replace(".wav", "").Replace(".mp3", ""); // Remove extension if present
                    pregunta.audio = Resources.Load<AudioClip>(audioResourcePath);
                    if (pregunta.audio == null)
                    {
                        Debug.LogError("No se pudo cargar el audio en Resources: " + audioResourcePath);
                    }
                }
                // Cargar audios para las respuestas
                foreach (var respuesta in pregunta.respuestas)
                {
                    if (!string.IsNullOrEmpty(respuesta.srcAudio))
                    {
                        string respuestaAudioPath = respuesta.srcAudio.Replace(".wav", "").Replace(".mp3", "");
                        respuesta.audio = Resources.Load<AudioClip>(respuestaAudioPath);
                        if (respuesta.audio == null)
                        {
                            Debug.LogError("No se pudo cargar el audio de la respuesta en Resources: " + respuestaAudioPath);
                        }
                    }
                }


                dialogo.pregunta = pregunta;
            }
        }
    }
    public int getNroCaso()
    {
        return nroCaso;
    }
}

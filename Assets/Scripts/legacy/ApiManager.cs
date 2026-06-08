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

    [Header("Configuraciones de Casos Unificadas")]
    [SerializeField]
    private List<CaseSetupSO> casoSetups = new List<CaseSetupSO>();
    private Dictionary<int, CaseSetupSO> casoSetupsDict = new Dictionary<int, CaseSetupSO>();

    private void Awake()
    {
        InitializeCaseSetups();
    }

    private void InitializeCaseSetups()
    {
        casoSetupsDict.Clear();
        foreach (var setup in casoSetups)
        {
            if (setup != null && !casoSetupsDict.ContainsKey(setup.nroCaso))
            {
                casoSetupsDict.Add(setup.nroCaso, setup);
            }
        }
    }

    public CaseSetupSO GetCaseSetup(int caso)
    {
        if (casoSetupsDict.Count == 0) InitializeCaseSetups();
        if (casoSetupsDict.TryGetValue(caso, out CaseSetupSO setup))
        {
            return setup;
        }
        return null;
    }
    

    // Evento para indicar que los diálogos se han cargado fase inicial
    public event Action<List<Dialogos>> DialogosCargadosEvent;
    // Evento para indicar que los diálogos se han cargado fase desarrollo
    public event Action<List<Dialogos>> DialogosCargadosDesarrolladoEvent;
    // Evento para indicar que los diálogos se han cargado
    public event Action<List<Dialogos>> DialogosCargadosFinalEvent;
   
    private bool recursosCargados = false;

    void Start()
    {
        // Consumimos la api de manera diferida para acelerar la carga del juego
        nroCaso = new System.Random().Next(2) == 0 ? 1 : 4;
        Debug.Log("Número de caso generado (Diferido): " + nroCaso);

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
    }

    public void CargarRecursosCaso()
    {
        if (recursosCargados) return;
        recursosCargados = true;
        
        Debug.Log("Cargando dinámicamente recursos de diálogos desde la API para el caso: " + nroCaso);

        StartCoroutine(GetDialogosFromApi(nroCaso, "Inicial", (dialogos) => {
            dialogosList = dialogos;
            Debug.Log("Diálogos Inicial cargados: " + dialogosList.Count);
            DialogosCargadosEvent?.Invoke(dialogosList);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Desarrollo", (dialogos) => {
            dialogosListDes = dialogos;
            Debug.Log("Diálogos Desarrollo cargados: " + dialogosListDes.Count);
            DialogosCargadosDesarrolladoEvent?.Invoke(dialogosListDes);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Final", (dialogos) => {
            dialogosListFin = dialogos;
            Debug.Log("Diálogos final cargados: " + dialogosListFin.Count);
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
                
                // Los audios ya NO se cargan aquí al inicio de golpe.
                // Esto optimiza la carga inicial cargando los recursos "por partes" (bajo demanda) en DialogosManager.cs.

                dialogo.pregunta = pregunta;
            }
        }
    }
    public int getNroCaso()
    {
        return nroCaso;
    }
}

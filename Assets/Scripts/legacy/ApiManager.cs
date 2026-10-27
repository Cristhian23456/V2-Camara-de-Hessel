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
    

    // Evento para indicar que los di??logos se han cargado fase inicial
    public event Action<List<Dialogos>> DialogosCargadosEvent;
    // Evento para indicar que los di??logos se han cargado fase desarrollo
    public event Action<List<Dialogos>> DialogosCargadosDesarrolladoEvent;
    // Evento para indicar que los di??logos se han cargado
    public event Action<List<Dialogos>> DialogosCargadosFinalEvent;
    public event Action<string> OnLoadError;
   
    private bool recursosCargados = false;

    void Start()
    {
        // Consumimos la api de manera diferida para acelerar la carga del juego
        nroCaso = new System.Random().Next(2) == 0 ? 1 : 4;
        Debug.Log("N??mero de caso generado (Diferido): " + nroCaso);

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
        
        Debug.Log("Cargando din??micamente recursos de di??logos desde la API para el caso: " + nroCaso);

        StartCoroutine(GetDialogosFromApi(nroCaso, "Inicial", (dialogos) => {
            dialogosList = dialogos;
            Debug.Log("Di??logos Inicial cargados: " + dialogosList.Count);
            DialogosCargadosEvent?.Invoke(dialogosList);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Desarrollo", (dialogos) => {
            dialogosListDes = dialogos;
            Debug.Log("Di??logos Desarrollo cargados: " + dialogosListDes.Count);
            DialogosCargadosDesarrolladoEvent?.Invoke(dialogosListDes);
        }));

        StartCoroutine(GetDialogosFromApi(nroCaso, "Final", (dialogos) => {
            dialogosListFin = dialogos;
            Debug.Log("Di??logos final cargados: " + dialogosListFin.Count);
            DialogosCargadosFinalEvent?.Invoke(dialogosListFin);
        }));
    }

    IEnumerator GetDialogosFromApi(int caso, string fase, Action<List<Dialogos>> callback)
    {
        float loadStart = Time.realtimeSinceStartup;
        string url = apiUrl + "/get-dialogos?caso=" + caso + "&fase=" + fase;
        Debug.Log("[ApiManager][TIMING] Start loading dialogs phase " + fase + " at " + loadStart.ToString("F3") + "s");
        
        int maxRetries = 3;
        int retryCount = 0;
        float timeoutSeconds = 10f;
        
        while (retryCount < maxRetries)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string jsonData = request.downloadHandler.text;
                    Debug.Log("[ApiManager][TIMING] Dialogs loaded successfully for phase " + fase + " (case " + caso + ") in " + (Time.realtimeSinceStartup - loadStart).ToString("F3") + "s");

                    try
                    {
                        // Deserializar el JSON en la clase contenedora
                        DialogosContainer dialogosContainer = JsonUtility.FromJson<DialogosContainer>("{\"dialogos\":" + jsonData + "}");
                        if (dialogosContainer == null || dialogosContainer.dialogos == null)
                        {
                            Debug.LogError("[ApiManager] Failed to parse dialogs JSON for phase " + fase);
                            retryCount++;
                            yield return new WaitForSeconds(1f * retryCount);
                            continue;
                        }

                        // Acceder a la lista de dialogos
                        List<Dialogos> listDialogos = dialogosContainer.dialogos;

                        // Procesar preguntas si existen
                        foreach (Dialogos dialogo in listDialogos)
                        {
                            if (dialogo != null && dialogo.tienePregunta && !string.IsNullOrEmpty(dialogo.preguntaId))
                            {
                                yield return StartCoroutine(GetPreguntaById(dialogo.preguntaId, dialogo));
                            }
                        }

                        // Callback para asignar la lista
                        callback?.Invoke(listDialogos);
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ApiManager] Exception parsing dialogs for phase " + fase + ": " + ex.Message);
                        retryCount++;
                        yield return new WaitForSeconds(1f * retryCount);
                        continue;
                    }
                }
                else
                {
                    Debug.LogError("[ApiManager] Error loading dialogs for phase " + fase + " (attempt " + (retryCount + 1) + "/" + maxRetries + "): " + request.error + " after " + (Time.realtimeSinceStartup - loadStart).ToString("F3") + "s");
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        yield return new WaitForSeconds(1f * retryCount);
                    }
                }
            }
        }
        
        // All retries failed - do NOT set loaded flags silently; notify error
        string errDialog = "Failed to load dialogs for phase " + fase + " after " + maxRetries + " attempts.";
        Debug.LogError("[ApiManager] " + errDialog);
        OnLoadError?.Invoke(errDialog);
        // Do not invoke success callback on failure
    }

    IEnumerator GetPreguntaById(string preguntaId, Dialogos dialogo)
    {
        float qStart = Time.realtimeSinceStartup;
        string url = apiUrl + "/get-questionsId/" + preguntaId;
        Debug.Log("[ApiManager][TIMING] Start loading question " + preguntaId + " at " + qStart.ToString("F3") + "s");
        
        int maxRetries = 2;
        int retryCount = 0;
        float timeoutSeconds = 8f;
        
        while (retryCount < maxRetries)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    string preguntaData = request.downloadHandler.text;
                    Debug.Log("[ApiManager][TIMING] Question loaded successfully: " + preguntaId + " in " + (Time.realtimeSinceStartup - qStart).ToString("F3") + "s");

                    try
                    {
                        Preguntas pregunta = JsonUtility.FromJson<Preguntas>(preguntaData);
                        if (pregunta != null && dialogo != null)
                        {
                            dialogo.pregunta = pregunta;
                        }
                        yield break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[ApiManager] Exception parsing question " + preguntaId + ": " + ex.Message);
                        retryCount++;
                        yield return new WaitForSeconds(0.5f * retryCount);
                        continue;
                    }
                }
                else
                {
                    Debug.LogError("[ApiManager] Error loading question " + preguntaId + " (attempt " + (retryCount + 1) + "/" + maxRetries + "): " + request.error + " after " + (Time.realtimeSinceStartup - qStart).ToString("F3") + "s");
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        yield return new WaitForSeconds(0.5f * retryCount);
                    }
                }
            }
        }
        
        string errQ = "Failed to load question " + preguntaId + " after " + maxRetries + " attempts.";
        Debug.LogError("[ApiManager] " + errQ);
        OnLoadError?.Invoke(errQ);
    }
    public int getNroCaso()
    {
        return nroCaso;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;
using System;

public class LoadData : MonoBehaviour
{

    private string apiUrl = "https://api-labpsicologia.onrender.com/api/";
    public event Action<List<Historial>> OnHistorialListReady;  // Evento que se dispara cuando el historial está listo
    [SerializeField]
    public bool tieneHistorial;
    public string fase;

    public void getHistory(string username)
    {
        StopAllCoroutines();
        StartCoroutine(getHistoryForUser(username));
    }

    IEnumerator getHistoryForUser(string userName)
    {
        string url = apiUrl + "get-user-history/" + userName;
        Debug.Log(url);
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Error al obtener el historial: " + request.error);
            }
            else
            {
                string jsonData = request.downloadHandler.text;
                HistorialContainer historialContainer = JsonUtility.FromJson<HistorialContainer>("{\"historial\":" + jsonData + "}");
                List<Historial> listHistorial = historialContainer.historial;

              
                // Dispara el evento indicando que el historial está listo
                OnHistorialListReady?.Invoke(listHistorial);
            }
        }
    }
   
}

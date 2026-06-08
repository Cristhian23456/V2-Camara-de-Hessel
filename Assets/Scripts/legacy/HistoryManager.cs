using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HistoryManager : MonoBehaviour
{
    [SerializeField]
    private DataUsers userData;
    [SerializeField]
    private LoadData loadData;
    [SerializeField]
    private Button btnHistorial;
    [Header("Interfaz de historial")]
    [SerializeField]
    private GameObject panelLoading;
    private List<Transform> listItems = new List<Transform>();
    [SerializeField]
    private Transform container_historial;
    [SerializeField]
    private Image imgPrefab;
    [SerializeField]
    private Button btnCargarPartida;
    [SerializeField]
    private SceneChange sceneChange;
 

    void Start()
    {
        btnCargarPartida.interactable = false;
        // Suscribirse al evento
        loadData.OnHistorialListReady += HandleHistorialListReady;
    }

    void OnDestroy()
    {
        // Desuscribirse del evento
        loadData.OnHistorialListReady -= HandleHistorialListReady;
    }

    public void funBtnHistorial()
    {
        loadData.getHistory(userData.username);
        panelLoading.SetActive(true);
    }

    private void HandleHistorialListReady(List<Historial> historialList)
    {
        // Manejar la lógica cuando la lista de historiales esté lista
        Debug.Log("Historial listo con " + historialList.Count + " elementos.");
        panelLoading.SetActive(false);
        CargarHistorial(historialList.Count, historialList);
        
        // Aquí puedes realizar cualquier acción que necesites con la lista de historiales
    }

    public void CargarHistorial(int cantidad, List<Historial> listHistorial)
    {

        if (listItems.Count >= cantidad)
        {
            for (int i = 0; i < listItems.Count; i++)
            {
                if (i < cantidad)
                {
                    string fecha = ConvertirFecha(listHistorial[i].fecha);
                    listItems[i].GetChild(0).GetComponent<TextMeshProUGUI>().text = fecha;
                    listItems[i].GetChild(1).GetComponent<TextMeshProUGUI>().text = "Simulador";
                    listItems[i].GetChild(2).GetComponent<TextMeshProUGUI>().text = "Fase " + listHistorial[i].fase;
                    listItems[i].GetChild(3).GetComponent<TextMeshProUGUI>().text = listHistorial[i].progreso;
                    Historial historial = listHistorial[i];
                    listItems[i].GetComponent<Button>().onClick.AddListener(()=>
                    {
                        DarFuncionItemHistorial(historial);
                    });
                    // listItems[i].GetComponentInChildren<TextMeshProUGUI>().text = listHistorial[i].fase;

                    listItems[i].gameObject.SetActive(true);

                }
                else
                {
                    listItems[i].gameObject.SetActive(false);
                }
            }
        }
        else
        {
            int cantidadRestante = (cantidad - listItems.Count);
            for (int i = 0; i < cantidadRestante; i++)
            {
                var newButton = Instantiate(imgPrefab, container_historial).GetComponent<Transform>();
                newButton.gameObject.SetActive(true);
                listItems.Add(newButton);
            }
            CargarHistorial(cantidad, listHistorial);

        }
    }
    private string ConvertirFecha(string fechaHora)
    {


        string[] partes = fechaHora.Split('T');
        string fecha = partes[0];

        return fecha;

    }
    private void DarFuncionItemHistorial(Historial history)
    {
        print("Selecione un historial");
        
        loadData.fase=  history.fase;
        loadData.tieneHistorial = true;
        btnCargarPartida.interactable = true;
    }


    public void cargarPartida()
    {
        btnCargarPartida.interactable = false;
        sceneChange.changeScena("SampleScene");
    }
}

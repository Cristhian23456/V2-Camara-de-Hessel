using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ManagerData : MonoBehaviour
{
    [SerializeField]
    private LoadData loadData;
    private  bool isHistory;
    private string fase;
    [SerializeField]
    private DialogosManager dialogosManager;
    [Header("Desactivar si tiene historial")]
    [SerializeField]
    private ControladorCinematica controladorCinematica;
    [SerializeField] private GameObject cameraCinematica;
    [SerializeField] private GameObject player, mainCamera;
    [SerializeField] private ApiManager apiManager;
    [SerializeField] private GameObject btnContinuar;
   
    [Header("Guardar informacion")]
    [SerializeField]
    private SaveData saveData;


    // Start is called before the first frame update
    void Start()
    {
        saveData = GameObject.Find("LoginController").GetComponent<SaveData>();
        loadData = GameObject.Find("LoginController").GetComponent<LoadData>();
        if (loadData != null)
        {
            isHistory = loadData.tieneHistorial;
            if (isHistory)
            {
                controladorCinematica.enabled = false;
                cameraCinematica.SetActive(false);
                player.SetActive(false);
                mainCamera.SetActive(true);
                btnContinuar.GetComponent<Button>().onClick.AddListener(()=>continuarFase());
            }
        }


    }

    
    // Update is called once per frame
    void Update()
    {
        
    }
    
    public void continuarFase()
    {
        switch (loadData.fase)
        {
            case "Inicial":
                dialogosManager.iniciarFase("Desarrollo");
                dialogosManager.darFuncionBtnAceptar();
                break;
            case "Desarrollo":
                dialogosManager.iniciarFase("Final");
                dialogosManager.darFuncionBtnAceptar();
                break;
        }
    }
}

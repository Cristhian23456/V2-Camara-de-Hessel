using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChange : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private SaveData saveData;
    [SerializeField]
    private TMP_InputField txtcorreo;
    [SerializeField]
    private TMP_InputField txtcontrasenia;
    void Start()
    {
        saveData = GameObject.Find("LoginController").GetComponent<SaveData>();

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void changeScena(string nombre)
    {
        SceneManager.LoadScene(nombre);
    }
    public void saveModo(string modo)
    {

        if (saveData != null)
        {
            saveData.modo = modo;
        }
    }
    public void saveCaso(int caso)
    {

        if (saveData != null)
        {
            saveData.caso = caso;
        }
    }

    public void LimpiarCampos()
    {
        txtcorreo.text = "";
        txtcontrasenia.text = "";
    }
}
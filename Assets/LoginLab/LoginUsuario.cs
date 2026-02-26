using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
using TMPro;
using System.Collections.Generic;
using MS2S.VirtopsiaVR;


public class LoginUsuario : MonoBehaviour
{
    public TMP_InputField IF_Email_Fb;
    public TMP_InputField IF_Password_Fb;
    public TextMeshProUGUI outputText;
    public Button btnLogin;
    public static string User_Email;
    public static string User_Password;
    public GameObject Panel_Opcion_Escena;
    public GameObject Panel_Espera;
    public GameObject Panel_Datos_Incorrectos;
    public WebService webService;
    private String UriUtpl = "https://campus3d.utpl.edu.ec/virtopsia-admin/api/authentication";

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return)) {
            IniciarSesion();
        }
    }

    public void IniciarSesion()
    {
        if (webService == null)
        {
            Debug.LogError("WebService no asignado en LoginUsuario (Inspector).");
            return;
        }

        Panel_Datos_Incorrectos.SetActive(false);
        Panel_Opcion_Escena.SetActive(false);

        Panel_Espera.SetActive(true);

        string user = IF_Email_Fb.text.ToLower();
        string pass = IF_Password_Fb.text;

        webService.Login(user, pass, false);
    }
    public void WebServiceCallback(bool success)
    {
        Panel_Espera.SetActive(false);

        if (success)
        {
            string username = PlayerPrefs.GetString(Storage.userNamePrefKey, "");
            string nombre = PlayerPrefs.GetString(Storage.userFirstNamePrefKey, username);

            if (DataUsers.Instance != null)
            {
                string email = username + "@utpl.edu.ec";
                DataUsers.Instance.SetUsersData(nombre, username, email);
            }

            // carga directamente la escena
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            Panel_Datos_Incorrectos.SetActive(true);
        }
    }


    public void Cerrar_Panel_Datos_Incorrectos () {
        Panel_Datos_Incorrectos.SetActive(false);
        Panel_Espera.SetActive(false);
        btnLogin.GetComponent<Button>().interactable = true;
    }

    public void Timer_Loading()
    {
        gameObject.GetComponent<SaveData>().writeNewUser(DataUsers.Instance.nombre, DataUsers.Instance.email, DataUsers.Instance.username, System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"));
    }


    public void CargarNivel(String SceneName)
    {
        StartCoroutine(CargarAsync(SceneName));

    }

    public Slider Slider;

    IEnumerator CargarAsync(String SceneName)
    {
        AsyncOperation Operación = SceneManager.LoadSceneAsync(SceneName);
        Operación.allowSceneActivation = false;
        while (!Operación.isDone)
        {
            float Progreso = Mathf.Clamp01(Operación.progress / .9f);
            Debug.Log(Progreso);
            Slider.value = Progreso;
            yield return null;
        }
    }


}

[Serializable]
public class UserUtpl
{
    public string username;
    public string password;

    public UserUtpl(string username, string password)
    {
        this.username = username;
        this.password = password;
    }
}

[Serializable]
public class NewUserUtpl
{
    public string username {get; set;}
    public string fullName {get; set;}
    
}
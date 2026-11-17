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
    [Tooltip("Texto opcional dentro de Panel_Datos_Incorrectos para mostrar el motivo del error.")]
    public TextMeshProUGUI txtMensajeError;
    public WebService webService;
    private String UriUtpl = "https://campus3d.utpl.edu.ec/virtopsia-admin/api/authentication";

    private const string DominioUtpl = "@utpl.edu.ec";
    private bool enviando;
    private bool cargandoEscena;

    void Update()
    {
        // Enter solo envía si no hay un envío en curso ni un panel de error abierto
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            && !enviando && !cargandoEscena
            && (Panel_Datos_Incorrectos == null || !Panel_Datos_Incorrectos.activeSelf))
        {
            IniciarSesion();
        }
    }

    public void IniciarSesion()
    {
        if (enviando || cargandoEscena) return; // evita envíos dobles (Enter + botón)

        if (webService == null)
        {
            Debug.LogError("WebService no asignado en LoginUsuario (Inspector).");
            MostrarError("No se pudo iniciar sesión: falta configurar el servicio de autenticación.");
            return;
        }

        if (Panel_Datos_Incorrectos != null) Panel_Datos_Incorrectos.SetActive(false);
        if (Panel_Opcion_Escena != null) Panel_Opcion_Escena.SetActive(false);
        MostrarMensaje("");

        string user = NormalizarUsuario(IF_Email_Fb != null ? IF_Email_Fb.text : "");
        string pass = IF_Password_Fb != null ? IF_Password_Fb.text : "";

        // Validaciones locales antes de llamar al servidor
        if (string.IsNullOrEmpty(user) && string.IsNullOrEmpty(pass))
        {
            MostrarError("Ingresa tu usuario y tu contraseña.");
            return;
        }
        if (string.IsNullOrEmpty(user))
        {
            MostrarError("Ingresa tu usuario.");
            return;
        }
        if (string.IsNullOrEmpty(pass))
        {
            MostrarError("Ingresa tu contraseña.");
            return;
        }
        if (user.Contains("@") || user.Contains(" "))
        {
            MostrarError("Usa tu usuario institucional (por ejemplo: usuario o usuario" + DominioUtpl + ").");
            return;
        }

        enviando = true;
        if (btnLogin != null) btnLogin.interactable = false;
        if (Panel_Espera != null) Panel_Espera.SetActive(true);

        webService.Login(user, pass, false);
    }

    /// <summary>Acepta "usuario" o "usuario@utpl.edu.ec" y devuelve solo el usuario en minúsculas.</summary>
    public static string NormalizarUsuario(string entrada)
    {
        if (string.IsNullOrWhiteSpace(entrada)) return "";
        string user = entrada.Trim().ToLowerInvariant();
        if (user.EndsWith(DominioUtpl))
            user = user.Substring(0, user.Length - DominioUtpl.Length);
        return user;
    }

    public void WebServiceCallback(LoginResultado resultado)
    {
        enviando = false;
        if (Panel_Espera != null) Panel_Espera.SetActive(false);

        if (resultado == LoginResultado.Ok)
        {
            string username = PlayerPrefs.GetString(Storage.userNamePrefKey, "");
            string nombre = PlayerPrefs.GetString(Storage.userFirstNamePrefKey, username);

            if (DataUsers.Instance != null)
            {
                string email = username + DominioUtpl;
                DataUsers.Instance.SetUsersData(nombre, username, email);
            }

            // carga directamente la escena
            cargandoEscena = true;
            SceneManager.LoadScene("SampleScene");
        }
        else
        {
            if (btnLogin != null) btnLogin.interactable = true;
            if (resultado == LoginResultado.CredencialesIncorrectas && IF_Password_Fb != null)
                IF_Password_Fb.text = "";
            MostrarError(WebService.MensajePara(resultado));
        }
    }

    // Compatibilidad con llamadas anteriores
    public void WebServiceCallback(bool success)
    {
        WebServiceCallback(success ? LoginResultado.Ok : LoginResultado.CredencialesIncorrectas);
    }

    private void MostrarError(string mensaje)
    {
        MostrarMensaje(mensaje);
        if (Panel_Datos_Incorrectos != null) Panel_Datos_Incorrectos.SetActive(true);
    }

    private void MostrarMensaje(string mensaje)
    {
        if (txtMensajeError != null) txtMensajeError.text = mensaje;
        if (outputText != null) outputText.text = mensaje;
        if (!string.IsNullOrEmpty(mensaje)) Debug.Log("[Login] " + mensaje);
    }

    public void Cerrar_Panel_Datos_Incorrectos () {
        if (Panel_Datos_Incorrectos != null) Panel_Datos_Incorrectos.SetActive(false);
        if (Panel_Espera != null) Panel_Espera.SetActive(false);
        if (btnLogin != null) btnLogin.interactable = true;
        enviando = false;
    }

    public void Timer_Loading()
    {
        var saveData = GetComponent<SaveData>();
        if (saveData == null || DataUsers.Instance == null)
        {
            Debug.LogWarning("[Login] Timer_Loading: falta SaveData o DataUsers; no se registra el usuario.");
            return;
        }
        saveData.writeNewUser(DataUsers.Instance.nombre, DataUsers.Instance.email, DataUsers.Instance.username, System.DateTime.Now.ToString("HH:mm:ss; dd MMMM yyyy"));
    }


    public void CargarNivel(String SceneName)
    {
        if (cargandoEscena) return;
        cargandoEscena = true;
        StartCoroutine(CargarAsync(SceneName));
    }

    public Slider Slider;

    IEnumerator CargarAsync(String SceneName)
    {
        AsyncOperation operacion = SceneManager.LoadSceneAsync(SceneName);
        if (operacion == null)
        {
            Debug.LogError("[Login] No se pudo cargar la escena: " + SceneName);
            cargandoEscena = false;
            yield break;
        }
        operacion.allowSceneActivation = false;
        // Con allowSceneActivation=false el progreso se detiene en 0.9 y isDone nunca llega a true,
        // por eso hay que activar la escena al llegar a 0.9.
        while (!operacion.isDone)
        {
            float progreso = Mathf.Clamp01(operacion.progress / .9f);
            if (Slider != null) Slider.value = progreso;
            if (operacion.progress >= 0.9f)
                operacion.allowSceneActivation = true;
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
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MS2S.VirtopsiaVR
{
    /// <summary>Resultado de un intento de inicio de sesión.</summary>
    public enum LoginResultado
    {
        Ok,
        CredencialesIncorrectas,
        SinConexion,
        ErrorServidor
    }

    public class WebService : MonoBehaviour
    {
        #region Public const

        public const string DEFAULT_WAN_SERVER_INSTANCE_URL = "https://campus3d.utpl.edu.ec/virtopsia-admin";
        public const string WS_LOGIN = "/api/authentication";
        // Segundos máximos de espera; evita quedarse trabado en la pantalla de espera.
        public const int REQUEST_TIMEOUT_SECONDS = 15;

        #endregion

        #region Private Serializable Fields

        [SerializeField]
        private Text feedbackText;

        [SerializeField]
        private GameObject callbackObject;

        #endregion

        private bool enCurso;

        #region Public Methods

        public void Login(string userName, string password, bool rememberMe)
        {
            if (enCurso) return; // evita envíos dobles
            enCurso = true;

            User user = new User();
            user.username = userName;
            user.password = password;

            string json = JsonUtility.ToJson(user);

            StartCoroutine(PostRequest(DEFAULT_WAN_SERVER_INSTANCE_URL + WS_LOGIN, json, (resultado, data) =>
            {
                enCurso = false;
                LoginResultado final = resultado;
                if (resultado == LoginResultado.Ok)
                {
                    final = GuardarDatosUsuario(userName, password, rememberMe, data);
                }

                LogFeedback(MensajePara(final));
                LoginUsuario callbackScene = ObtenerCallback();
                if (callbackScene != null)
                    callbackScene.WebServiceCallback(final);
                else
                    Debug.LogError("[WebService] No se encontró LoginUsuario para notificar el resultado del login.");
            }));
        }

        public static string MensajePara(LoginResultado resultado)
        {
            switch (resultado)
            {
                case LoginResultado.Ok: return "";
                case LoginResultado.CredencialesIncorrectas: return "Usuario o contraseña incorrectos.";
                case LoginResultado.SinConexion: return "No hay conexión con el servidor. Revisa tu conexión a internet e inténtalo de nuevo.";
                default: return "El servidor no pudo procesar el inicio de sesión. Inténtalo más tarde.";
            }
        }

        #endregion

        #region Private Methods

        private LoginUsuario ObtenerCallback()
        {
            if (callbackObject != null)
            {
                var login = callbackObject.GetComponent<LoginUsuario>();
                if (login != null) return login;
            }
            return FindAnyObjectByType<LoginUsuario>();
        }

        private LoginResultado GuardarDatosUsuario(string userName, string password, bool rememberMe, byte[] data)
        {
            UserData userData;
            try
            {
                string responseString = System.Text.Encoding.UTF8.GetString(data);
                userData = JsonUtility.FromJson<UserData>(responseString);
            }
            catch (Exception e)
            {
                Debug.LogError("[WebService] Respuesta de login inválida: " + e.Message);
                return LoginResultado.ErrorServidor;
            }

            if (userData == null)
            {
                Debug.LogError("[WebService] Respuesta de login vacía.");
                return LoginResultado.ErrorServidor;
            }

            // Datos básicos
            PlayerPrefs.SetString(Storage.userNamePrefKey, userName);
            PlayerPrefs.SetString(Storage.passwordPrefKey, password);
            PlayerPrefs.SetInt(Storage.rememberMePrefKey, rememberMe ? 1 : 0);

            // Primer nombre y primer apellido (tolerante a campos vacíos)
            string primerNombre = PrimeraPalabra(userData.firstName);
            string primerApellido = PrimeraPalabra(userData.lastName);
            string nombreCompleto = (primerNombre + " " + primerApellido).Trim();
            if (string.IsNullOrEmpty(nombreCompleto)) nombreCompleto = userName;

            PlayerPrefs.SetString(Storage.userFirstNamePrefKey, nombreCompleto);
            PlayerPrefs.SetString(Storage.userRolePrefKey, userData.role != null ? userData.role.code : "");
            PlayerPrefs.SetString(Storage.userAvatarRolePrefKey, ".");
            PlayerPrefs.SetString(Storage.userGenerePrefKey, userData.genere ?? "");
            PlayerPrefs.SetString(Storage.userIdentificationPrefKey, userData.identification ?? "");

            // Permisos
            string permissions = "";
            if (userData.permissions != null)
            {
                for (int i = 0; i < userData.permissions.Length; i++)
                {
                    if (userData.permissions[i] == null) continue;
                    if (permissions.Length > 0) permissions += ",";
                    permissions += userData.permissions[i].code;
                }
            }
            PlayerPrefs.SetString(Storage.userPermissionsPrefKey, permissions);
            PlayerPrefs.Save();

            return LoginResultado.Ok;
        }

        private static string PrimeraPalabra(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return "";
            return texto.Trim().Split(' ')[0];
        }

        #endregion

        #region Web Requests

        IEnumerator PostRequest(string url, string json, Action<LoginResultado, byte[]> result)
        {
            LogFeedback("Esperando...");

            using (var uwr = new UnityWebRequest(url, "POST"))
            {
                byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
                uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = REQUEST_TIMEOUT_SECONDS;
                uwr.SetRequestHeader("Content-Type", "application/json");
                uwr.SetRequestHeader("Authorization", "dKp9FfIjJL85AfuS8aZzHYUxlQw09AHW6EoiE4o7sZds3qFVuwpCxXFegA6AxGZ");
                uwr.SetRequestHeader("Accept", "application/json");

                Debug.Log("POST: " + uwr.url);

                yield return uwr.SendWebRequest();

                switch (uwr.result)
                {
                    case UnityWebRequest.Result.Success:
                        string body = uwr.downloadHandler.text;
                        if (string.IsNullOrEmpty(body))
                            result(LoginResultado.ErrorServidor, null);
                        else if (body.Contains("errorMessage"))
                            result(LoginResultado.CredencialesIncorrectas, null);
                        else
                            result(LoginResultado.Ok, uwr.downloadHandler.data);
                        break;

                    case UnityWebRequest.Result.ConnectionError:
                        Debug.LogWarning("[WebService] Error de conexión: " + uwr.error);
                        result(LoginResultado.SinConexion, null);
                        break;

                    case UnityWebRequest.Result.ProtocolError:
                        Debug.LogWarning($"[WebService] HTTP {uwr.responseCode}: {uwr.error}");
                        bool credenciales = uwr.responseCode == 400 || uwr.responseCode == 401 || uwr.responseCode == 403
                            || (uwr.downloadHandler.text ?? "").Contains("errorMessage");
                        result(credenciales ? LoginResultado.CredencialesIncorrectas : LoginResultado.ErrorServidor, null);
                        break;

                    default:
                        Debug.LogWarning("[WebService] Error procesando la respuesta: " + uwr.error);
                        result(LoginResultado.ErrorServidor, null);
                        break;
                }
            }
        }

        void LogFeedback(string message)
        {
            if (feedbackText != null)
                feedbackText.text = message;
        }

        #endregion
    }
}

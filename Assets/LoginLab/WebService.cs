using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MS2S.VirtopsiaVR
{
    public class WebService : MonoBehaviour
    {
        #region Public const

        public const string DEFAULT_WAN_SERVER_INSTANCE_URL = "https://campus3d.utpl.edu.ec/virtopsia-admin";
        public const string WS_LOGIN = "/api/authentication";

        #endregion

        #region Private Serializable Fields

        [SerializeField]
        private Text feedbackText;

        [SerializeField]
        private GameObject callbackObject;

        #endregion

        #region MonoBehaviour CallBacks

        void Start()
        {
        }

        #endregion

        #region Public Methods

        public void Login(string userName, string password, bool rememberMe)
        {
            User user = new User();
            user.username = userName;
            user.password = password;

            string json = JsonUtility.ToJson(user);

            StartCoroutine(PostRequest(DEFAULT_WAN_SERVER_INSTANCE_URL + WS_LOGIN, json, (returnValue) =>
            {
                LoginUsuario callbackScene = callbackObject.GetComponent<LoginUsuario>();

                if (returnValue != null)
                {
                    // Guardar datos básicos
                    PlayerPrefs.SetString(Storage.userNamePrefKey, userName);
                    PlayerPrefs.SetString(Storage.passwordPrefKey, password);
                    PlayerPrefs.SetInt(Storage.rememberMePrefKey, rememberMe ? 1 : 0);

                    // Leer JSON de respuesta
                    string responseString = System.Text.Encoding.UTF8.GetString(returnValue);
                    Debug.Log("Login Response: " + responseString);

                    UserData userData = JsonUtility.FromJson<UserData>(responseString);

                    // Primer nombre y apellido
                    string primerNombre = userData.firstName.Split(' ')[0];
                    string primerApellido = userData.lastName.Split(' ')[0];
                    string nombreCompleto = primerNombre + " " + primerApellido;

                    // Guardar en PlayerPrefs
                    PlayerPrefs.SetString(Storage.userFirstNamePrefKey, nombreCompleto);
                    PlayerPrefs.SetString(Storage.userRolePrefKey, userData.role.code);
                    PlayerPrefs.SetString(Storage.userAvatarRolePrefKey, ".");
                    PlayerPrefs.SetString(Storage.userGenerePrefKey, userData.genere);
                    PlayerPrefs.SetString(Storage.userIdentificationPrefKey, userData.identification);

                    // Guardar permisos
                    string permissions = "";
                    for (int i = 0; i < userData.permissions.Length; i++)
                    {
                        permissions += userData.permissions[i].code;
                        if (i < userData.permissions.Length - 1)
                            permissions += ",";
                    }
                    PlayerPrefs.SetString(Storage.userPermissionsPrefKey, permissions);

                    // Callback OK
                    callbackScene.WebServiceCallback(true);
                }
                else
                {
                    callbackScene.WebServiceCallback(false);
                }
            }));
        }

        #endregion

        #region Web Requests

        IEnumerator PostRequest(string url, string json, System.Action<byte[]> result)
        {
            LogFeedback("Esperando...");

            var uwr = new UnityWebRequest(url, "POST");
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            uwr.uploadHandler = new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");
            uwr.SetRequestHeader("Authorization", "dKp9FfIjJL85AfuS8aZzHYUxlQw09AHW6EoiE4o7sZds3qFVuwpCxXFegA6AxGZ");
            uwr.SetRequestHeader("Accept", "application/json");

            Debug.Log("POST: " + uwr.url);

            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                if (uwr.downloadHandler.text.Contains("errorMessage"))
                {
                    LogFeedback("Usuario o contraseña incorrectos");
                    result(null);
                }
                else
                {
                    result(uwr.downloadHandler.data);
                }
            }
            else
            {
                LogFeedback("Error conectando al servidor...");
                result(null);
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

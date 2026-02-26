using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DataUsers : MonoBehaviour
{
    public static DataUsers Instance;
    public string nombre;
    public string username;
    public string email;

    void Awake()
    {
        if (DataUsers.Instance == null) {
            DataUsers.Instance = this;
            DontDestroyOnLoad(this.gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void SetUsersData (string nombreGet, string usernameGet, string emailGet) {
        nombre = nombreGet;
        
        username = usernameGet;
        email = emailGet;
    }
}

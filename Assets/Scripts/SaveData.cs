using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class SaveData : MonoBehaviour
{
    private string url_api = "https://api-labpsicologia.onrender.com/api/";
    [SerializeField]
    private string username;
    [SerializeField]
    private string idPartida;
    [SerializeField]
    public string fechaIncio;
    [SerializeField]
    public string modo;
    public int caso;
    public void writeNewUser(string name, string email, string username, string date)
    {
        User user = new User(name, email, username, date);
        print(user.username);
        print(user.nombre_completo);

    }
    public void updatePartidaUser(string faseCasoEstudio, string fechaModificacion, string partidaCaso)
    {
        Partida avancePartida = new Partida(faseCasoEstudio, fechaModificacion, partidaCaso);
        print(avancePartida.faseCasoEstudio);
        print(avancePartida.partidaCasoUsuario);

    }
    public void updateUserIntentEntry(string fecha, string progreso, double puntaje)
    {
        Intento intentoPartida = new Intento(fechaIncio, progreso, puntaje);
        print(intentoPartida.progreso);
        print(intentoPartida.puntaje);
    }
}


class User
{
    public string nombre_completo;
    public string email;
    public string username;
    public string date;

    public User(string nombre_completo, string email, string username, string date)
    {
        this.nombre_completo = nombre_completo;
        this.email = email;
        this.username = username;
        this.date = date;
    }
}
[System.Serializable]
public class Response
{
    public string message;
    public string id;
}

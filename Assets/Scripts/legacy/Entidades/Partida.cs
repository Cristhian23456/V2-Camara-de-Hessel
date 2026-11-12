using System.Collections;
using UnityEngine;
using System;
[Serializable]
public class Partida 
{
    public string faseCasoEstudio;
    public string fechaModificacion;
    public string partidaCasoUsuario;
    public Partida(string fase, string fechaModificacion, string partidaCaso)
    {
        this.faseCasoEstudio = fase;
        this.fechaModificacion = fechaModificacion;
        this.partidaCasoUsuario = partidaCaso;
    }
}

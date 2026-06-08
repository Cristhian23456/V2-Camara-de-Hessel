using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class Intento 
{
    public string fecha_hora_inicio;
    public string progreso;
    public double puntaje;
    public Intento(string fecha_hora_inicio, string progreso, double puntaje)
    {
        this.fecha_hora_inicio = fecha_hora_inicio;
        this.progreso = progreso;
        this.puntaje = puntaje;
    }
}

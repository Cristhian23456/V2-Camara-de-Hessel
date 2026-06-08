using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[Serializable]
public class Historial 
{
    public string fecha;
    public string modo="Simulador";
    public string fase;
    public string progreso;

    public Historial(string fecha, string fase, string progreso)
    {
        this.fecha = fecha;
        this.modo = "Simulador";
        this.fase = fase;
        this.progreso = progreso;
    }
}

[Serializable]
public class HistorialContainer
{
    public List<Historial> historial;
}
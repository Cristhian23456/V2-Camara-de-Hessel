using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class Dialogos
{
    public int orden;
    public string contenido;
    public bool tienePregunta;
    public string preguntaId;
    public string personaje;
    public bool esImportante;
    public Preguntas pregunta=null;
    public int caso;

    // Constructor que recibe parámetros para inicializar el obejto dialogo
    public Dialogos(int orden, string contenido, bool tienePregunta, string preguntaId, string personaje, int caso, Preguntas pregunta, bool esImportante)
    {
        this.orden = orden;
        this.contenido = contenido;
        this.tienePregunta = tienePregunta;
        this.preguntaId = preguntaId;
        this.personaje = personaje;
        this.caso = caso;
        this.pregunta = null;
        this.esImportante = esImportante;
    }
}

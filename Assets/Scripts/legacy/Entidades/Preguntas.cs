using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public class Preguntas
{
    public int calificacion;
    public string id;
    public bool tieneDialogo;
    public string personajePregunta;
    public string pregunta;
    public AudioClip audio;
    public string srcAudio; // Ruta del archivo de audio relativa a Resources
    public Respuestas[] respuestas;
    public Preguntas(int calificacion, bool tieneDialogo, string personajePregunta, string pregunta, Respuestas[] respuestas,string srcAudio,AudioClip audio, string id)
    {
        this.id = id;
        this.calificacion = calificacion;
        this.tieneDialogo = tieneDialogo;
        this.personajePregunta = personajePregunta;
        this.pregunta = pregunta;
        this.respuestas = respuestas;
        this.srcAudio = srcAudio;
        this.audio = audio;
    }
}


[Serializable]
public class Respuestas
{
    public string retroalimentacion;
    public bool esCorrecta;
    public string respuesta;
    public AudioClip audio;
    public string srcAudio; // Ruta del archivo de audio relativa a Resources

    public Respuestas(string retroalimentacion,bool esCorrecta,string respuesta, AudioClip audio, string srcAudio)
    {
        this.retroalimentacion = retroalimentacion;
         this.esCorrecta = esCorrecta;
        this.respuesta = respuesta;
        this.audio = audio;
        this.srcAudio = srcAudio;
    }
}

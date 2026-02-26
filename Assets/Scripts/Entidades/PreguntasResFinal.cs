using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PreguntasResFinal
{
    public string pregunta;
    public string respuesta;
    public string respuestaCorrecta;
    public string retroalimentacion;

    public PreguntasResFinal(string p, string r, string res)
    {
        this.pregunta = p;
        this.respuesta = r;
        this.retroalimentacion = res;
    }
    public PreguntasResFinal(string p, string r, string rCorrecta, string retro)
    {
        this.pregunta = p;
        this.respuesta = r;
        this.respuestaCorrecta = rCorrecta;
        this.retroalimentacion = retro;
    }

}

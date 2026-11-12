using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
[Serializable]
public struct QuestionsBeck
{
      
    public string questionText;
    [TextArea(2,2)]
    public string[] options; // Las opciones de respuesta

}

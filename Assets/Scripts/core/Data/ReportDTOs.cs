using System;
using UnityEngine;

public enum ModoSimulacion { Simulador, Observador, Evaluador }
public enum EstadoFase { Inicial, Desarrollo, Final }

[Serializable] public struct PartidaDTO {
    public string casoId;
    public EstadoFase fase;
    public DateTime fechaModificacion;
    public string estadoJson;
}

[Serializable] public struct HistorialDTO {
    public DateTime timestamp;
    public ModoSimulacion modo;
    public EstadoFase fase;
    public float progresoPorcentaje;
}

[Serializable] public struct IntentoDTO {
    public DateTime fechaInicio;
    public float progresoPorcentaje;
    public double puntaje;
    public string feedbackAutomatico;
    public RespuestaDTO[] respuestas;
}

[Serializable] public struct RespuestaDTO {
    public string preguntaId;
    public string respuestaUsuario;
    public bool esCorrecta;
    public double puntajeObtenido;
}
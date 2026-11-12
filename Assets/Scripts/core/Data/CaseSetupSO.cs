using UnityEngine;

[CreateAssetMenu(fileName = "CaseSetup", menuName = "Gesell/Case Setup")]
public class CaseSetupSO : ScriptableObject
{
    [Header("Caso Metadata")]
    public int nroCaso;
    public string terapeutaObjectName = "TerapeutaC1";
    public string pacienteObjectName = "PacienteC1";
    public int counterAtencion = 16;
    
    [Header("Objetos en Escena")]
    public GameObject personajeLlorando;
    public GameObject personajeNormal;
    
    [Header("Ficha Diagnóstico (DSM)")]
    public int criteriosCorrectosDSM = 8;
    public bool[] respuestasCorrectasDSM;
    
    [Header("Inventario de Beck")]
    public int puntajeCorrectoBeck = 32;
}

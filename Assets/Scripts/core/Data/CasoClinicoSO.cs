using UnityEngine;
using System;
using System.Collections.Generic;

[Serializable] public struct DialogueNode {
    [Tooltip("Orden de aparición")] public int orden;
    [TextArea] public string contenido;
    [Tooltip("Paciente, Terapeuta, Observador")] public string personaje;
    public bool requiereEvaluacion;
    public string preguntaId; // Solo para referencia DTO
}

[CreateAssetMenu(fileName = "CasoClinico", menuName = "Gesell/Caso Clínico")]
public class CasoClinicoSO : ScriptableObject
{
    [Header("Metadata")]
    public string casoId;
    public string titulo;
    public string descripcionClinica;
    
    [Header("Contenido")]
    public List<DialogueNode> dialogos = new();
    
    private void OnValidate()
    {
        // Validación automática para psicólogos
        dialogos.Sort((a, b) => a.orden.CompareTo(b.orden));
        for (int i = 0; i < dialogos.Count; i++) {
            if (string.IsNullOrWhiteSpace(dialogos[i].contenido)) {
                Debug.LogWarning($"[Caso {casoId}] Diálogo {i} vacío. Revisa antes de build.");
            }
        }
    }
}
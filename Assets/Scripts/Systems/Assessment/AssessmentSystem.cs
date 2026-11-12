using System;
using UnityEngine;

public static class ScoringLogic
{
    public static double Calculate(double maxScore, double correct, int total)
    {
        if (total == 0) return 0;
        return Math.Clamp((correct / total) * maxScore, 0, maxScore);
    }

    public static bool ValidateRange(double value, double min, double max)
    {
        return value >= min && value <= max;
    }
}

public class AssessmentSystem : MonoBehaviour
{
    [Header("Rúbrica")]
    [SerializeField] private double maxScore = 100;
    [SerializeField] private int totalQuestions = 10;
    
    private int correctAnswers = 0;
    public event Action<IntentoDTO> OnAttemptCompleted;

    public void RegisterAnswer(bool isCorrect)
    {
        if (isCorrect) correctAnswers++;
    }

    public void FinalizeAttempt(string casoId, RespuestaDTO[] respuestas)
    {
        double score = ScoringLogic.Calculate(maxScore, correctAnswers, totalQuestions);
        var intento = new IntentoDTO {
            fechaInicio = DateTime.UtcNow,
            progresoPorcentaje = 100f,
            puntaje = score,
            feedbackAutomatico = GenerateFeedback(score),
            respuestas = respuestas
        };
        OnAttemptCompleted?.Invoke(intento);
    }

    private string GenerateFeedback(double score) =>
        score >= 80 ? "Dominio clínico adecuado." :
        score >= 60 ? "Revisar criterios diagnósticos." : "Requiere refuerzo en entrevista clínica.";
}
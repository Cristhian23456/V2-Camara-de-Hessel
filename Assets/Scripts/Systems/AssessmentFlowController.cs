using System;
using UnityEngine;

public class AssessmentFlowController : MonoBehaviour
{
    [SerializeField] private AssessmentSystem scoringSystem;
    [SerializeField] private RetryManager retryManager;
    // [SerializeField] private CaseFlowController caseFlow;  // ← Comentado por ahora

    public enum AssessmentModule { DSM, Beck, FinalQuestion }
    private AssessmentModule _currentModule;

    private void OnEnable()
    {
        // ✅ Suscripción correcta a eventos estáticos
        scoringSystem.OnAttemptCompleted += HandleAssessmentComplete;
        RetryManager.OnMaxAttemptsReached += HandleMaxFailures; // ← Sin 'retryManager.'
    }

    private void OnDisable()
    {
        scoringSystem.OnAttemptCompleted -= HandleAssessmentComplete;
        RetryManager.OnMaxAttemptsReached -= HandleMaxFailures; // ← Sin 'retryManager.'
    }

    public void StartModule(AssessmentModule module)
    {
        _currentModule = module;
        retryManager.ResetForNewModule();
        // GameEvents.DialogueStarted($"Inicio de evaluación: {module}");  // ← Usa el método, no el evento directo
    }

    private void HandleAssessmentComplete(IntentoDTO result)
    {
        retryManager.RecordAttempt((int)_currentModule, result.puntaje > 0);
        GameEvents.ScoreUpdated(result.puntaje); // ✅ Usa el método estático, NO el evento
    }

    private void HandleMaxFailures()
    {
        Debug.Log("Máximo de intentos alcanzados");
        // caseFlow.AdvanceToNextPhase();  // ← Comentado
    }

    public void ExportCurrentAttempt()
    {
        Debug.Log("📊 Reporte pedagógico generado listo para CSV/LMS");
    }
}
using System;

public static class GameEvents
{
    // Eventos
    public static event Action<string> OnDialogueStarted;
    public static event Action OnDialogueComplete;
    public static event Action<bool> OnPauseStateChanged;
    public static event Action<double> OnScoreUpdated;
    public static event Action OnAttemptCompleted;
    public static event Action<string> OnSceneTransitionStart;

    // ✅ Métodos estáticos para invocar eventos
    public static void DialogueStarted(string text) => OnDialogueStarted?.Invoke(text);
    public static void DialogueComplete() => OnDialogueComplete?.Invoke();
    public static void PauseStateChanged(bool isPaused) => OnPauseStateChanged?.Invoke(isPaused);
    public static void ScoreUpdated(double score) => OnScoreUpdated?.Invoke(score); // ← Este es el que falla
    public static void AttemptCompleted() => OnAttemptCompleted?.Invoke();
    public static void SceneTransitionStart(string scene) => OnSceneTransitionStart?.Invoke(scene);
}
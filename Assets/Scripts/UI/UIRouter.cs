using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIRouter : MonoBehaviour
{
    [Header("Pause UI")]
    [SerializeField] private GameObject pausePanel;
    
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI percentageText;
    
    [Header("Dialogue UI")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TextMeshProUGUI dialogueText;

    private void OnEnable()
    {
        GameEvents.OnDialogueStarted += HandleDialogue;
        GameEvents.OnPauseStateChanged += HandlePause;
        GameEvents.OnScoreUpdated += HandleScore;
    }

    private void OnDisable()
    {
        GameEvents.OnDialogueStarted -= HandleDialogue;
        GameEvents.OnPauseStateChanged -= HandlePause;
        GameEvents.OnScoreUpdated -= HandleScore;
    }

    private void HandleDialogue(string text)
    {
        if (dialoguePanel) dialoguePanel.SetActive(true);
        if (dialogueText) dialogueText.text = text;
    }

    private void HandlePause(bool isPaused) => pausePanel?.SetActive(isPaused);

    private void HandleScore(double score)
    {
        if (scoreText) scoreText.text = score.ToString("F1");
        if (percentageText) percentageText.text = $"{score:F0}%";
    }
}
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class TypewriterPresenter : MonoBehaviour
{
    [Header("Configuración")]
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField, Range(1, 100)] private float charsPerSecond = 30f;
    [SerializeField, Tooltip("Avanzar automáticamente tras terminar")] private float autoAdvanceDelay = 2f;
    [SerializeField] private bool allowSkip = true;

    private Coroutine currentRoutine;
    private bool isComplete = false;
    public bool IsComplete => isComplete;

    public event Action OnComplete;
    public event Action OnSkip;

    public void Display(string text, Action onComplete = null)
    {
        StopRoutine();
        targetText.text = text;
        isComplete = false;
        currentRoutine = StartCoroutine(TypeRoutine(onComplete));
    }

    private IEnumerator TypeRoutine(Action onComplete)
    {
        targetText.maxVisibleCharacters = 0;
        float delay = 1f / charsPerSecond;

        for (int i = 0; i < targetText.text.Length; i++)
        {
            if (allowSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))) {
                ShowAll();
                yield break;
            }
            targetText.maxVisibleCharacters++;
            yield return new WaitForSecondsRealtime(delay);
        }
        CompleteRoutine(onComplete);
    }

    private void CompleteRoutine(Action onComplete)
    {
        isComplete = true;
        onComplete?.Invoke();
        OnComplete?.Invoke();

        if (autoAdvanceDelay > 0)
            StartCoroutine(WaitAndAdvance());
    }

    private IEnumerator WaitAndAdvance()
    {
        yield return new WaitForSecondsRealtime(autoAdvanceDelay);
        OnComplete?.Invoke(); // Disparar segunda vez para flujo
    }

    public void ShowAll()
    {
        StopRoutine();
        targetText.maxVisibleCharacters = targetText.text.Length;
        isComplete = true;
        OnSkip?.Invoke();
        OnComplete?.Invoke();
    }

    private void StopRoutine()
    {
        if (currentRoutine != null) StopCoroutine(currentRoutine);
    }

    private void OnDestroy() => StopRoutine();
}
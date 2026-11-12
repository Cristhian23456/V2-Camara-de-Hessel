using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using System;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("UI Feedback")]
    [SerializeField] private CanvasGroup fadeOverlay;
    [SerializeField] private Slider progressBar;
    [SerializeField, Range(0.5f, 2f)] private float fadeDuration = 0.8f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (fadeOverlay != null) fadeOverlay.alpha = 0;
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    public void LoadSceneAsync(string sceneName, Action onComplete = null)
    {
        GameEvents.SceneTransitionStart(sceneName);
        StartCoroutine(AsyncLoadRoutine(sceneName, onComplete));
    }

    private IEnumerator AsyncLoadRoutine(string sceneName, Action onComplete)
    {
        yield return FadeIn();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        operation.allowSceneActivation = false;
        progressBar.gameObject.SetActive(true);

        while (operation.progress < 0.9f)
        {
            progressBar.value = operation.progress * 100f;
            yield return null;
        }

        progressBar.value = 100f;
        yield return new WaitForSeconds(0.2f);

        operation.allowSceneActivation = true;
        yield return new WaitUntil(() => operation.isDone);

        progressBar.gameObject.SetActive(false);
        onComplete?.Invoke();
        yield return FadeOut();
    }

    private IEnumerator FadeIn()
    {
        if (fadeOverlay == null) yield break;
        float t = 0; fadeOverlay.alpha = 0;
        while (t < 1) { t += Time.unscaledDeltaTime / fadeDuration; fadeOverlay.alpha = t; yield return null; }
    }

    private IEnumerator FadeOut()
    {
        if (fadeOverlay == null) yield break;
        float t = 0;
        while (t < 1) { t += Time.unscaledDeltaTime / fadeDuration; fadeOverlay.alpha = 1 - t; yield return null; }
    }
}
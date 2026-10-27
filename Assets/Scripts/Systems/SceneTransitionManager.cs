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
        Debug.Log("[SceneTransitionManager] Loading scene asynchronously: " + sceneName);
        try
        {
            // GameEvents may not exist in all builds; call defensively if present
            var geType = System.Type.GetType("GameEvents");
            if (geType != null)
            {
                var method = geType.GetMethod("SceneTransitionStart", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (method != null)
                {
                    method.Invoke(null, new object[] { sceneName });
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SceneTransitionManager] GameEvents.SceneTransitionStart failed: " + ex.Message);
        }
        StartCoroutine(AsyncLoadRoutine(sceneName, onComplete));
    }

    private IEnumerator AsyncLoadRoutine(string sceneName, Action onComplete)
    {
        float transStart = Time.realtimeSinceStartup;
        Debug.Log("[SceneTransitionManager][TIMING] Scene transition start: " + sceneName + " at " + transStart.ToString("F3") + "s");
        yield return FadeIn();

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
        if (operation == null)
        {
            Debug.LogError("[SceneTransitionManager] Failed to start async load for scene: " + sceneName);
            yield return FadeOut();
            yield break;
        }
        operation.allowSceneActivation = false;
        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.value = 0f;
        }

        float timeout = 30f;
        float elapsed = 0f;
        while (operation.progress < 0.9f)
        {
            elapsed += Time.unscaledDeltaTime;
            if (progressBar != null)
            {
                progressBar.value = operation.progress * 100f;
            }
            if (elapsed > timeout)
            {
                Debug.LogError("[SceneTransitionManager] Timeout loading scene: " + sceneName);
                break;
            }
            yield return null;
        }

        if (progressBar != null)
        {
            progressBar.value = 100f;
        }
        yield return new WaitForSecondsRealtime(0.2f);

        operation.allowSceneActivation = true;
        elapsed = 0f;
        while (!operation.isDone)
        {
            elapsed += Time.unscaledDeltaTime;
            if (elapsed > timeout)
            {
                Debug.LogError("[SceneTransitionManager] Timeout waiting for scene activation: " + sceneName);
                break;
            }
            yield return null;
        }

        if (progressBar != null)
        {
            progressBar.gameObject.SetActive(false);
        }
        Debug.Log("[SceneTransitionManager][TIMING] Scene transition complete: " + sceneName + " in " + (Time.realtimeSinceStartup - transStart).ToString("F3") + "s");
        try
        {
            onComplete?.Invoke();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[SceneTransitionManager] onComplete callback error: " + ex.Message);
        }
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
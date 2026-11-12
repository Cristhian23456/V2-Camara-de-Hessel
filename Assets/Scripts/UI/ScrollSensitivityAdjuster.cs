using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public static class ScrollSensitivityAdjuster
{
    // Se ejecuta automáticamente al iniciar el juego y después de cargar cualquier escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AdjustAllScrollRects();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AdjustAllScrollRects();
    }

    public static void AdjustAllScrollRects()
    {
        // En Unity 6 se usa FindObjectsByType especificando FindObjectsInactive y FindObjectsSortMode
        ScrollRect[] scrollRects = Object.FindObjectsByType<ScrollRect>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var scroll in scrollRects)
        {
            if (scroll != null && scroll.scrollSensitivity < 15f)
            {
                scroll.scrollSensitivity = 20f; // Ajustamos a un valor fluido y estándar (20f)
                Debug.Log($"[ScrollAdjuster] Ajustada sensibilidad de scroll en '{scroll.gameObject.name}' a 20f (anterior: {scroll.scrollSensitivity}f)");
            }
        }
    }
}

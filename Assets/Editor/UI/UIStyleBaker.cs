using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Reemplaza a los antiguos UIPanelStyler y ScrollSensitivityAdjuster (#15), que en cada carga
/// de escena recorrían TODOS los objetos y re-emparentaban botones por nombre en tiempo de ejecución.
/// Esta herramienta aplica esos mismos cambios UNA vez en el Editor y los guarda en las escenas,
/// así el juego ya no paga ese costo y los cambios quedan visibles (y revisables) en git.
/// Menú: Tools/UI/Aplicar estilos a escenas del Build
/// </summary>
public static class UIStyleBaker
{
    private static readonly Color COLOR_FONDO_PRINCIPAL = new Color32(226, 232, 240, 255); // Slate-200
    private static readonly Color COLOR_PANEL_INTERIOR = new Color32(241, 245, 249, 255);  // Slate-100
    private const float SCROLL_SENSITIVITY_MIN = 15f;
    private const float SCROLL_SENSITIVITY = 20f;

    private static readonly string[] PanelesRetro = { "panelRetroalimentacion", "panleRetoFinal", "panelPresione" };
    private static readonly string[] BotonesAMover = { "btncontinuar", "btnaceptar", "enviar" };

    [MenuItem("Tools/UI/Aplicar estilos a escenas del Build")]
    public static void AplicarEnEscenasDelBuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string escenaOriginal = SceneManager.GetActiveScene().path;
        int totalCambios = 0;

        foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
        {
            if (!buildScene.enabled || string.IsNullOrEmpty(buildScene.path)) continue;

            Scene escena = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
            int cambios = AplicarEnEscena(escena);
            if (cambios > 0)
            {
                EditorSceneManager.MarkSceneDirty(escena);
                EditorSceneManager.SaveScene(escena);
            }
            Debug.Log($"[UIStyleBaker] {buildScene.path}: {cambios} cambio(s).");
            totalCambios += cambios;
        }

        if (!string.IsNullOrEmpty(escenaOriginal))
            EditorSceneManager.OpenScene(escenaOriginal, OpenSceneMode.Single);

        EditorUtility.DisplayDialog("Estilos de UI",
            $"Listo: {totalCambios} cambio(s) guardados en las escenas del Build.\nRevisa y haz commit de las escenas modificadas.",
            "OK");
    }

    [MenuItem("Tools/UI/Aplicar estilos a la escena abierta")]
    public static void AplicarEnEscenaAbierta()
    {
        Scene escena = SceneManager.GetActiveScene();
        int cambios = AplicarEnEscena(escena);
        if (cambios > 0) EditorSceneManager.MarkSceneDirty(escena);
        Debug.Log($"[UIStyleBaker] {escena.path}: {cambios} cambio(s). Guarda la escena para conservarlos.");
    }

    /// <summary>Aplica colores, botones y scroll en una escena. Devuelve cuántos cambios hizo.</summary>
    public static int AplicarEnEscena(Scene escena)
    {
        var todos = new List<Transform>();
        foreach (GameObject raiz in escena.GetRootGameObjects())
            todos.AddRange(raiz.GetComponentsInChildren<Transform>(true));

        int cambios = 0;
        cambios += AplicarColores(todos);
        cambios += MoverBotonesDentroDePaneles(todos);
        cambios += AjustarScroll(escena);
        return cambios;
    }

    private static int AplicarColores(List<Transform> todos)
    {
        int cambios = 0;
        foreach (Transform t in todos)
        {
            Color? destino = null;
            if (t.name == "fondoAmarillo")
            {
                destino = COLOR_FONDO_PRINCIPAL;
            }
            else if (t.name == "Panel" && t.parent != null)
            {
                string padre = t.parent.name;
                if (padre == "fondoAmarillo" || ContieneAlguno(padre, PanelesRetro))
                    destino = COLOR_PANEL_INTERIOR;
            }

            if (destino == null) continue;
            Image img = t.GetComponent<Image>();
            if (img == null || img.color == destino.Value) continue;

            Undo.RecordObject(img, "Aplicar estilos de UI");
            img.color = destino.Value;
            cambios++;
        }
        return cambios;
    }

    private static int MoverBotonesDentroDePaneles(List<Transform> todos)
    {
        int cambios = 0;
        foreach (string nombrePanel in PanelesRetro)
        {
            Transform panel = todos.Find(t => t != null && t.name == nombrePanel);
            if (panel == null || panel.parent == null) continue;

            Transform contenedor = panel.Find("fondoAmarillo") ?? panel;
            Transform padre = panel.parent;

            for (int i = padre.childCount - 1; i >= 0; i--)
            {
                Transform hijo = padre.GetChild(i);
                if (hijo == panel || hijo.GetComponent<Button>() == null) continue;
                if (!ContieneAlguno(hijo.name.ToLowerInvariant(), BotonesAMover)) continue;

                Undo.SetTransformParent(hijo, contenedor, "Mover botón dentro del panel");
                hijo.SetAsLastSibling();

                if (hijo is RectTransform rect)
                {
                    Undo.RecordObject(rect, "Ubicar botón");
                    rect.anchorMin = new Vector2(0.5f, 0f);
                    rect.anchorMax = new Vector2(0.5f, 0f);
                    rect.pivot = new Vector2(0.5f, 0f);
                    rect.anchoredPosition = new Vector2(0f, 20f);
                }
                Debug.Log($"[UIStyleBaker] Botón '{hijo.name}' movido dentro de '{nombrePanel}'.");
                cambios++;
            }
        }
        return cambios;
    }

    private static int AjustarScroll(Scene escena)
    {
        int cambios = 0;
        foreach (GameObject raiz in escena.GetRootGameObjects())
        {
            foreach (ScrollRect scroll in raiz.GetComponentsInChildren<ScrollRect>(true))
            {
                if (scroll.scrollSensitivity >= SCROLL_SENSITIVITY_MIN) continue;
                Undo.RecordObject(scroll, "Ajustar sensibilidad de scroll");
                scroll.scrollSensitivity = SCROLL_SENSITIVITY;
                cambios++;
            }
        }
        return cambios;
    }

    private static bool ContieneAlguno(string texto, string[] claves)
    {
        foreach (string clave in claves)
            if (texto.Contains(clave)) return true;
        return false;
    }
}

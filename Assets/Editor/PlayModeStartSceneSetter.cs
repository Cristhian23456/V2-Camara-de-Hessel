using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class PlayModeStartSceneSetter
{
    static PlayModeStartSceneSetter()
    {
        // Configura Unity para que, al presionar Play en el Editor, siempre inicie desde la escena de login
        string scenePath = "Assets/Scenes/Iniciar sesion.unity";
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
            Debug.Log($"[PlayModeStartScene] Configurado para iniciar siempre desde: {scenePath}");
        }
        else
        {
            Debug.LogWarning($"[PlayModeStartScene] No se encontró la escena de login en: {scenePath}");
        }
    }
}

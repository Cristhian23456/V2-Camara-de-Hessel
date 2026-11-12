using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Se ejecuta automaticamente al cargar cualquier escena.
/// Aplica colores pasteles a los paneles fondoAmarillo y Panel,
/// y reparenta los botones Continuar/Aceptar dentro de sus paneles padre.
/// </summary>
public static class UIPanelStyler
{
    // Colores pasteles amigables y combinados
    private static readonly Color COLOR_FONDO_PRINCIPAL = new Color32(226, 232, 240, 255);   // Slate-200 gris azulado suave
    private static readonly Color COLOR_PANEL_INTERIOR  = new Color32(241, 245, 249, 255);   // Slate-100 blanco gris limpio

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        AplicarEstilos();
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AplicarEstilos();
    }

    private static void AplicarEstilos()
    {
        AplicarColoresPaneles();
        ReparentarBotonesEnPaneles();
    }

    /// <summary>
    /// Busca todos los GameObjects llamados "fondoAmarillo" y "Panel" dentro de los paneles
    /// de retroalimentacion y les cambia el color a pasteles armoniosos.
    /// </summary>
    private static void AplicarColoresPaneles()
    {
        // Buscar TODOS los objetos en la escena (incluyendo inactivos)
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();

        foreach (Transform t in allTransforms)
        {
            if (t == null || t.gameObject.scene.name == null) continue;

            string nombre = t.gameObject.name;

            // Cambiar color de fondoAmarillo (fondo amarillo chillon -> gris azulado suave)
            if (nombre == "fondoAmarillo")
            {
                Image img = t.GetComponent<Image>();
                if (img != null)
                {
                    img.color = COLOR_FONDO_PRINCIPAL;
                }
            }

            // Cambiar color de Panel (dentro de fondoAmarillo o paneles de retroalimentacion)
            if (nombre == "Panel")
            {
                Transform parent = t.parent;
                if (parent != null)
                {
                    string parentName = parent.gameObject.name;
                    if (parentName == "fondoAmarillo" || 
                        parentName.Contains("panelRetroalimentacion") || 
                        parentName.Contains("panleRetoFinal") ||
                        parentName.Contains("panelPresione"))
                    {
                        Image img = t.GetComponent<Image>();
                        if (img != null)
                        {
                            img.color = COLOR_PANEL_INTERIOR;
                        }
                    }
                }
            }
        }

        Debug.Log("[UIPanelStyler] Colores pasteles aplicados a fondoAmarillo y Panel.");
    }

    /// <summary>
    /// Busca botones que esten fuera de sus paneles padre y los reparenta correctamente.
    /// Los paneles clave son: panelRetroalimentacion, panleRetoFinal, panelPresione.
    /// </summary>
    private static void ReparentarBotonesEnPaneles()
    {
        string[] panelNames = { "panelRetroalimentacion", "panleRetoFinal", "panelPresione" };

        foreach (string panelName in panelNames)
        {
            GameObject panel = BuscarObjetoPorNombre(panelName);
            if (panel == null) continue;

            // Buscar el fondoAmarillo o el contenedor visual dentro del panel
            Transform contenedor = panel.transform;
            Transform fondoAm = contenedor.Find("fondoAmarillo");
            if (fondoAm != null)
            {
                contenedor = fondoAm;
            }

            // Verificar si hay botones hermanos del panel que deberian estar adentro
            Transform panelParent = panel.transform.parent;
            if (panelParent == null) continue;

            for (int i = panelParent.childCount - 1; i >= 0; i--)
            {
                Transform child = panelParent.GetChild(i);
                if (child.gameObject == panel) continue;

                string childName = child.gameObject.name.ToLower();

                if (childName.Contains("btncontinuar") || childName.Contains("btnaceptar") || 
                    childName.Contains("enviar"))
                {
                    Button btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        child.SetParent(contenedor);
                        child.SetAsLastSibling();

                        RectTransform btnRect = child.GetComponent<RectTransform>();
                        if (btnRect != null)
                        {
                            btnRect.anchorMin = new Vector2(0.5f, 0f);
                            btnRect.anchorMax = new Vector2(0.5f, 0f);
                            btnRect.pivot = new Vector2(0.5f, 0f);
                            btnRect.anchoredPosition = new Vector2(0f, 20f);
                        }

                        Debug.Log($"[UIPanelStyler] Boton '{child.gameObject.name}' reparentado dentro de '{panelName}'.");
                    }
                }
            }
        }
    }

    private static GameObject BuscarObjetoPorNombre(string nombre)
    {
        Transform[] allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t != null && t.gameObject.scene.name != null && t.gameObject.name == nombre)
            {
                return t.gameObject;
            }
        }
        return null;
    }
}

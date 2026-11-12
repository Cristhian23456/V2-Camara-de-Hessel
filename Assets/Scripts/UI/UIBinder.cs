using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class UIBinder : MonoBehaviour
{
    [SerializeField] private UIStyleSO style;
    [SerializeField] private Button targetButton;

    private void OnValidate()
    {
        if (style == null || targetButton == null) return;
        
        var colors = targetButton.colors;
        colors.normalColor = style.primaryColor;
        targetButton.colors = colors;

        if (targetButton.TryGetComponent<RectTransform>(out var rect))
        {
            rect.offsetMin = new Vector2(style.padding, style.padding);
            rect.offsetMax = new Vector2(-style.padding, -style.padding);
        }
    }
}
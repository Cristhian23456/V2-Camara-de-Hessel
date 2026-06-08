using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "UIStyle", menuName = "Gesell/UI Style")]
public class UIStyleSO : ScriptableObject
{
    [Header("Colores")] public Color primaryColor = new Color32(33, 66, 131, 255);
    [Header("Layout")] public float padding = 15f; public float spacing = 10f;
    [Header("Botones")] public Sprite normalSprite, hoverSprite, pressedSprite;
}
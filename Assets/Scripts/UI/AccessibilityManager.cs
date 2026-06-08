using System;
using UnityEngine;
using UnityEngine.UI;

public class AccessibilityManager : MonoBehaviour
{
    public static event Action<bool> OnFontSizeChanged;
    [SerializeField] private CanvasScaler canvasScaler;
    [SerializeField] private float defaultRefWidth = 1920f;
    [SerializeField] private float largeRefWidth = 2560f;
    
    private bool _isLargeText;

    private void Awake()
    {
        _isLargeText = PlayerPrefs.GetInt("LargeText", 0) == 1;
        ApplyFontSize(_isLargeText);
    }

    public void ToggleLargeText(bool enable)
    {
        _isLargeText = enable;
        ApplyFontSize(enable);
        PlayerPrefs.SetInt("LargeText", enable ? 1 : 0);
        OnFontSizeChanged?.Invoke(enable);
    }

    private void ApplyFontSize(bool large)
    {
        if (canvasScaler == null) return;
        canvasScaler.referenceResolution = new Vector2(large ? largeRefWidth : defaultRefWidth, 1080);
        Canvas.ForceUpdateCanvases();
    }
}
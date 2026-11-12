using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI targetText;
    [SerializeField] private float scrollSpeed = 8f;
    [SerializeField] private Button bottomButton; // Evita solapamiento
    
    private ScrollRect _scroll;
    private RectTransform _content;
    private Coroutine _scrollRoutine;

    private void Awake()
    {
        _scroll = GetComponent<ScrollRect>();
        _content = _scroll.content;
    }

    public void OnTextUpdated()
    {
        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
        _scrollRoutine = StartCoroutine(SmoothScrollToBottom());
    }

    private System.Collections.IEnumerator SmoothScrollToBottom()
    {
        float t = 0;
        Vector2 startPos = _content.anchoredPosition;
        Vector2 endPos = new Vector2(startPos.x, -(_content.sizeDelta.y - _scroll.viewport.rect.height));
        
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * scrollSpeed;
            _content.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }
    }

    public void DisableAutoScroll(bool disable)
    {
        _scroll.vertical = !disable;
        if (_scrollRoutine != null) StopCoroutine(_scrollRoutine);
    }
}
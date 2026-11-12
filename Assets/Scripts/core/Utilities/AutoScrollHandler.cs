using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AutoScrollHandler : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private float scrollSpeed = 2f;
    [SerializeField] private float scrollDelay = 0.1f;

    public void ScrollAlElemento(Transform elemento)
    {
        if (scrollRect == null || elemento == null) return;
        StartCoroutine(ScrollSuave(elemento));
    }

    private IEnumerator ScrollSuave(Transform target)
    {
        yield return new WaitForSecondsRealtime(scrollDelay);

        Canvas.ForceUpdateCanvases();

        float duration = 0.5f / scrollSpeed;
        float elapsed = 0f;
        Vector2 startPosition = scrollRect.normalizedPosition;
        Vector2 endPosition = CalculateNormalizedPosition(target);

        while (elapsed < duration)
        {
            scrollRect.normalizedPosition = Vector2.Lerp(startPosition, endPosition, elapsed / duration);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        scrollRect.normalizedPosition = endPosition;
    }

    private Vector2 CalculateNormalizedPosition(Transform target)
    {
        RectTransform content = scrollRect.content;
        // Calcular la posición vertical normalizada del target dentro del panel de contenido
        float targetPos = Mathf.InverseLerp(
            content.position.y,
            content.position.y - content.rect.height,
            target.position.y
        );
        return new Vector2(0f, Mathf.Clamp01(targetPos));
    }
}

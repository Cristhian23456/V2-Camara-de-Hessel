using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textField;
    [SerializeField] private float charsPerSecond = 50f;
    [SerializeField] private AudioClip audioTyping;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private bool waitForInput = true;
    [SerializeField] private GameObject continueButton;

    private string fullText;
    private int currentCharIndex = 0;
    private bool isTyping = false;
    private bool canSkip = false;

    public bool IsTyping => isTyping;

    public void StartTyping(string text, bool allowSkip = true)
    {
        if (textField == null) return;
        
        fullText = text;
        currentCharIndex = 0;
        textField.maxVisibleCharacters = 0;
        textField.text = fullText;
        isTyping = true;
        canSkip = allowSkip;

        if (continueButton != null)
            continueButton.SetActive(false);

        StopAllCoroutines();
        StartCoroutine(TypeCoroutine());
    }

    private IEnumerator TypeCoroutine()
    {
        float charInterval = 1f / charsPerSecond;

        while (currentCharIndex < fullText.Length && isTyping)
        {
            textField.maxVisibleCharacters++;
            currentCharIndex++;

            // Reproducir sonido de tecleado cada 3 caracteres para no saturar
            if (audioTyping != null && audioSource != null && currentCharIndex % 3 == 0)
            {
                audioSource.PlayOneShot(audioTyping, 0.5f);
            }

            // Permitir skip presionando Espacio o Clic Izquierdo
            if (canSkip && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                SkipToEnd();
                yield break;
            }

            yield return new WaitForSecondsRealtime(charInterval);
        }

        FinishTyping();
    }

    public void SkipToEnd()
    {
        if (canSkip && isTyping)
        {
            StopAllCoroutines();
            if (textField != null) textField.maxVisibleCharacters = fullText.Length;
            currentCharIndex = fullText.Length;
            FinishTyping();
        }
    }

    private void FinishTyping()
    {
        isTyping = false;
        if (continueButton != null && waitForInput)
        {
            continueButton.SetActive(true);
        }
    }
}

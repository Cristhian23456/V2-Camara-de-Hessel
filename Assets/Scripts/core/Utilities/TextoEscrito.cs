using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Efecto "máquina de escribir" común a todos los textos del simulador (#4).
/// - Clic, Espacio o Enter muestran el texto completo de inmediato.
/// - Cuenta solo caracteres visibles (las etiquetas &lt;b&gt;, &lt;color&gt;... no suman tiempo).
/// - Si el texto no cabe en su cuadro, se achica (auto-size) en lugar de desbordarse.
/// Uso dentro de una corrutina: yield return TextoEscrito.Escribir(txt, texto, 0.07f);
/// </summary>
public static class TextoEscrito
{
    /// <summary>Un carácter por frame a 60 FPS (equivale a los antiguos WaitForSeconds(2f / 500)).</summary>
    public const float RAPIDO = 1f / 60f;
    /// <summary>Velocidad de los diálogos (antes WaitForSeconds(35f / 500)).</summary>
    public const float NORMAL = 35f / 500f;

    public static bool PulsoAvanzar()
    {
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
    }

    /// <summary>Activa auto-size usando el tamaño actual como máximo. Solo achica si no cabe.</summary>
    public static void PrepararAutoSize(TextMeshProUGUI txt, float minimoRelativo = 0.7f)
    {
        if (txt == null || txt.enableAutoSizing) return;
        float tamano = txt.fontSize;
        txt.fontSizeMax = tamano;
        txt.fontSizeMin = Mathf.Max(8f, tamano * minimoRelativo);
        txt.enableAutoSizing = true;
    }

    public static IEnumerator Escribir(TextMeshProUGUI txt, string texto, float segundosPorCaracter, float autoSizeMinimo = 0.7f)
    {
        if (txt == null) yield break;

        txt.richText = true;
        PrepararAutoSize(txt, autoSizeMinimo);
        txt.text = texto ?? "";
        txt.maxVisibleCharacters = 0;
        txt.ForceMeshUpdate();
        int total = txt.textInfo.characterCount;

        // Ignorar el clic que abrió este texto
        yield return null;

        float acumulado = 0f;
        while (txt.maxVisibleCharacters < total)
        {
            if (PulsoAvanzar())
            {
                txt.maxVisibleCharacters = total;
                yield return null; // el mismo clic no debe saltar también lo que sigue
                break;
            }
            acumulado += Time.deltaTime;
            int visibles = segundosPorCaracter > 0f ? Mathf.FloorToInt(acumulado / segundosPorCaracter) : total;
            txt.maxVisibleCharacters = Mathf.Clamp(visibles, 0, total);
            yield return null;
        }
        txt.maxVisibleCharacters = total;
    }

    /// <summary>Espera un tiempo de lectura proporcional al largo del texto; se puede saltar.</summary>
    public static IEnumerator EsperarLectura(string texto, float minimo = 2f, float maximo = 6f, float porCaracter = 0.03f)
    {
        int largo = string.IsNullOrEmpty(texto) ? 0 : texto.Length;
        float espera = Mathf.Clamp(minimo + largo * porCaracter, minimo, maximo);
        float t = 0f;
        while (t < espera)
        {
            if (PulsoAvanzar()) yield break;
            t += Time.deltaTime;
            yield return null;
        }
    }
}

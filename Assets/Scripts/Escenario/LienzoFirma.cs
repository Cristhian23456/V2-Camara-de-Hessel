using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Área de firma: el usuario dibuja con el mouse (o el dedo) sobre una textura.
/// Debe ir en un objeto con RawImage. Llamar a Inicializar() antes de usarlo.
/// </summary>
[RequireComponent(typeof(RawImage))]
public class LienzoFirma : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public int grosor = 3;
    public Color32 tinta = new Color32(20, 30, 90, 255);

    /// <summary>Se dispara cada vez que cambia el dibujo (trazo nuevo o limpiar).</summary>
    public event Action Cambio;

    public Texture2D Textura { get; private set; }
    public int PixelesPintados { get; private set; }

    private static readonly Color32 Blanco = new Color32(255, 255, 255, 255);

    private RectTransform rt;
    private Color32[] buffer;
    private int ancho, alto;
    private Vector2? ultimo;
    private bool sucio;

    public void Inicializar(int anchoPx, int altoPx)
    {
        ancho = Mathf.Max(16, anchoPx);
        alto = Mathf.Max(16, altoPx);
        rt = (RectTransform)transform;
        if (Textura != null) Destroy(Textura);
        Textura = new Texture2D(ancho, alto, TextureFormat.RGBA32, false)
        {
            name = "FirmaEstudiante",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        buffer = new Color32[ancho * alto];
        GetComponent<RawImage>().texture = Textura;
        Limpiar();
    }

    public void Limpiar()
    {
        if (buffer == null) return;
        for (int i = 0; i < buffer.Length; i++) buffer[i] = Blanco;
        PixelesPintados = 0;
        ultimo = null;
        sucio = true;
        Aplicar();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!APixel(e, out var p)) return;
        Punto(p);
        ultimo = p;
        Aplicar();
    }

    public void OnDrag(PointerEventData e)
    {
        if (!APixel(e, out var p)) { ultimo = null; return; }
        if (ultimo.HasValue) Linea(ultimo.Value, p);
        else Punto(p);
        ultimo = p;
        Aplicar();
    }

    public void OnPointerUp(PointerEventData e)
    {
        ultimo = null;
    }

    private bool APixel(PointerEventData e, out Vector2 p)
    {
        p = default;
        if (buffer == null) return false;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, e.position, e.pressEventCamera, out var local))
            return false;
        Rect r = rt.rect;
        p = new Vector2((local.x - r.x) / r.width * ancho, (local.y - r.y) / r.height * alto);
        return p.x >= 0 && p.y >= 0 && p.x < ancho && p.y < alto;
    }

    private void Linea(Vector2 a, Vector2 b)
    {
        float distancia = Vector2.Distance(a, b);
        int pasos = Mathf.Max(1, Mathf.CeilToInt(distancia / Mathf.Max(1f, grosor * 0.5f)));
        for (int i = 0; i <= pasos; i++) Punto(Vector2.Lerp(a, b, i / (float)pasos));
    }

    private void Punto(Vector2 c)
    {
        int cx = Mathf.RoundToInt(c.x), cy = Mathf.RoundToInt(c.y), r = grosor;
        for (int y = -r; y <= r; y++)
        {
            int py = cy + y;
            if (py < 0 || py >= alto) continue;
            for (int x = -r; x <= r; x++)
            {
                int px = cx + x;
                if (px < 0 || px >= ancho || x * x + y * y > r * r) continue;
                int i = py * ancho + px;
                if (buffer[i].r == 255 && buffer[i].g == 255 && buffer[i].b == 255) PixelesPintados++;
                buffer[i] = tinta;
            }
        }
        sucio = true;
    }

    private void Aplicar()
    {
        if (!sucio || Textura == null) return;
        Textura.SetPixels32(buffer);
        Textura.Apply(false);
        sucio = false;
        Cambio?.Invoke();
    }

    private void OnDestroy()
    {
        if (Textura != null) Destroy(Textura);
    }
}

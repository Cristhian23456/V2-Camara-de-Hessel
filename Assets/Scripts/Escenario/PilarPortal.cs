using UnityEngine;

/// <summary>
/// Efecto visual del pilar verde: el color "respira" (brillo con emisión), una luz suave
/// acompaña el pulso y el pilar gira despacio. Se intensifica cuando el jugador se acerca.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class PilarPortal : MonoBehaviour
{
    [SerializeField] private Color color = new Color32(0x2E, 0xCC, 0x71, 0xFF);
    [SerializeField, Min(0.05f)] private float periodo = 1.8f;
    [SerializeField, Range(0f, 3f)] private float emisionMin = 0.35f;
    [SerializeField, Range(0f, 3f)] private float emisionMax = 1.4f;
    [SerializeField] private float velocidadGiro = 25f;
    [SerializeField, Min(0.1f)] private float distanciaCerca = 3f;
    [SerializeField] private bool crearLuz = true;

    private Renderer rend;
    private MaterialPropertyBlock bloque;
    private Light luz;
    private Transform jugador;
    private static readonly int Emision = Shader.PropertyToID("_EmissionColor");
    private static readonly int ColorBase = Shader.PropertyToID("_Color");

    private void Awake()
    {
        rend = GetComponent<Renderer>();
        bloque = new MaterialPropertyBlock();
        // El Standard shader solo emite si la palabra clave está activa.
        foreach (var m in rend.materials) m.EnableKeyword("_EMISSION");

        if (crearLuz)
        {
            var go = new GameObject("LuzPilar");
            go.transform.SetParent(transform, false);
            luz = go.AddComponent<Light>();
            luz.type = LightType.Point;
            luz.color = color;
            luz.range = 2.5f;
            luz.shadows = LightShadows.None;
        }
    }

    private void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) jugador = p.transform;
    }

    private void Update()
    {
        float onda = 0.5f + 0.5f * Mathf.Sin(Time.time * 2f * Mathf.PI / periodo);
        float cerca = 0f;
        if (jugador != null)
        {
            Vector3 d = jugador.position - transform.position; d.y = 0f;
            cerca = 1f - Mathf.Clamp01(d.magnitude / distanciaCerca);
        }
        float intensidad = Mathf.Lerp(emisionMin, emisionMax, onda) * (1f + cerca);

        rend.GetPropertyBlock(bloque);
        bloque.SetColor(ColorBase, color);
        bloque.SetColor(Emision, color * intensidad);
        rend.SetPropertyBlock(bloque);

        if (luz != null) luz.intensity = 0.6f + intensidad * 0.9f;
        transform.Rotate(0f, velocidadGiro * Time.deltaTime, 0f, Space.World);
    }
}

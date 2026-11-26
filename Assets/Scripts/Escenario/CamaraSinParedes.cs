using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Cámara de tercera persona hija del jugador que no atraviesa paredes: cada frame lanza una
/// esfera desde la cabeza del jugador hacia la posición ideal de la cámara y, si choca con algo,
/// acerca la cámara justo delante del obstáculo. Cuando queda muy cerca, oculta el cuerpo del
/// jugador (solo deja su sombra) para no ver el interior del modelo.
/// </summary>
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(1000)]
public class CamaraSinParedes : MonoBehaviour
{
    [Tooltip("Punto de la cabeza/cuello del jugador desde el que se mide. Si está vacío se usa la altura de la cámara.")]
    [SerializeField] private Transform pivote;
    [SerializeField, Min(0.02f)] private float radioEsfera = 0.12f;
    [SerializeField, Min(0f)] private float margen = 0.06f;
    [SerializeField, Min(0.05f)] private float distanciaMinima = 0.25f;
    [Tooltip("Velocidad con la que la cámara vuelve a alejarse cuando el camino queda libre.")]
    [SerializeField, Min(0.1f)] private float velocidadRetorno = 4f;
    [SerializeField, Range(0.01f, 0.3f)] private float nearClip = 0.05f;
    [Tooltip("Por debajo de esta distancia al pivote se oculta el cuerpo del jugador.")]
    [SerializeField, Min(0f)] private float distanciaOcultarJugador = 0.45f;
    [SerializeField] private LayerMask capas = ~0;

    private Transform raiz;
    private Vector3 offsetLocal;
    private Vector3 pivoteLocal;
    private float distanciaActual;
    private Renderer[] cuerpo;
    private bool cuerpoOculto;
    private readonly RaycastHit[] golpes = new RaycastHit[16];

    private void Awake()
    {
        raiz = transform.parent;
        if (raiz == null) { enabled = false; return; }

        // Si el antiguo CameraController está activo, él manda.
        var antiguo = GetComponent<CameraController>();
        if (antiguo != null && antiguo.enabled) { enabled = false; return; }

        offsetLocal = transform.localPosition;
        pivoteLocal = pivote != null
            ? raiz.InverseTransformPoint(pivote.position)
            : new Vector3(0f, offsetLocal.y * 0.9f, 0f);
        distanciaActual = (offsetLocal - pivoteLocal).magnitude;

        var cam = GetComponent<Camera>();
        cam.nearClipPlane = Mathf.Min(cam.nearClipPlane, nearClip);

        cuerpo = raiz.GetComponentsInChildren<Renderer>(true);
    }

    private void OnDisable() => MostrarCuerpo(true);

    private void LateUpdate()
    {
        Vector3 origen = raiz.TransformPoint(pivoteLocal);
        Vector3 ideal = raiz.TransformPoint(offsetLocal);
        Vector3 haciaCamara = ideal - origen;
        float distanciaIdeal = haciaCamara.magnitude;
        if (distanciaIdeal < 0.001f) return;
        Vector3 dir = haciaCamara / distanciaIdeal;

        float permitida = distanciaIdeal;
        int n = Physics.SphereCastNonAlloc(origen, radioEsfera, dir, golpes, distanciaIdeal, capas, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < n; i++)
        {
            var c = golpes[i].collider;
            if (c == null || c.transform.IsChildOf(raiz)) continue;      // el propio jugador
            if (golpes[i].distance <= 0f && golpes[i].point == Vector3.zero) continue; // solapado al inicio
            permitida = Mathf.Min(permitida, golpes[i].distance - margen);
        }
        permitida = Mathf.Clamp(permitida, distanciaMinima, distanciaIdeal);

        // Se acerca al instante (nunca se ve a través) y se aleja suavemente.
        distanciaActual = permitida < distanciaActual
            ? permitida
            : Mathf.MoveTowards(distanciaActual, permitida, velocidadRetorno * Time.deltaTime);

        transform.position = origen + dir * distanciaActual;
        MostrarCuerpo(distanciaActual > distanciaOcultarJugador);
    }

    private void MostrarCuerpo(bool visible)
    {
        if (cuerpo == null || cuerpoOculto == !visible) return;
        cuerpoOculto = !visible;
        foreach (var r in cuerpo)
            if (r != null) r.shadowCastingMode = visible ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly;
    }
}

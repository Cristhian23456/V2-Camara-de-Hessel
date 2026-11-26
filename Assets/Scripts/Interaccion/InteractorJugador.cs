using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Va en el jugador. Cada frame busca el IInteractuable más cercano que esté delante y dentro de
/// su radio, muestra "[E] acción" y lo ejecuta al pulsar la tecla.
/// </summary>
public class InteractorJugador : MonoBehaviour
{
    private static readonly List<IInteractuable> registrados = new List<IInteractuable>();

    [SerializeField] private KeyCode tecla = KeyCode.E;
    [Tooltip("Producto escalar mínimo entre la mirada del jugador y la dirección al objeto (−1 = detrás también).")]
    [SerializeField, Range(-1f, 1f)] private float anguloMinimo = -0.1f;

    private PlayerController controlador;
    private IInteractuable actual;

    public static void Registrar(IInteractuable i) { if (!registrados.Contains(i)) registrados.Add(i); }
    public static void Quitar(IInteractuable i) { registrados.Remove(i); }

    private void Awake()
    {
        controlador = GetComponent<PlayerController>();
    }

    private void OnDisable()
    {
        actual = null;
        InteraccionUI.Ocultar();
    }

    private void Update()
    {
        // Si el movimiento está bloqueado (diálogos, paneles, cinemáticas) no se ofrece interacción.
        if ((controlador != null && !controlador.enabled) || Time.timeScale <= 0f)
        {
            if (actual != null) { actual = null; InteraccionUI.Ocultar(); }
            return;
        }

        IInteractuable mejor = Buscar();
        if (mejor != actual)
        {
            actual = mejor;
            if (actual == null) InteraccionUI.Ocultar();
        }
        if (actual == null) return;

        InteraccionUI.Mostrar(actual.TextoInteraccion, tecla.ToString());

        if (Input.GetKeyDown(tecla) && actual.PuedeInteractuar)
        {
            InteraccionUI.Pulsar();
            actual.Interactuar(transform);
        }
    }

    private IInteractuable Buscar()
    {
        Vector3 pos = transform.position;
        Vector3 frente = transform.forward; frente.y = 0f; frente.Normalize();
        IInteractuable mejor = null;
        float mejorDist = float.MaxValue;

        for (int i = registrados.Count - 1; i >= 0; i--)
        {
            var it = registrados[i];
            if (it == null || (it is Object o && o == null)) { registrados.RemoveAt(i); continue; }
            if (it is Behaviour b && !b.isActiveAndEnabled) continue;

            Vector3 d = it.PuntoInteraccion - pos; d.y = 0f;
            float dist = d.magnitude;
            if (dist > it.RadioInteraccion || dist >= mejorDist) continue;
            if (dist > 0.15f && Vector3.Dot(frente, d / dist) < anguloMinimo) continue;

            mejor = it;
            mejorDist = dist;
        }
        return mejor;
    }
}

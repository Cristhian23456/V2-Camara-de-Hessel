using UnityEngine;

/// <summary>
/// Zona frente a la puerta de un área restringida. Cuando el jugador entra muestra un aviso
/// (AvisoUI). El rótulo físico "ÁREA RESTRINGIDA" es un hijo de este objeto.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AreaRestringida : MonoBehaviour
{
    [SerializeField] private string nombreArea = "Archivo";
    [SerializeField, TextArea(2, 4)]
    private string mensaje = "Acceso solo para personal autorizado. Este espacio no forma parte de la práctica.";
    [SerializeField] private string tagJugador = "Player";
    [Tooltip("Segundos mínimos entre dos avisos de esta misma zona.")]
    [SerializeField, Min(0f)] private float enfriamiento = 4f;
    [SerializeField, Min(0.5f)] private float duracionAviso = 4f;

    private float ultimoAviso = float.NegativeInfinity;

    public string NombreArea => nombreArea;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(tagJugador)) return;
        if (Time.unscaledTime - ultimoAviso < enfriamiento) return;
        ultimoAviso = Time.unscaledTime;
        MostrarAviso();
    }

    /// <summary>Muestra el aviso de esta zona (público para pruebas y para otros scripts).</summary>
    public void MostrarAviso()
    {
        AvisoUI.Mostrar("Área restringida: " + nombreArea, mensaje, TipoAviso.Restringido, duracionAviso);
    }
}

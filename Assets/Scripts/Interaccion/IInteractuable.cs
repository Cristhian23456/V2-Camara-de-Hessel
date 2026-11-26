using UnityEngine;

/// <summary>Algo con lo que el jugador puede interactuar pulsando la tecla de acción (E).</summary>
public interface IInteractuable
{
    /// <summary>Punto del mundo desde el que se mide la distancia al jugador.</summary>
    Vector3 PuntoInteraccion { get; }

    /// <summary>Distancia horizontal máxima (m) a la que aparece el aviso.</summary>
    float RadioInteraccion { get; }

    /// <summary>Texto del aviso, p. ej. "Abrir puerta del Archivo".</summary>
    string TextoInteraccion { get; }

    /// <summary>Si ahora mismo se puede usar (p. ej. no está en plena animación).</summary>
    bool PuedeInteractuar { get; }

    void Interactuar(Transform jugador);
}

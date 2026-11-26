using UnityEngine;

public class MovimientoInteractivo : MonoBehaviour
{
    [SerializeField] private Transform punto;
    [SerializeField] private Transform personajeTransform;
    [SerializeField] private GameObject grupo;
    [SerializeField] private GameObject mensajes;
    [SerializeField] private PlayerController playerController;

    /// <summary>Lleva al jugador desde el pasillo del tutorial al vestíbulo del laboratorio.</summary>
    public void teletransprote()
    {
        if (grupo != null) grupo.SetActive(false);
        Time.timeScale = 1;
        if (mensajes != null) mensajes.SetActive(false);

        if (playerController == null && personajeTransform != null)
            playerController = personajeTransform.GetComponent<PlayerController>();

        // El CharacterController sobrescribe transform.position: se usa Teleport().
        if (playerController != null) playerController.Teleport(punto.position, punto.rotation);
        else if (personajeTransform != null) personajeTransform.position = punto.position;

        if (playerController != null) playerController.enabled = true;
    }

    public void desactivarScript()
    {
        if (playerController != null) playerController.enabled = false;
    }

    public void activarScript()
    {
        if (playerController != null) playerController.enabled = true;
    }
}

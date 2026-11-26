using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pilar verde del tutorial: al tocarlo (desde cualquier lado) lleva al jugador al vestíbulo del
/// laboratorio. Se dispara una sola vez aunque el trigger reciba varios contactos.
/// </summary>
public class ColisionTutorial : MonoBehaviour
{
    [SerializeField] private GameObject controleClick;
    [SerializeField] private GameObject panelAviso;
    [SerializeField] private PlayerController playerController;
    [SerializeField, Min(0f)] private float duracionMensaje = 1.8f;

    private bool usado;

    private void OnTriggerEnter(Collider other)
    {
        if (usado || !other.CompareTag("Player")) return;
        usado = true;

        if (controleClick != null)
        {
            // El mensaje se cierra solo: se ocultan sus botones para no exigir clics.
            foreach (var btn in controleClick.GetComponentsInChildren<Button>(true))
                btn.gameObject.SetActive(false);
            controleClick.SetActive(true);
        }
        if (panelAviso != null) panelAviso.SetActive(false);

        var mov = FindAnyObjectByType<MovimientoInteractivo>();
        if (mov != null) mov.teletransprote();

        if (playerController == null) playerController = other.GetComponent<PlayerController>();

        // La corrutina corre en el jugador porque teletransprote() desactiva este pilar.
        MonoBehaviour anfitrion = playerController != null ? (MonoBehaviour)playerController : this;
        anfitrion.StartCoroutine(OcultarMensaje(controleClick, duracionMensaje));
    }

    private static IEnumerator OcultarMensaje(GameObject mensaje, float segundos)
    {
        yield return new WaitForSeconds(segundos);
        if (mensaje != null) mensaje.SetActive(false);
    }
}

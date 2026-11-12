using UnityEngine;

public class Personaje2DController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Transform targetPosition;

    private void Start()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        if (targetPosition == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) targetPosition = player.transform;
        }
    }

    private void Update()
    {
        if (spriteRenderer != null && targetPosition != null)
        {
            Vector3 direction = targetPosition.position - transform.position;
            // Voltear el sprite en el eje X dependiendo de la posición del jugador
            if (direction.x < 0)
            {
                spriteRenderer.flipX = true;
            }
            else
            {
                spriteRenderer.flipX = false;
            }
        }
    }
}

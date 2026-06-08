using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField]
    private float Yaxis;
    [SerializeField]
    private float Xaxis;
    [SerializeField]
    private float RotationSensitivity = 8.0f;
    [SerializeField]
    private Transform targetPlayer;
    [SerializeField]
    private float RotationMin;
    [SerializeField]
    private float RotationMax;
    [SerializeField]
    private float heightOffset = 1.2f; // Altura a nivel del pecho/cuello para la cámara de tercera persona
    [SerializeField]
    private float defaultDistance = 2.0f; // Distancia por defecto detrás del jugador
    [SerializeField]
    private float sphereRadius = 0.2f; // Radio de la esfera de colisión

    Vector3 targetRotation;
    Vector3 currentVel;
    float smoothTime = 0.12f;
    [SerializeField]
    private bool enabledMobileInputs = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!enabledMobileInputs)
        {
            RotationSensitivity = 9.0f;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (enabledMobileInputs)
        {
            // Yaxis += touchField.TouchDist.x * RotationSensitivity;
            // Xaxis -= touchField.TouchDist.y * RotationSensitivity;
        }
        else
        {
            Yaxis += Input.GetAxis("Mouse X") * RotationSensitivity;
            Xaxis -= Input.GetAxis("Mouse Y") * RotationSensitivity;
        }

        Xaxis = Mathf.Clamp(Xaxis, RotationMin, RotationMax);
        targetRotation = Vector3.SmoothDamp(targetRotation, new Vector3(Xaxis, Yaxis), ref currentVel, smoothTime);
        transform.eulerAngles = targetRotation;

        // --- SISTEMA FÍSICO AUTOMÁTICO PARA EVITAR VER A TRAVÉS DE LAS PAREDES ---
        // Punto de pivote a la altura del cuello/pecho del jugador para una rotación/cámara natural
        Vector3 cameraPivot = targetPlayer.position + Vector3.up * heightOffset;
        
        // Posición ideal deseada de la cámara
        Vector3 desiredPosition = cameraPivot - transform.forward * defaultDistance;

        // Dirección e inclinación desde el pivote hacia la cámara
        Vector3 direction = (desiredPosition - cameraPivot).normalized;
        float maxDistance = Vector3.Distance(cameraPivot, desiredPosition);

        // Excluir dinámicamente la capa del jugador para evitar que la cámara choque consigo misma o parpadee
        int playerLayer = targetPlayer.gameObject.layer;
        int layerMask = ~(1 << playerLayer);

        RaycastHit hit;
        bool hasHit = false;
        float finalDistance = maxDistance;

        // 1. Realizar SphereCast predictivo desde el pivote del jugador hacia la cámara
        if (Physics.SphereCast(cameraPivot, sphereRadius, direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            hasHit = true;
            finalDistance = Mathf.Min(finalDistance, hit.distance);
        }

        // 2. Realizar un Raycast simple de respaldo para capturar bordes afilados o solapamientos iniciales
        if (Physics.Raycast(cameraPivot, direction, out hit, maxDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            hasHit = true;
            // Para el raycast, restamos el radio de la esfera para mantener la cámara alejada de la pared
            finalDistance = Mathf.Min(finalDistance, Mathf.Max(0f, hit.distance - sphereRadius));
        }

        if (hasHit)
        {
            // Mover la cámara delante del obstáculo manteniendo un margen seguro
            transform.position = cameraPivot + direction * Mathf.Max(0.15f, finalDistance - 0.05f);
        }
        else
        {
            // Si el camino está despejado, colocamos la cámara en su posición deseada
            transform.position = desiredPosition;
        }
    }
}

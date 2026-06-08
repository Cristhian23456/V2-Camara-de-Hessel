using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float moveSpeed = 3f;
    [SerializeField]
    private float smoothRotationTime = 0.25f;
    float currentVelocity;

    float currentSpeed;
    float speedVelocity;
    [SerializeField]
    private Transform cameraTransform;
    [SerializeField]
    private bool enabledMobileInputs = false;
   
    private Animator anim;
    private CharacterController characterController;

    private float lastLogTime = 0f;

    private void OnEnable()
    {
        Debug.Log("PlayerController [ON_ENABLE]: El script ha sido activado.");
    }

    private void OnDisable()
    {
        Debug.Log("PlayerController [ON_DISABLE]: El script ha sido desactivado. StackTrace:\n" + System.Environment.StackTrace);
    }

    // Start is called before the first frame update
    void Start() 
    { 
        anim = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        #if UNITY_EDITOR || UNITY_STANDALONE
        enabledMobileInputs = false;
        #endif

        Debug.Log("PlayerController [START]: inicializado. enabledMobileInputs forzado a: " + enabledMobileInputs + ", HasCharacterController: " + (characterController != null));
    }

    void Update()
    {
        Vector2 input = Vector2.zero;
        if (enabledMobileInputs)
        {
            // input = new Vector2(joyStick.Horizontal, joyStick.Vertical);
        }
        else
        {
            input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        }

        // Log diagnóstico periódico si se detecta entrada por teclado
        if (input.magnitude > 0.05f && Time.time - lastLogTime > 0.5f)
        {
            lastLogTime = Time.time;
            Debug.Log("PlayerController [DIAGNOSTICO]: Teclas detectadas = " + input + 
                      ", Velocidad Objetivo = " + (moveSpeed * input.magnitude) + 
                      ", isGrounded = " + (characterController != null ? characterController.isGrounded.ToString() : "N/A"));
        }

        // Detectar si el usuario quiere caminar hacia atrás (tecla S o flecha abajo)
        bool isMovingBackward = input.y < -0.1f;

        Vector2 inputDir = input.normalized;    
        if (inputDir.magnitude > 0)
        {
            if (isMovingBackward)
            {
                // Un velY negativo (-1) en un BlendTree de Unity automáticamente activa la animación de caminar hacia atrás
                if (anim != null) anim.SetFloat("velY", -1f);
            }
            else
            {
                if (anim != null) anim.SetFloat("velY", 1f);
            }
        }
        else
        {
            if (anim != null) anim.SetFloat("velY", 0f);
        }

        if (inputDir != Vector2.zero)
        {
            if (isMovingBackward)
            {
                // Cuando camina hacia atrás, mantiene la vista hacia el frente (dirección de la cámara)
                // y no gira 180 grados, simulando un retroceso real.
                float rotation = cameraTransform.eulerAngles.y;
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, rotation, ref currentVelocity, smoothRotationTime);
            }
            else
            {
                float rotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
                transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, rotation, ref currentVelocity, smoothRotationTime);
            }
        }

        float targetSpeed = moveSpeed * inputDir.magnitude;
        if (isMovingBackward)
        {
            // Caminar hacia atrás suele ser un poco más lento por realismo
            targetSpeed = moveSpeed * 0.6f;
        }

        currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, 0.1f);
        
        // Determinar dirección final del movimiento
        Vector3 moveDirection = transform.forward;
        if (isMovingBackward)
        {
            moveDirection = -transform.forward;
        }

        Vector3 velocity = moveDirection * currentSpeed;

        // Lógica física para evitar traspasar paredes y obstáculos
        if (characterController != null && characterController.enabled)
        {
            // Aplicar gravedad para mantener al personaje en el suelo
            if (!characterController.isGrounded)
            {
                velocity.y += Physics.gravity.y * Time.deltaTime;
            }
            else
            {
                // Un pequeño tirón hacia abajo para mantener al CharacterController pegado al suelo de forma estable
                velocity.y = -2f;
            }
            characterController.Move(velocity * Time.deltaTime);
        }
        else
        {
            // Si no tiene CharacterController, realizamos una verificación de colisión (Physics.Raycast)
            // en la dirección del movimiento para evitar atravesar paredes (Wall clipping protection)
            float checkDistance = currentSpeed * Time.deltaTime + 0.3f;
            Vector3 rayStart = transform.position + Vector3.up * 0.5f; // Lanzar el rayo a media altura (cintura)
            
            // Desactivar temporalmente los colisionadores del propio jugador para evitar auto-colisión
            Collider[] myColliders = GetComponentsInChildren<Collider>();
            foreach (var col in myColliders)
            {
                if (col != null) col.enabled = false;
            }

            bool hitWall = Physics.Raycast(rayStart, moveDirection, out RaycastHit hit, checkDistance);

            foreach (var col in myColliders)
            {
                if (col != null) col.enabled = true;
            }

            if (!hitWall)
            {
                transform.Translate(velocity * Time.deltaTime, Space.World);
            }
            else
            {
                // Detener movimiento si choca con una pared
                currentSpeed = 0f;
            }
        }
    }
}

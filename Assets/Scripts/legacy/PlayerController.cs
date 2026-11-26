using UnityEngine;

/// <summary>
/// Movimiento del jugador en tercera persona.
/// Usa un CharacterController: la cápsula completa choca con paredes y puertas (no solo un
/// rayo a la cintura), se desliza por las esquinas y atraviesa los triggers (pilar, entradas,
/// avisos) en lugar de frenarse contra ellos. El root motion del Animator se ignora para que la
/// animación nunca empuje al personaje a través de una pared.
/// Controles: W/S avanzar/retroceder, A/D girar.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float smoothRotationTime = 0.25f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private bool enabledMobileInputs = false;
    [Tooltip("Factor de velocidad al caminar hacia atrás.")]
    [SerializeField, Range(0.2f, 1f)] private float factorRetroceso = 0.6f;

    private Animator anim;
    private CharacterController characterController;
    private float currentVelocity;
    private float currentSpeed;
    private float speedVelocity;
    private float velocidadVertical;

    private static readonly int VelY = Animator.StringToHash("velY");

    /// <summary>Velocidad horizontal actual (m/s).</summary>
    public float VelocidadActual => currentSpeed;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        if (anim != null) anim.applyRootMotion = false;
        characterController = ConfigurarCharacterController();
    }

    private void Start()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        enabledMobileInputs = false;
#endif
        if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
    }

    private void OnDisable()
    {
        currentSpeed = 0f;
        speedVelocity = 0f;
        if (anim != null && anim.isActiveAndEnabled) anim.SetFloat(VelY, 0f);
    }

    /// <summary>Mueve al jugador a otra posición sin que el CharacterController lo devuelva.</summary>
    public void Teleport(Vector3 posicion, Quaternion? rotacion = null)
    {
        if (characterController == null) characterController = GetComponent<CharacterController>();
        bool estaba = characterController != null && characterController.enabled;
        if (characterController != null) characterController.enabled = false;
        transform.position = posicion;
        if (rotacion.HasValue) transform.rotation = rotacion.Value;
        currentSpeed = 0f;
        velocidadVertical = 0f;
        if (characterController != null) characterController.enabled = estaba;
        Physics.SyncTransforms();
    }

    private void Update()
    {
        Vector2 input = enabledMobileInputs
            ? Vector2.zero
            : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

        bool retrocede = input.y < -0.1f;
        Vector2 dir = input.normalized;
        bool hayEntrada = dir.sqrMagnitude > 0f;

        if (anim != null) anim.SetFloat(VelY, !hayEntrada ? 0f : (retrocede ? -1f : 1f));

        if (hayEntrada && cameraTransform != null)
        {
            // Al retroceder mantiene la vista al frente; al avanzar/girar orienta según la cámara.
            float objetivo = retrocede
                ? cameraTransform.eulerAngles.y
                : Mathf.Atan2(dir.x, dir.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            float y = Mathf.SmoothDampAngle(transform.eulerAngles.y, objetivo, ref currentVelocity, smoothRotationTime);
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        float velocidadObjetivo = retrocede ? moveSpeed * factorRetroceso : moveSpeed * dir.magnitude;
        currentSpeed = Mathf.SmoothDamp(currentSpeed, velocidadObjetivo, ref speedVelocity, 0.1f);

        Vector3 movimiento = (retrocede ? -transform.forward : transform.forward) * currentSpeed;

        if (characterController.isGrounded) velocidadVertical = -2f;           // pegado al suelo
        else velocidadVertical += Physics.gravity.y * Time.deltaTime;
        movimiento.y = velocidadVertical;

        CollisionFlags flags = characterController.Move(movimiento * Time.deltaTime);

        // Si choca de frente contra una pared, frena en lugar de "empujar" la animación.
        if ((flags & CollisionFlags.Sides) != 0 && characterController.velocity.sqrMagnitude < 0.01f)
            currentSpeed = Mathf.Min(currentSpeed, 0.2f);
    }

    /// <summary>
    /// Crea o ajusta el CharacterController a partir de la cápsula que tuviera el jugador y
    /// elimina el Rigidbody/CapsuleCollider antiguos, que peleaban con el movimiento por script.
    /// </summary>
    private CharacterController ConfigurarCharacterController()
    {
        var cc = GetComponent<CharacterController>();
        var capsula = GetComponent<CapsuleCollider>();
        if (capsula != null)
        {
            cc.radius = capsula.radius;
            cc.height = capsula.height;
            cc.center = new Vector3(0f, capsula.center.y, 0f);
            Destroy(capsula);
        }
        var rb = GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        cc.skinWidth = Mathf.Max(0.01f, cc.radius * 0.1f);
        cc.minMoveDistance = 0f;
        cc.stepOffset = Mathf.Min(cc.stepOffset, cc.height * 0.25f);
        return cc;
    }
}

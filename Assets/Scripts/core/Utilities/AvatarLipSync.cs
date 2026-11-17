using UnityEngine;

/// <summary>
/// Mueve la boca de un avatar según el volumen de un AudioSource.
/// Solo reacciona cuando el avatar es el hablante activo (ver <see cref="SetSpeaking"/>),
/// para que dos avatares que comparten el mismo AudioSource no muevan la boca a la vez.
/// El parámetro "hablar" del Animator solo se modifica cuando este componente lo activó
/// por audio, así no anula el SetBool("hablar", true) de los diálogos sin voz.
/// </summary>
public class AvatarLipSync : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.1f, 5f)] private float sensitivity = 1.5f;
    [SerializeField, Range(0f, 100f)] private float maxWeight = 100f;
    [SerializeField, Range(0f, 1f)] private float speakingThreshold = 0.05f;

    [Header("Speaker")]
    [Tooltip("Solo el avatar marcado como hablante mueve la boca con el audio.")]
    [SerializeField] private bool isSpeaking = false;

    [Header("BlendShape Settings")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private int mouthBlendShapeIndex = -1;

    [Header("Fallback Bone Settings")]
    [SerializeField] private Transform jawBone;
    [SerializeField] private Vector3 jawOpenRotation = new Vector3(10f, 0f, 0f);

    private readonly float[] sampleBuffer = new float[256];
    private Animator animator;
    private int hablarBoolHash;
    private bool hasAnimator;
    // true solo si este componente puso hablar=true por audio
    private bool hablarSetByLipSync;
    private bool mouthOpen;

    public bool IsSpeaking => isSpeaking;

    public void SetAudioSource(AudioSource src) => audioSource = src;
    public void SetSensitivity(float sens) => sensitivity = sens;

    /// <summary>Marca (o desmarca) este avatar como el que está hablando.</summary>
    public void SetSpeaking(bool speaking)
    {
        isSpeaking = speaking;
        if (!speaking) ResetMouth();
    }

    private void Awake()
    {
        hablarBoolHash = Animator.StringToHash("hablar");
    }

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        hasAnimator = animator != null;

        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        // Auto-detectar el índice del blendshape de la boca
        if (skinnedMeshRenderer != null && skinnedMeshRenderer.sharedMesh != null && mouthBlendShapeIndex == -1)
        {
            Mesh mesh = skinnedMeshRenderer.sharedMesh;
            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string name = mesh.GetBlendShapeName(i).ToLower();
                if (name.Contains("mouth") && (name.Contains("open") || name.Contains("talk") || name.Contains("boca") || name.Contains("abrir") || name.Contains("aaa") || name.Contains("jaw")))
                {
                    mouthBlendShapeIndex = i;
                    Debug.Log($"[LipSync] Found mouth BlendShape index {i}: '{mesh.GetBlendShapeName(i)}' on {gameObject.name}");
                    break;
                }
            }
        }
    }

    private void Update()
    {
        if (!isSpeaking || audioSource == null || !audioSource.isPlaying)
        {
            ResetMouth();
            return;
        }

        // Volumen en tiempo real del audio
        audioSource.GetOutputData(sampleBuffer, 0);
        float sum = 0f;
        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sum += sampleBuffer[i] * sampleBuffer[i];
        }
        float rms = Mathf.Sqrt(sum / sampleBuffer.Length);
        float volume = Mathf.Clamp01(rms * sensitivity);

        // Solo activa "hablar" por audio; no lo apaga si lo encendió otro script
        if (hasAnimator && volume > speakingThreshold && !hablarSetByLipSync)
        {
            animator.SetBool(hablarBoolHash, true);
            hablarSetByLipSync = true;
        }

        // 1. Boca por BlendShape
        if (skinnedMeshRenderer != null && mouthBlendShapeIndex >= 0)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, volume * maxWeight);
        }
        // 2. Boca por hueso de la mandíbula
        else if (jawBone != null)
        {
            jawBone.localRotation = Quaternion.Euler(jawOpenRotation * volume);
        }
        mouthOpen = true;
    }

    private void ResetMouth()
    {
        // Devuelve hablar=false solo si fue este componente quien lo activó
        if (hasAnimator && hablarSetByLipSync)
        {
            animator.SetBool(hablarBoolHash, false);
            hablarSetByLipSync = false;
        }

        if (!mouthOpen) return;

        if (skinnedMeshRenderer != null && mouthBlendShapeIndex >= 0)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, 0f);
        }
        else if (jawBone != null)
        {
            jawBone.localRotation = Quaternion.identity;
        }
        mouthOpen = false;
    }
}

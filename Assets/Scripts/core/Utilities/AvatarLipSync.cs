using UnityEngine;

public class AvatarLipSync : MonoBehaviour
{
    [Header("Audio Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField, Range(0.1f, 5f)] private float sensitivity = 1.5f;
    [SerializeField, Range(0f, 100f)] private float maxWeight = 100f;
    
    [Header("BlendShape Settings")]
    [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
    [SerializeField] private int mouthBlendShapeIndex = -1;

    [Header("Fallback Bone Settings")]
    [SerializeField] private Transform jawBone;
    [SerializeField] private Vector3 jawOpenRotation = new Vector3(10f, 0f, 0f);
    
    private float[] sampleBuffer = new float[256];
    private Animator animator;
    private int hablarBoolHash;
    private bool hasAnimator = false;

    public void SetAudioSource(AudioSource src) => audioSource = src;
    public void SetSensitivity(float sens) => sensitivity = sens;

    private void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        hasAnimator = animator != null;
        hablarBoolHash = Animator.StringToHash("hablar");

        if (audioSource == null)
        {
            audioSource = FindObjectOfType<AudioSource>();
        }

        if (skinnedMeshRenderer == null)
        {
            skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        }

        // Auto-detect mouth blendshape index
        if (skinnedMeshRenderer != null && mouthBlendShapeIndex == -1)
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
        if (audioSource == null || !audioSource.isPlaying)
        {
            ResetMouth();
            return;
        }

        // Sample real-time audio volume
        audioSource.GetOutputData(sampleBuffer, 0);
        float sum = 0f;
        for (int i = 0; i < sampleBuffer.Length; i++)
        {
            sum += sampleBuffer[i] * sampleBuffer[i];
        }
        float rms = Mathf.Sqrt(sum / sampleBuffer.Length);
        float volume = Mathf.Clamp01(rms * sensitivity);

        // Update Animator speaking state if available
        if (hasAnimator)
        {
            // Set speaking to true if volume is above a small threshold
            animator.SetBool(hablarBoolHash, volume > 0.05f);
        }

        // 1. BlendShape Mouth LipSync
        if (skinnedMeshRenderer != null && mouthBlendShapeIndex >= 0)
        {
            float targetWeight = volume * maxWeight;
            skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, targetWeight);
        }
        // 2. Transform Jaw Bone LipSync
        else if (jawBone != null)
        {
            jawBone.localRotation = Quaternion.Euler(jawOpenRotation * volume);
        }
    }

    private void ResetMouth()
    {
        if (hasAnimator)
        {
            animator.SetBool(hablarBoolHash, false);
        }

        if (skinnedMeshRenderer != null && mouthBlendShapeIndex >= 0)
        {
            skinnedMeshRenderer.SetBlendShapeWeight(mouthBlendShapeIndex, 0f);
        }
        else if (jawBone != null)
        {
            jawBone.localRotation = Quaternion.identity;
        }
    }
}

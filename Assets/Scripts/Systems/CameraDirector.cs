using UnityEngine;
using System.Collections;

public class CameraDirector : MonoBehaviour
{
    public enum CameraView { General, Psicologo, Paciente, Custom }
    
    [Header("Configuración")]
    [SerializeField] private Transform[] cameraTargets;
    [SerializeField, Range(1f, 12f)] private float smoothSpeed = 5f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private Camera mainCamera;

    private CameraView currentView;
    private Coroutine transitionRoutine;

    private void Awake()
    {
        if (mainCamera == null) mainCamera = Camera.main;
    }

    public void SwitchTo(CameraView targetView)
    {
        if (currentView == targetView || targetView >= (CameraView)cameraTargets.Length) return;
        currentView = targetView;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(SmoothTransition(cameraTargets[(int)targetView]));
    }

    private IEnumerator SmoothTransition(Transform target)
    {
        float t = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Quaternion startRot = mainCamera.transform.rotation;
        Vector3 targetPos = target.position;
        Quaternion targetRot = target.rotation;

        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) * smoothSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.transform.rotation = Quaternion.Lerp(startRot, targetRot, t);
            yield return null;
        }
    }
}
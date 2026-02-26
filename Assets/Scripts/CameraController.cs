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
         //   Yaxis += touchField.TouchDist.x * RotationSensitivity;
          //      Xaxis -= touchField.TouchDist.y * RotationSensitivity;
        }
        else
        {
            Yaxis += Input.GetAxis("Mouse X") * RotationSensitivity;
            Xaxis -= Input.GetAxis("Mouse Y") * RotationSensitivity;
        }

        Xaxis = Mathf.Clamp(Xaxis, RotationMin, RotationMax);
        targetRotation = Vector3.SmoothDamp(targetRotation, new Vector3(Xaxis, Yaxis), ref currentVel, smoothTime);
        transform.eulerAngles = targetRotation;
        transform.position = targetPlayer.position - transform.forward * 2.0f;
    }
}

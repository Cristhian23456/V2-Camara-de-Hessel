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
    // Start is called before the first frame update
    void Start() { 
     anim = GetComponent<Animator>();
    }
void Update()
{
    Vector2 input = Vector2.zero;
    if (enabledMobileInputs)
    {
     //   input = new Vector2(joyStick.Horizontal, joyStick.Vertical);

    }
    else
    {
        input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    }



    Vector2 inputDir = input.normalized;    
    if (inputDir.magnitude > 0)
    {
        anim.SetFloat("velY", 1);

    }
    else
    {
        anim.SetFloat("velY", 0);

    }
    if (inputDir != Vector2.zero)
    {
        float rotation = Mathf.Atan2(inputDir.x, inputDir.y) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
        transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, rotation, ref currentVelocity, smoothRotationTime);
    }
    float targetSpeed = moveSpeed * inputDir.magnitude;
    currentSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedVelocity, 0.1f);
    transform.Translate(transform.forward * currentSpeed * Time.deltaTime, Space.World);
}
}

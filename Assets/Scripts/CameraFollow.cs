using UnityEngine;
public class CameraFollow : MonoBehaviour
{
    [Header("Target Settings")]
    public Transform target;

    [Header("Camera Settings")]
    [Range(0.01f, 1f)]
    public float smoothSpeed = 0.125f;
    public Vector3 offset;
    public bool lookAtTarget = true;

    void Start()
    {
        if(target != null)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) 
        return;

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;

        if (lookAtTarget)
        {
            transform.LookAt(target);
        }
    } 
}

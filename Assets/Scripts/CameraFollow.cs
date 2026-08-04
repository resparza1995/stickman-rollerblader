using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target to Follow")]
    public Transform target;

    [Header("Follow Settings")]
    public float smoothTime = 0.25f;
    
    [Header("Axis Control")]
    public bool followY = false;
    public float fixedY = -0.47f;

    [Header("Offset")]
    public float offsetX = 0f;
    public float offsetZ = -10f;

    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        // If fixedY is left default or you want to lock to starting camera Y:
        // You can set fixedY in inspector or use current position Y
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float targetY = followY ? (target.position.y + fixedY) : fixedY;
        Vector3 targetPosition = new Vector3(target.position.x + offsetX, targetY, offsetZ);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}

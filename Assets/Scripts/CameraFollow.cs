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

    [Header("Bounds Control")]
    public bool useBounds = false;
    public SpriteRenderer backgroundBounds;
    public float minX = 0f;
    public float maxX = 100f;

    private Vector3 velocity = Vector3.zero;
    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            cam = Camera.main;
        }

        CalculateBackgroundBounds();
    }

    public void CalculateBackgroundBounds()
    {
        if (backgroundBounds != null && cam != null)
        {
            float cameraHalfWidth = cam.orthographicSize * cam.aspect;
            float bgLeft = backgroundBounds.bounds.min.x;
            float bgRight = backgroundBounds.bounds.max.x;

            minX = bgLeft + cameraHalfWidth;
            maxX = bgRight - cameraHalfWidth;

            if (minX > maxX)
            {
                float center = (bgLeft + bgRight) / 2f;
                minX = center;
                maxX = center;
            }

            useBounds = true;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        float targetY = followY ? (target.position.y + fixedY) : fixedY;
        float targetX = target.position.x + offsetX;

        if (useBounds)
        {
            targetX = Mathf.Clamp(targetX, minX, maxX);
        }

        Vector3 targetPosition = new Vector3(targetX, targetY, offsetZ);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}

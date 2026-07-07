using UnityEngine;
using UnityEngine.InputSystem;

public class ThirdPersonCameraController : MonoBehaviour
{
    [SerializeField] private float distance = 4f;
    [SerializeField] private float heightOffset = 1.5f;
    [SerializeField] private float sensX = 0.1f;
    [SerializeField] private float sensY = 0.1f;
    [SerializeField] private float maxTilt = 60f;
    [SerializeField] private float minTilt = -20f;

    private Transform target;
    private OrientationSync orientationSync;
    private float yaw;
    private float pitch = 15f;

    public void SetTarget(Transform t)
    {
        target = t;
        orientationSync = t.GetComponent<OrientationSync>();
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Remove Time.deltaTime - mouse delta is already per-frame
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        yaw += mouseDelta.x * sensX;
        pitch -= mouseDelta.y * sensY;
        pitch = Mathf.Clamp(pitch, minTilt, maxTilt);

        Vector3 pivotPoint = target.position + Vector3.up * heightOffset;
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 cameraOffset = rotation * new Vector3(0f, 0f, -distance);

        transform.position = pivotPoint + cameraOffset;
        transform.LookAt(pivotPoint);

        if (orientationSync != null)
            orientationSync.SetYaw(yaw);
    }

    public void SetYaw(float newYaw)
    {
        yaw = newYaw;
    }

    public float GetYaw()
    {
        return yaw;
    }
}
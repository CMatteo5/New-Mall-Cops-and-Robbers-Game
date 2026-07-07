using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.InputSystem;

public class OrientationSync : MonoBehaviour
{
    [SerializeField] private Transform orientation;

    private CinemachinePanTilt panTilt;
    private bool isFirstPerson = false;
    private float yRotation = 0f;

    public void Initialize(CinemachinePanTilt assignedPanTilt)
    {
        panTilt = assignedPanTilt;
    }

    public void SetFirstPerson(bool value)
    {
        isFirstPerson = value;
    }

    public void SetYaw(float newYaw)
    {
        yRotation = newYaw;
        if (orientation != null)
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
    }

    private void Start()
    {
        if (orientation == null)
            Debug.LogWarning("OrientationSync: orientation not assigned!");
    }

    private void LateUpdate()
    {
        if (orientation == null) return;

        if (isFirstPerson && panTilt != null)
        {
            // First person only: read from Cinemachine PanTilt
            yRotation = panTilt.PanAxis.Value;
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
        // Third person: do nothing here - ThirdPersonCameraController calls SetYaw directly
    }
}
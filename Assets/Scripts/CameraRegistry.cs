using UnityEngine;
using Unity.Cinemachine;

public class CameraRegistry : MonoBehaviour
{
    public static CinemachineCamera ThirdPersonCamera;
    public static CinemachineCamera FirstPersonCamera;

    // The LOCAL player's OrientationSync, registered explicitly at spawn time.
    // We can't use FindFirstObjectByType<OrientationSync>() here since that's per-player -
    // with more than one player connected it can grab the WRONG player's copy, which is
    // exactly what caused first-person look/rotation to freeze once a second player joined.
    public static OrientationSync LocalOrientationSync;

    [SerializeField] private CinemachineCamera m_ThirdPersonCamera;
    [SerializeField] private CinemachineCamera m_FirstPersonCamera;
    [SerializeField] private CinemachineBrain m_Brain;

    private void OnEnable()
    {
        ThirdPersonCamera = m_ThirdPersonCamera;
        FirstPersonCamera = m_FirstPersonCamera;

        // Start in third person - disable brain, disable both vcams
        if (m_Brain != null) m_Brain.enabled = false;
        if (FirstPersonCamera != null) FirstPersonCamera.Priority = 0;
        if (ThirdPersonCamera != null) ThirdPersonCamera.Priority = 0;

        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        if (tpController != null) tpController.enabled = true;
    }

    public static void SetThirdPerson()
    {
        var registry = FindFirstObjectByType<CameraRegistry>();
        if (registry != null && registry.m_Brain != null)
            registry.m_Brain.enabled = false;

        if (FirstPersonCamera != null) FirstPersonCamera.Priority = 0;
        if (ThirdPersonCamera != null) ThirdPersonCamera.Priority = 0;

        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        var sync = LocalOrientationSync;

        // Carry yaw from first person PanTilt into third person controller
        if (tpController != null && FirstPersonCamera != null)
        {
            var panTilt = FirstPersonCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null)
                tpController.SetYaw(panTilt.PanAxis.Value);
        }

        if (tpController != null) tpController.enabled = true;
        if (sync != null) sync.SetFirstPerson(false);
    }

    public static void SetFirstPerson()
    {
        var registry = FindFirstObjectByType<CameraRegistry>();
        if (registry != null && registry.m_Brain != null)
            registry.m_Brain.enabled = true;

        if (FirstPersonCamera != null) FirstPersonCamera.Priority = 20;
        if (ThirdPersonCamera != null) ThirdPersonCamera.Priority = 0;

        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        var sync = LocalOrientationSync;

        // Carry yaw from third person controller into first person PanTilt
        if (tpController != null && FirstPersonCamera != null)
        {
            var panTilt = FirstPersonCamera.GetComponent<CinemachinePanTilt>();
            if (panTilt != null && tpController != null)
                panTilt.PanAxis.Value = tpController.GetYaw();
        }

        if (tpController != null) tpController.enabled = false;
        if (sync != null) sync.SetFirstPerson(true);
    }

    private void OnDisable()
    {
        ThirdPersonCamera = null;
        FirstPersonCamera = null;
    }
}

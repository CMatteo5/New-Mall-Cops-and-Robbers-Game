using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;
using System.Collections;

public class CustomCLientPlayerMove : NetworkBehaviour
{
    [Header("Network Components")]
    [SerializeField] private PlayerInput m_PlayerInput;

    [Header("Movement Components")]
    [SerializeField] private CustomPlayerMovement m_PlayerMovement;
    [SerializeField] private OrientationSync m_OrientationSync;
    [SerializeField] private CameraToggle m_CameraToggle;

    [Header("Physics")]
    [SerializeField] private Rigidbody m_Rigidbody;

    private void Awake()
    {
        m_PlayerInput.enabled = false;
        m_PlayerMovement.enabled = false;
        m_OrientationSync.enabled = false;
        if (m_CameraToggle != null) m_CameraToggle.enabled = false;

        if (m_Rigidbody != null)
        {
            m_Rigidbody.isKinematic = false; // Keep gravity always on
            m_Rigidbody.freezeRotation = true;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(SetupAfterSpawn());
    }

    private IEnumerator SetupAfterSpawn()
    {
        yield return null;
        //Debug.Log($"SetupAfterSpawn fired - IsOwner: {IsOwner}, IsHost: {IsHost}");

        if (IsOwner)
        {
            //// Null checks debug
            //Debug.Log($"m_PlayerMovement null: {m_PlayerMovement == null}");
            //Debug.Log($"m_PlayerInput null: {m_PlayerInput == null}");
            //Debug.Log($"m_OrientationSync null: {m_OrientationSync == null}");
            //Debug.Log($"m_Rigidbody null: {m_Rigidbody == null}");

            if (m_PlayerInput != null) m_PlayerInput.enabled = true;
            if (m_PlayerMovement != null) m_PlayerMovement.enabled = true;
            if (m_OrientationSync != null) m_OrientationSync.enabled = true;
            if (m_CameraToggle != null) m_CameraToggle.enabled = true;
            if (m_Rigidbody != null) m_Rigidbody.isKinematic = false;

            AssignCameras();
        }
        else
        {
            if (m_Rigidbody != null) m_Rigidbody.isKinematic = true;
        }
    }

    private void AssignCameras()
    {
        // Register OUR OrientationSync as the local one, so CameraRegistry never has to
        // guess which player's copy to use - critical once multiple players are connected,
        // since FindFirstObjectByType<OrientationSync>() could otherwise grab the wrong
        // player's instance and freeze our own first-person look/rotation entirely.
        CameraRegistry.LocalOrientationSync = m_OrientationSync;

        // Third person - use our manual controller instead of Cinemachine procedural
        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        if (tpController != null)
            tpController.SetTarget(transform);
        else
            Debug.LogWarning("No ThirdPersonCameraController found!");

        // First person - still uses Cinemachine
        Transform fpTarget = transform.Find("FirstPersonCameraRoot");
        if (CameraRegistry.FirstPersonCamera != null && fpTarget != null)
            CameraRegistry.FirstPersonCamera.Follow = fpTarget;

        // Init orientation from first person PanTilt
        var panTilt = CameraRegistry.FirstPersonCamera?.GetComponent<CinemachinePanTilt>();
        if (panTilt != null)
            m_OrientationSync.Initialize(panTilt);
    }
}

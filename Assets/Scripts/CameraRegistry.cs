using UnityEngine;
using Unity.Cinemachine;

public class CameraRegistry : MonoBehaviour
{
    public static CinemachineCamera FollowCamera;

    [SerializeField] private CinemachineCamera m_Camera;

    private void OnEnable()
    {
        Debug.Log($"CameraRegistry set: {FollowCamera}");
        FollowCamera = m_Camera;
    }

    private void OnDisable()
    {
        FollowCamera = null;
    }
}

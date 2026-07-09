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
        if (GameState.ShopOpen) return; // pause camera when shop is open

        if (isFirstPerson && panTilt != null)
        {
            yRotation = panTilt.PanAxis.Value;
            orientation.rotation = Quaternion.Euler(0f, yRotation, 0f);
        }
    }
}
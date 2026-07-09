using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using StarterAssets;
using Unity.Cinemachine;
using System.Collections;

public class ClientPlayerMove : NetworkBehaviour
{
    [SerializeField] private PlayerInput m_PlayerInput;
    [SerializeField] private StarterAssetsInputs m_StarterAssetsInputs;
    [SerializeField] private ThirdPersonController m_ThirdPersonController;
    [SerializeField] private Animator m_Animator;

    private void Awake()
    {
        m_StarterAssetsInputs.enabled = false;
        m_PlayerInput.enabled = false;
        m_ThirdPersonController.enabled = false;
        m_Animator.enabled = false;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        StartCoroutine(SetupAfterSpawn());
    }

    private IEnumerator SetupAfterSpawn()
    {
        yield return null; // Wait one frame for NGO to fully settle

        if (IsOwner)
        {
            m_StarterAssetsInputs.enabled = true;
            m_PlayerInput.enabled = true;
            m_ThirdPersonController.enabled = true;
            m_Animator.enabled = true;

            CinemachineCamera vcam = FindFirstObjectByType<CinemachineCamera>();
            if (vcam != null)
                vcam.Follow = transform.Find("PlayerCameraRoot");
            else
                Debug.LogWarning("No CinemachineCamera found in scene!");
        }
        else
        {
            // Non-owner: disable CharacterController so NetworkTransform can move it freely
            GetComponent<CharacterController>().enabled = false;
            m_Animator.enabled = true;
        }
    }
}
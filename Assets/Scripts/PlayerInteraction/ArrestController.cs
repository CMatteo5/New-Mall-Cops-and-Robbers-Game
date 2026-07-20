using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;

public class ArrestController : NetworkBehaviour
{
    [Header("Settings")]
    [SerializeField] private float arrestRange = 2.5f;
    [SerializeField] private float arrestCooldown = 10f;

    [Header("UI Prompt")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private string arrestPromptFormat = "Press R to arrest {0}";
    [SerializeField] private string cooldownPromptFormat = "Arrest on cooldown ({0:F1}s)";

    private float lastArrestTime = -10f;
    private PlayerTeam myTeam;
    private PlayerPickupController pickupController;

    public bool CanArrest => Time.time >= lastArrestTime + arrestCooldown;
    public float CooldownRemaining => Mathf.Max(0f, (lastArrestTime + arrestCooldown) - Time.time);

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        myTeam = GetComponent<PlayerTeam>();
        pickupController = GetComponent<PlayerPickupController>();

        if (promptText == null)
        {
            GameObject promptObj = GameObject.Find("InteractPrompt");
            if (promptObj != null)
                promptText = promptObj.GetComponent<TextMeshProUGUI>();
        }

        HidePrompt();
    }

    private void Update()
    {
        if (!IsOwner) return;
        if (GameState.ShopOpen) return;

        if (myTeam == null || !myTeam.IsCop)
        {
            HidePrompt();
            return;
        }

        PlayerTeam target = FindNearbyRobber();

        if (target != null)
        {
            if (CanArrest)
                ShowPrompt(string.Format(arrestPromptFormat, target.gameObject.name));
            else
                ShowPrompt(string.Format(cooldownPromptFormat, CooldownRemaining));
        }
        else
        {
            HidePrompt();
        }
    }

    public void OnArrest(InputValue value)
    {
        if (!value.isPressed) return;
        if (!IsOwner) return;
        if (myTeam == null || !myTeam.IsCop) return;
        if (GameState.ShopOpen) return;

        PlayerTeam target = FindNearbyRobber();
        if (target != null && CanArrest)
        {
            lastArrestTime = Time.time;
            ArrestPlayerServerRpc(target.NetworkObject.NetworkObjectId);
        }
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        else if (promptText != null) promptText.gameObject.SetActive(true);

        if (promptText != null) promptText.text = message;
    }

    private void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
        else if (promptText != null) promptText.gameObject.SetActive(false);
    }

    private PlayerTeam FindNearbyRobber()
    {
        float closestDist = arrestRange;
        PlayerTeam closest = null;

        foreach (var pt in FindObjectsByType<PlayerTeam>(FindObjectsSortMode.None))
        {
            if (pt == myTeam) continue;
            if (!pt.IsRobber) continue;

            float dist = Vector3.Distance(transform.position, pt.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = pt;
            }
        }

        return closest;
    }

    [ServerRpc]
    private void ArrestPlayerServerRpc(ulong targetNetworkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(targetNetworkObjectId, out NetworkObject target)) return;

        PlayerTeam targetTeam = target.GetComponent<PlayerTeam>();
        if (targetTeam == null || !targetTeam.IsRobber) return;

        PlayerPickupController targetPickup = target.GetComponent<PlayerPickupController>();
        if (targetPickup != null && targetPickup.CurrentItem != null)
            targetPickup.CurrentItem.RequestDropServerRpc(target.transform.position);

        // The arrested player owns their own transform (ClientNetworkTransform is
        // client-authoritative), so a server-side position write gets overwritten
        // by the owner's next sync. Instead, tell ONLY the arrested player's client
        // to move itself — as the owner, its move is authoritative and replicates.
        ArrestController targetArrest = target.GetComponent<ArrestController>();
        if (targetArrest != null)
        {
            targetArrest.TeleportSelfClientRpc(JailManager.Instance.JailPosition,
                new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { target.OwnerClientId }
                    }
                });
        }
    }

    [ClientRpc]
    private void TeleportSelfClientRpc(Vector3 jailPosition,
        ClientRpcParams clientRpcParams = default)
    {
        // Runs on the arrested player's own client. Because this client owns the
        // ClientNetworkTransform, moving transform here is the authoritative move
        // and propagates to everyone else.
        transform.position = jailPosition;
        Debug.Log("Hello from TeleportSelf Client RPC");

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
            rb.linearVelocity = Vector3.zero;
    }
}
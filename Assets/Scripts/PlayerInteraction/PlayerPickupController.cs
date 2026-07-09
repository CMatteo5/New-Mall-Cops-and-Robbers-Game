using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Attach to your networked player prefab. Only runs its interaction logic for the
/// local, owned player - other players' copies of this component do nothing.
/// </summary>
public class PlayerPickupController : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;
    public Transform holdPoint;

    [Header("UI Prompt")]
    public TextMeshProUGUI promptText;
    public GameObject promptPanel;
    public string pickupPromptFormat = "Press E to pick up {0}";
    public string dropPrompt = "Press E to drop";

    [Header("Settings")]
    public float pickupRange = 3f;
    public float dropForwardOffset = 0.5f;
    public LayerMask pickupLayerMask = ~0;
    [Tooltip("How far the raycast itself can reach - keep generous. The actual pickup range is enforced separately below, measured from the player's body.")]
    public float maxRaycastDistance = 50f;

    private Pickupable heldItem;
    private Pickupable lookedAtItem;

    public override void OnNetworkSpawn()
    {
        // Only the local player needs a camera, UI prompt, or input handling at all.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        if (playerCamera == null) playerCamera = Camera.main;

        if (promptText == null)
        {
            GameObject promptObj = GameObject.Find("InteractPrompt");
            if (promptObj != null) promptText = promptObj.GetComponent<TextMeshProUGUI>();
        }
    }

    void Update()
    {
        // Update() only ever runs on the local owning player because OnNetworkSpawn
        // disables this component entirely for everyone else.
        UpdateLookTarget();
        UpdatePrompt();

        bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (!interactPressed) return;

        if (heldItem != null)
        {
            DropItem();
        }
        else if (lookedAtItem != null)
        {
            PickUp(lookedAtItem);
        }
    }

    private void UpdateLookTarget()
    {
        if (heldItem != null)
        {
            lookedAtItem = null;
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, pickupLayerMask))
        {
            Pickupable pickupable = hit.collider.GetComponentInParent<Pickupable>();
            bool withinBodyRange = Vector3.Distance(transform.position, hit.point) <= pickupRange;
            lookedAtItem = (pickupable != null && !pickupable.IsHeld && withinBodyRange) ? pickupable : null;
        }
        else
        {
            lookedAtItem = null;
        }
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;

        string message = null;
        if (heldItem != null) message = dropPrompt;
        else if (lookedAtItem != null) message = string.Format(pickupPromptFormat, lookedAtItem.itemName);

        bool show = message != null;
        if (promptPanel != null) promptPanel.SetActive(show);
        else promptText.gameObject.SetActive(show);

        if (show) promptText.text = message;
    }

    private void PickUp(Pickupable item)
    {
        // Optimistic local tracking so the UI/prompt responds immediately;
        // the actual authoritative state comes back from the server via NetworkVariable.
        heldItem = item;
        lookedAtItem = null;
        item.RequestPickUpServerRpc(NetworkObject.NetworkObjectId);
    }

    private void DropItem()
    {
        Vector3 dropPosition = holdPoint.position + playerCamera.transform.forward * dropForwardOffset;
        heldItem.RequestDropServerRpc(dropPosition);
        heldItem = null;
    }

    public bool IsHoldingItem => heldItem != null;
    public Pickupable CurrentItem => heldItem;
}

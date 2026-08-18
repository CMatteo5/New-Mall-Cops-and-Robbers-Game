using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Attach to your networked player prefab. Only runs its interaction logic for the
/// local, owned player - other players' copies of this component do nothing.
///
/// Looks for a Pickupable in front of the player and, on E, requests it be added to
/// this player's PlayerInventory (see Pickupable.RequestPickUpServerRpc). Items are no
/// longer physically carried in-hand by this controller - the hand visual is driven
/// separately by the currently selected hotbar slot (see PlayerHeldItemVisual).
///
/// Press G to drop the currently SELECTED hotbar item back into the world in front of
/// you (see PlayerInventory.RequestDropSelected) - a separate key from E/pickup so you
/// don't accidentally drop your equipped item while just looking around for something
/// to pick up.
/// </summary>
public class PlayerPickupController : NetworkBehaviour
{
    [Header("References")]
    public Camera playerCamera;

    [Header("UI Prompt")]
    public TextMeshProUGUI promptText;
    public GameObject promptPanel;
    public string pickupPromptFormat = "Press E to pick up {0}";

    [Header("Settings")]
    public float pickupRange = 3f;
    public LayerMask pickupLayerMask = ~0;
    [Tooltip("How far the raycast itself can reach - keep generous. The actual pickup range is enforced separately below, measured from the player's body.")]
    public float maxRaycastDistance = 50f;

    [Header("Drop")]
    [Tooltip("How far in front of the player the dropped item spawns.")]
    public float dropForwardOffset = 1.5f;
    [Tooltip("How far above the player's feet the dropped item spawns, so it doesn't spawn inside the floor.")]
    public float dropHeightOffset = 1f;

    private Pickupable lookedAtItem;
    private PlayerInventory inventory;

    public override void OnNetworkSpawn()
    {
        // Only the local player needs a camera, UI prompt, or input handling at all.
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        if (playerCamera == null) playerCamera = Camera.main;
        inventory = GetComponent<PlayerInventory>();

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

        // Pickup is explicitly allowed during Lobby (free-roam, picking teams) and an
        // active round - only disabled on the Ended win/lose screen. Written as an
        // explicit allow-list (rather than "not Ended") so it stays correct even if
        // more phases get added later.
        bool pickupAllowed = GameTimer.CurrentPhase == GamePhase.Lobby
                           || GameTimer.CurrentPhase == GamePhase.InProgress;
        if (!pickupAllowed)
        {
            HidePrompt();
            return;
        }

        UpdateLookTarget();
        UpdatePrompt();

        bool interactPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        if (interactPressed && lookedAtItem != null)
            PickUp(lookedAtItem);

        bool dropPressed = Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame;
        if (dropPressed)
            DropSelected();
    }

    /// <summary>
    /// Returns a live camera, re-acquiring Camera.main if our cached reference
    /// was destroyed (e.g. by a scene load).
    /// </summary>
    private bool TryGetCamera(out Camera cam)
    {
        if (playerCamera == null) playerCamera = Camera.main;
        cam = playerCamera;
        return cam != null;
    }

    private void UpdateLookTarget()
    {
        if (!TryGetCamera(out Camera cam))
        {
            lookedAtItem = null;
            return;
        }

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, pickupLayerMask))
        {
            Pickupable pickupable = hit.collider.GetComponentInParent<Pickupable>();
            bool withinBodyRange = Vector3.Distance(transform.position, hit.point) <= pickupRange;
            lookedAtItem = (pickupable != null && withinBodyRange) ? pickupable : null;
        }
        else
        {
            lookedAtItem = null;
        }
    }

    private void UpdatePrompt()
    {
        if (promptText == null) return;

        string message = lookedAtItem != null ? string.Format(pickupPromptFormat, lookedAtItem.itemName) : null;

        bool show = message != null;
        if (promptPanel != null) promptPanel.SetActive(show);
        else promptText.gameObject.SetActive(show);

        if (show) promptText.text = message;
    }

    private void HidePrompt()
    {
        if (promptText == null) return;
        if (promptPanel != null) promptPanel.SetActive(false);
        else promptText.gameObject.SetActive(false);
    }

    private void PickUp(Pickupable item)
    {
        lookedAtItem = null;
        item.RequestPickUpServerRpc(NetworkObject.NetworkObjectId);
    }

    private void DropSelected()
    {
        if (inventory == null) return;

        Vector3 forward = TryGetCamera(out Camera cam) ? cam.transform.forward : transform.forward;
        Vector3 dropPosition = transform.position + Vector3.up * dropHeightOffset + forward * dropForwardOffset;
        inventory.RequestDropSelected(dropPosition);
    }
}

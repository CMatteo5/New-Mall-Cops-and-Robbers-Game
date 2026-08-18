using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Put this on a world object with a trigger Collider to let players walk over it and
/// have it go straight into their PlayerInventory (hotbar/storage), instead of being
/// carried in-hand like Pickupable/PlayerPickupController. itemName must match an
/// entry in your ItemRegistry so the UI can show its icon.
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class WorldItemPickup : NetworkBehaviour
{
    [Header("Item Info - itemName must match an ItemRegistry entry")]
    public string itemName = "Item";
    public int quantity = 1;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // server decides pickups, same authority model as SellZone

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        if (inventory.ServerAddItem(itemName, quantity))
            NetworkObject.Despawn(true);
    }
}

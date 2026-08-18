using UnityEngine;
using Unity.Netcode;

/// <summary>
/// A world item that can be picked up (via PlayerPickupController, E key). Picking it
/// up adds it straight into the player's PlayerInventory (hotbar/storage) and despawns
/// this world object - items are no longer physically carried in-hand. What shows in
/// your hand is driven by whichever hotbar slot is currently SELECTED (see
/// PlayerHeldItemVisual), not by whatever you last picked up.
///
/// itemName must match an entry in your ItemRegistry so the inventory UI (icon) and
/// SellZone (sell price) can look it up.
/// </summary>
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
public class Pickupable : NetworkBehaviour
{
    [Header("Item Info - itemName must match an ItemRegistry entry")]
    public string itemName = "Item";
    public int quantity = 1;

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickUpServerRpc(ulong requesterNetworkObjectId)
    {
        if (!IsServer) return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                requesterNetworkObjectId, out NetworkObject requester)) return;

        PlayerInventory inventory = requester.GetComponent<PlayerInventory>();
        if (inventory == null) return;

        if (inventory.ServerAddItem(itemName, quantity))
        {
            NetworkObject.Despawn(true);
        }
        // else: no room in the inventory right now - leave the item where it is so the
        // player can come back for it (or drop something else to make room) later.
    }
}

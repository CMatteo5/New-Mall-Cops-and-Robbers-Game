using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative sell trigger. Credits whichever player most recently
/// dropped the item, not a shared/global pool.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SellZone : NetworkBehaviour
{
    public string requiredItemName = "";

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // only the server decides sales

        Pickupable item = other.GetComponentInParent<Pickupable>();
        if (item == null || item.IsHeld) return;

        if (!string.IsNullOrEmpty(requiredItemName) && item.itemName != requiredItemName)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                item.LastHolderNetworkObjectId, out NetworkObject sellerObject))
        {
            PlayerWallet wallet = sellerObject.GetComponent<PlayerWallet>();
            if (wallet != null)
            {
                wallet.AddMoney(item.sellPrice);
            }
        }

        item.ServerSell();
    }
}

using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative sell trigger. Walk in while carrying a matching item in your
/// PlayerInventory (hotbar or storage, doesn't matter which) and one unit of it sells
/// automatically, crediting YOUR wallet - not a shared/global pool. Sell price comes
/// from the matching ItemRegistry entry's sellPrice field.
/// </summary>
[RequireComponent(typeof(Collider))]
public class SellZone : NetworkBehaviour
{
    [Tooltip("Only this item sells here. Leave blank to sell whatever sellable item the player is carrying first.")]
    public string requiredItemName = "";

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // only the server decides sales

        PlayerInventory inventory = other.GetComponentInParent<PlayerInventory>();
        if (inventory == null) return;

        TrySellMatchingItem(inventory);
    }

    private void TrySellMatchingItem(PlayerInventory inventory)
    {
        string nameToSell = requiredItemName;

        if (string.IsNullOrEmpty(nameToSell))
        {
            nameToSell = FindFirstItemName(inventory);
            if (nameToSell == null) return;
        }
        else if (!inventory.HasItem(nameToSell))
        {
            return;
        }

        int sellPrice = GetSellPrice(inventory, nameToSell);
        int removed = inventory.ServerRemoveItem(nameToSell, 1);
        if (removed <= 0) return;

        PlayerWallet wallet = inventory.GetComponent<PlayerWallet>();
        if (wallet != null)
            wallet.AddMoney(sellPrice * removed);
    }

    private string FindFirstItemName(PlayerInventory inventory)
    {
        for (int i = 0; i < inventory.HotbarSize; i++)
        {
            InventorySlotData slot = inventory.GetHotbarSlot(i);
            if (!slot.IsEmpty) return slot.itemName.ToString();
        }
        for (int i = 0; i < inventory.StorageSize; i++)
        {
            InventorySlotData slot = inventory.GetStorageSlot(i);
            if (!slot.IsEmpty) return slot.itemName.ToString();
        }
        return null;
    }

    private int GetSellPrice(PlayerInventory inventory, string itemName)
    {
        if (inventory.Registry == null || inventory.Registry.items == null) return 0;

        foreach (ItemData item in inventory.Registry.items)
            if (item != null && item.itemName == itemName) return item.sellPrice;

        return 0;
    }
}

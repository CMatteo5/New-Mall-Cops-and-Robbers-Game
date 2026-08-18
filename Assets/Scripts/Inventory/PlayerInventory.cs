using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative multi-slot inventory: a hotbar (quick-access, number-key
/// selectable) plus separate backpack storage, Minecraft-style. Slots hold items by
/// name, resolved against an ItemRegistry for icon lookups in the UI. Attach to the
/// player prefab alongside PlayerTeam / PlayerWallet.
/// </summary>
public class PlayerInventory : NetworkBehaviour
{
    /// <summary>The local player's own inventory, so UI can reach it without a search.</summary>
    public static PlayerInventory Local { get; private set; }

    [Header("Setup")]
    [SerializeField] private ItemRegistry itemRegistry;
    [SerializeField] private int hotbarSize = 5;
    [SerializeField] private int storageSize = 20;

    // Initialized inline (not in Awake) - NetworkVariable-derived fields must exist
    // before OnNetworkSpawn runs.
    private readonly NetworkList<InventorySlotData> hotbar = new NetworkList<InventorySlotData>();
    private readonly NetworkList<InventorySlotData> storage = new NetworkList<InventorySlotData>();

    private readonly NetworkVariable<int> selectedHotbarIndex = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public int HotbarSize => hotbarSize;
    public int StorageSize => storageSize;
    public int SelectedHotbarIndex => selectedHotbarIndex.Value;
    public ItemRegistry Registry => itemRegistry;

    /// <summary>Fired on the owning client whenever a slot changes, so UI can refresh.</summary>
    public event System.Action OnInventoryChanged;

    public override void OnNetworkSpawn()
    {
        if (IsOwner) Local = this;

        if (IsServer)
        {
            for (int i = 0; i < hotbarSize; i++) hotbar.Add(InventorySlotData.Empty);
            for (int i = 0; i < storageSize; i++) storage.Add(InventorySlotData.Empty);
        }

        hotbar.OnListChanged += _ => OnInventoryChanged?.Invoke();
        storage.OnListChanged += _ => OnInventoryChanged?.Invoke();
        selectedHotbarIndex.OnValueChanged += (_, __) => OnInventoryChanged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsOwner && Local == this) Local = null;
    }

    public InventorySlotData GetHotbarSlot(int index) =>
        index >= 0 && index < hotbar.Count ? hotbar[index] : InventorySlotData.Empty;

    public InventorySlotData GetStorageSlot(int index) =>
        index >= 0 && index < storage.Count ? storage[index] : InventorySlotData.Empty;

    public InventorySlotData GetSelectedItem() => GetHotbarSlot(selectedHotbarIndex.Value);

    /// <summary>True if this player has at least one of the given item anywhere (hotbar or storage).</summary>
    public bool HasItem(string itemName)
    {
        for (int i = 0; i < hotbar.Count; i++)
            if (!hotbar[i].IsEmpty && hotbar[i].itemName.ToString() == itemName) return true;
        for (int i = 0; i < storage.Count; i++)
            if (!storage[i].IsEmpty && storage[i].itemName.ToString() == itemName) return true;
        return false;
    }

    public void SelectHotbarSlot(int index)
    {
        if (!IsOwner) return;
        SelectHotbarSlotServerRpc(index);
    }

    [ServerRpc]
    private void SelectHotbarSlotServerRpc(int index)
    {
        if (index < 0 || index >= hotbar.Count) return;
        selectedHotbarIndex.Value = index;
    }

    /// <summary>
    /// Server-only: adds an item, stacking onto matching slots first (hotbar then
    /// storage), then filling empty slots (hotbar then storage). Returns false if
    /// there wasn't room for all of it (whatever didn't fit is simply not added).
    /// </summary>
    public bool ServerAddItem(string itemName, int quantity = 1)
    {
        if (!IsServer) return false;

        TryStackInto(hotbar, itemName, ref quantity);
        if (quantity <= 0) return true;
        TryStackInto(storage, itemName, ref quantity);
        if (quantity <= 0) return true;

        TryFillEmpty(hotbar, itemName, ref quantity);
        if (quantity <= 0) return true;
        TryFillEmpty(storage, itemName, ref quantity);

        return quantity <= 0;
    }

    private void TryStackInto(NetworkList<InventorySlotData> list, string itemName, ref int quantity)
    {
        for (int i = 0; i < list.Count && quantity > 0; i++)
        {
            InventorySlotData slot = list[i];
            if (slot.IsEmpty || slot.itemName.ToString() != itemName) continue;

            slot.quantity += quantity;
            quantity = 0;
            list[i] = slot;
        }
    }

    private void TryFillEmpty(NetworkList<InventorySlotData> list, string itemName, ref int quantity)
    {
        for (int i = 0; i < list.Count && quantity > 0; i++)
        {
            if (!list[i].IsEmpty) continue;

            list[i] = new InventorySlotData { itemName = itemName, quantity = quantity };
            quantity = 0;
        }
    }

    /// <summary>Server-only: removes up to `quantity` of an item, hotbar first. Returns how many were actually removed.</summary>
    public int ServerRemoveItem(string itemName, int quantity = 1)
    {
        if (!IsServer) return 0;
        int removed = RemoveFrom(hotbar, itemName, quantity);
        removed += RemoveFrom(storage, itemName, quantity - removed);
        return removed;
    }

    private int RemoveFrom(NetworkList<InventorySlotData> list, string itemName, int quantity)
    {
        int removed = 0;
        for (int i = 0; i < list.Count && removed < quantity; i++)
        {
            InventorySlotData slot = list[i];
            if (slot.IsEmpty || slot.itemName.ToString() != itemName) continue;

            int take = Mathf.Min(slot.quantity, quantity - removed);
            slot.quantity -= take;
            removed += take;
            list[i] = slot.quantity <= 0 ? InventorySlotData.Empty : slot;
        }
        return removed;
    }

    /// <summary>
    /// Client call: drop ONE unit of the currently SELECTED hotbar item back into the
    /// world at the given position, spawning a fresh pickup there (using the matching
    /// ItemRegistry entry's prefab) so it can be picked back up later.
    /// </summary>
    public void RequestDropSelected(Vector3 dropPosition)
    {
        if (!IsOwner) return;
        RequestDropSelectedServerRpc(dropPosition);
    }

    [ServerRpc]
    private void RequestDropSelectedServerRpc(Vector3 dropPosition)
    {
        int index = selectedHotbarIndex.Value;
        if (index < 0 || index >= hotbar.Count) return;

        InventorySlotData slot = hotbar[index];
        if (slot.IsEmpty) return;

        string droppedItemName = slot.itemName.ToString();

        slot.quantity -= 1;
        hotbar[index] = slot.quantity <= 0 ? InventorySlotData.Empty : slot;

        SpawnDroppedItem(droppedItemName, dropPosition);
    }

    private void SpawnDroppedItem(string itemNameToSpawn, Vector3 dropPosition)
    {
        if (itemRegistry == null || itemRegistry.items == null) return;

        ItemData data = null;
        foreach (ItemData item in itemRegistry.items)
        {
            if (item != null && item.itemName == itemNameToSpawn) { data = item; break; }
        }

        if (data == null || data.prefab == null) return;

        GameObject spawned = Instantiate(data.prefab, dropPosition, Quaternion.identity);
        NetworkObject netObj = spawned.GetComponent<NetworkObject>();
        if (netObj != null) netObj.Spawn(true);
    }

    /// <summary>Client call: move/swap a slot between hotbar and storage (or within the same list).</summary>
    public void RequestMoveSlot(bool fromHotbar, int fromIndex, bool toHotbar, int toIndex)
    {
        if (!IsOwner) return;
        RequestMoveSlotServerRpc(fromHotbar, fromIndex, toHotbar, toIndex);
    }

    [ServerRpc]
    private void RequestMoveSlotServerRpc(bool fromHotbar, int fromIndex, bool toHotbar, int toIndex)
    {
        NetworkList<InventorySlotData> from = fromHotbar ? hotbar : storage;
        NetworkList<InventorySlotData> to = toHotbar ? hotbar : storage;

        if (fromIndex < 0 || fromIndex >= from.Count) return;
        if (toIndex < 0 || toIndex >= to.Count) return;

        InventorySlotData temp = from[fromIndex];
        from[fromIndex] = to[toIndex];
        to[toIndex] = temp;
    }
}

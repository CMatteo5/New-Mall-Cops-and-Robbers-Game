using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// One inventory slot: which item (by name, matching an ItemData.itemName entry in
/// your ItemRegistry) and how many. An empty itemName/zero quantity means the slot
/// is empty.
/// </summary>
public struct InventorySlotData : INetworkSerializable, System.IEquatable<InventorySlotData>
{
    public FixedString64Bytes itemName;
    public int quantity;

    public bool IsEmpty => itemName.IsEmpty || quantity <= 0;

    public static InventorySlotData Empty => new InventorySlotData { itemName = default, quantity = 0 };

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref itemName);
        serializer.SerializeValue(ref quantity);
    }

    public bool Equals(InventorySlotData other) =>
        itemName.Equals(other.itemName) && quantity == other.quantity;
}

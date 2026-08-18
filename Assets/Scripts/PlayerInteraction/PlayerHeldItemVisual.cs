using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Purely cosmetic - runs for EVERY player (not just the local owner), so everyone
/// sees what everyone else has equipped, not just yourself. Shows a clientside copy of
/// the ItemData.prefab for whichever item sits in the player's CURRENTLY SELECTED
/// hotbar slot, parented to their HoldPointMarker. Nothing else is ever shown in-hand -
/// picking up more items or storing things in the backpack doesn't change what's
/// visible until you actually select that hotbar slot (number keys 1-9).
///
/// Attach to the player prefab alongside PlayerInventory. Requires an ItemRegistry to
/// be assigned on PlayerInventory (for the itemName -> prefab lookup), and a
/// HoldPointMarker somewhere in the player's hierarchy.
/// </summary>
public class PlayerHeldItemVisual : NetworkBehaviour
{
    private PlayerInventory inventory;
    private Transform holdPoint;

    private GameObject currentVisual;
    private string currentVisualItemName;

    public override void OnNetworkSpawn()
    {
        inventory = GetComponent<PlayerInventory>();

        HoldPointMarker marker = GetComponentInChildren<HoldPointMarker>(true);
        holdPoint = marker != null ? marker.transform : null;

        if (inventory != null)
            inventory.OnInventoryChanged += Refresh;

        Refresh();
    }

    public override void OnNetworkDespawn()
    {
        if (inventory != null)
            inventory.OnInventoryChanged -= Refresh;

        ClearVisual();
    }

    private void Refresh()
    {
        if (inventory == null || holdPoint == null) return;

        InventorySlotData selected = inventory.GetSelectedItem();

        if (selected.IsEmpty)
        {
            ClearVisual();
            return;
        }

        string itemName = selected.itemName.ToString();
        if (itemName == currentVisualItemName && currentVisual != null) return; // already showing it

        ClearVisual();

        ItemData data = FindItemData(itemName);
        if (data == null || data.prefab == null) return;

        // Deliberately do NOT Instantiate the actual item prefab. It carries
        // NetworkObject/Rigidbody/Collider/Pickupable/OwnerNetworkTransform/
        // NetworkRigidbody - a whole web of NetworkBehaviours with RequireComponent
        // dependencies on each other (e.g. NetworkRigidbody requires
        // OwnerNetworkTransform), and this visual is never spawned through
        // NetworkManager. Trying to strip those components after the fact ran into
        // Unity refusing to destroy one because another still depended on it, which
        // froze Play mode. Instead, just copy the mesh + materials onto a brand new
        // plain GameObject - no networked or physics components ever get created in
        // the first place, so there's nothing to strip and nothing that can throw.
        MeshFilter sourceMeshFilter = data.prefab.GetComponentInChildren<MeshFilter>(true);
        MeshRenderer sourceMeshRenderer = data.prefab.GetComponentInChildren<MeshRenderer>(true);
        if (sourceMeshFilter == null || sourceMeshRenderer == null || sourceMeshFilter.sharedMesh == null)
            return;

        Transform sourceMeshTransform = sourceMeshFilter.transform;
        Transform prefabRoot = data.prefab.transform;
        bool meshIsOnRoot = sourceMeshTransform == prefabRoot;

        GameObject visualRoot = new GameObject("HeldItemVisual (" + itemName + ")");
        visualRoot.transform.SetParent(holdPoint, false);
        visualRoot.transform.localPosition = Vector3.zero;
        visualRoot.transform.localRotation = Quaternion.identity;
        visualRoot.transform.localScale = Vector3.one;

        GameObject meshObj = new GameObject("Mesh");
        meshObj.transform.SetParent(visualRoot.transform, false);
        // If the mesh sits on the prefab's own root, its "local" transform there is
        // just wherever that object happened to be saved in the world/scene, not a
        // meaningful offset - skip it and use identity. If the mesh is on a child of
        // the prefab, that child's local transform IS meaningful (offset within the
        // item), so preserve it.
        meshObj.transform.localPosition = meshIsOnRoot ? Vector3.zero : sourceMeshTransform.localPosition;
        meshObj.transform.localRotation = meshIsOnRoot ? Quaternion.identity : sourceMeshTransform.localRotation;
        meshObj.transform.localScale = sourceMeshTransform.localScale;

        MeshFilter mf = meshObj.AddComponent<MeshFilter>();
        mf.sharedMesh = sourceMeshFilter.sharedMesh;

        MeshRenderer mr = meshObj.AddComponent<MeshRenderer>();
        mr.sharedMaterials = sourceMeshRenderer.sharedMaterials;

        currentVisual = visualRoot;
        currentVisualItemName = itemName;
    }

    private void ClearVisual()
    {
        if (currentVisual != null) Destroy(currentVisual);
        currentVisual = null;
        currentVisualItemName = null;
    }

    private ItemData FindItemData(string itemNameToFind)
    {
        if (inventory.Registry == null || inventory.Registry.items == null) return null;

        foreach (ItemData item in inventory.Registry.items)
            if (item != null && item.itemName == itemNameToFind) return item;

        return null;
    }
}

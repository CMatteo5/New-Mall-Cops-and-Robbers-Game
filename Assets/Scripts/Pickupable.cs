using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Networked pickup item. While held, ownership transfers to the holding player,
/// who drives the item's position locally (zero-latency for them) via
/// OwnerNetworkTransform, which then syncs that movement to everyone else.
/// While dropped, ownership reverts to the server, which simulates physics normally.
///
/// Held/holder state is tracked with a SINGLE NetworkVariable (0 = not held,
/// otherwise the holder's NetworkObjectId) rather than two separate variables,
/// since separate NetworkVariables are not guaranteed to arrive/apply in the
/// order they were set on the server - that race caused the item to freeze
/// on remote clients in earlier testing.
///
/// Requires: Rigidbody, Collider, NetworkObject, OwnerNetworkTransform on this GameObject.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(OwnerNetworkTransform))]
public class Pickupable : NetworkBehaviour
{
    [Header("Item Info")]
    public string itemName = "Item";
    public int sellPrice = 10;

    // 0 = not held. Any other value = the NetworkObjectId of the holding player.
    private readonly NetworkVariable<ulong> holderNetworkObjectId = new NetworkVariable<ulong>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsHeld => holderNetworkObjectId.Value != 0;

    /// <summary>Server-only: who most recently dropped this item, so SellZone knows who to pay.</summary>
    public ulong LastHolderNetworkObjectId { get; private set; }

    private Rigidbody rb;
    private Collider col;
    private Transform followTarget; // resolved locally by whoever currently owns this item

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
    }

    public override void OnNetworkSpawn()
    {
        rb.isKinematic = !IsOwner;

        holderNetworkObjectId.OnValueChanged += OnHolderChanged;
        ApplyHeldState(holderNetworkObjectId.Value);
    }

    public override void OnNetworkDespawn()
    {
        holderNetworkObjectId.OnValueChanged -= OnHolderChanged;
    }

    void Update()
    {
        // Only the current owner ever moves the item directly.
        // When held, owner = the holding player (instant, local movement).
        // When dropped, owner = the server (normal physics simulation).
        if (IsOwner && IsHeld && followTarget != null)
        {
            transform.SetPositionAndRotation(followTarget.position, followTarget.rotation);
        }
    }

    private void OnHolderChanged(ulong oldValue, ulong newValue) => ApplyHeldState(newValue);

    private void ApplyHeldState(ulong holderId)
    {
        followTarget = null;

        if (holderId != 0 && NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                holderId, out NetworkObject holder))
        {
            followTarget = holder.GetComponentInChildren<HoldPointMarker>(true)?.transform;
            col.enabled = false; // avoid physically colliding with the player/world while carried
        }
        else
        {
            col.enabled = true;
        }

        rb.isKinematic = !IsOwner || holderId != 0;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestPickUpServerRpc(ulong requesterNetworkObjectId)
    {
        if (holderNetworkObjectId.Value != 0) return; // already held

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                requesterNetworkObjectId, out NetworkObject holder))
        {
            NetworkObject.ChangeOwnership(holder.OwnerClientId);
        }

        holderNetworkObjectId.Value = requesterNetworkObjectId;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestDropServerRpc(Vector3 dropPosition)
    {
        if (holderNetworkObjectId.Value == 0) return;

        LastHolderNetworkObjectId = holderNetworkObjectId.Value; // remember who dropped it before clearing
        holderNetworkObjectId.Value = 0;
        transform.position = dropPosition;

        NetworkObject.RemoveOwnership(); // hands authority back to the server for physics
    }

    /// <summary>Server-only: called by SellZone to remove the item for everyone.</summary>
    public void ServerSell()
    {
        if (!IsServer) return;
        NetworkObject.Despawn(true);
    }
}

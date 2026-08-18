using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Owner-authoritative "teleport myself" endpoint used by SpawnManager and
/// LobbyManager. Team switching itself happens only by walking into a TeamZone
/// trigger in the world (see TeamZone.cs), which calls LobbyManager.TryAssignTeam
/// directly - that call also triggers this teleport, so this component doesn't need
/// its own team-request methods. Attach to the player prefab alongside PlayerTeam.
/// </summary>
[RequireComponent(typeof(PlayerTeam))]
public class TeamSelector : NetworkBehaviour
{
    /// <summary>Server-only. Moves THIS player to the given world position.</summary>
    public void ServerTeleportTo(Vector3 position)
    {
        if (!IsServer) return;
        TeleportSelfClientRpc(position, new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { OwnerClientId } }
        });
    }

    [ClientRpc]
    private void TeleportSelfClientRpc(Vector3 position, ClientRpcParams clientRpcParams = default)
    {
        transform.position = position;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic) rb.linearVelocity = Vector3.zero;
    }
}

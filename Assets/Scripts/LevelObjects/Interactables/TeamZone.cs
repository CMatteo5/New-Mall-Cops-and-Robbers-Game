using Unity.Netcode;
using UnityEngine;

public class TeamZone : NetworkBehaviour
{
    [SerializeField] private PlayerTeams teamType;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        RequestTeamServerRpc(netObj.NetworkObjectId);
    }

    // OnTriggerExit removed - team persists after leaving zone

    [ServerRpc(RequireOwnership = false)]
    private void RequestTeamServerRpc(ulong networkObjectId)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects
            .TryGetValue(networkObjectId, out NetworkObject player)) return;

        bool success = LobbyManager.Instance.TryAssignTeam(player, teamType);

        if (!success)
            NotifyFullClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { player.OwnerClientId }
                }
            });
    }

    [ClientRpc]
    private void NotifyFullClientRpc(ClientRpcParams clientRpcParams = default)
    {
        Debug.Log("Cops team is full!");
        // Hook into your UI to show a message to the player
    }
}
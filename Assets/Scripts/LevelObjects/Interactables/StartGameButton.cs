using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Physical "Start Game" button placed in the world - replaces the old UI Start
/// button entirely. Only the HOST's own player triggers it; anyone else walking
/// through it does nothing. Even then, it only actually starts the match once every
/// connected player has picked Cop or Robber - see GameTimer.TryStartGame for the
/// real gating logic, this component just forwards the attempt.
///
/// Requires a trigger Collider on this GameObject (Reset() sets that up for you if
/// you add a Collider via the usual "Add Component" flow).
/// </summary>
[RequireComponent(typeof(NetworkObject))]
public class StartGameButton : NetworkBehaviour
{
    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // server decides, same authority model as other trigger zones

        NetworkObject netObj = other.GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        // Only the host's own player can trigger this - everyone else walking
        // through it does nothing. This code only ever runs on the host machine
        // (IsServer is only true there), so LocalClientId here unambiguously means
        // "the host's own client id."
        if (netObj.OwnerClientId != NetworkManager.Singleton.LocalClientId) return;

        GameTimer.Instance?.TryStartGame();
    }
}

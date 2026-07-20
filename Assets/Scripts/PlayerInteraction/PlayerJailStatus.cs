using Unity.Netcode;

/// <summary>
/// Tracks whether this player is currently "in jail". Attach to your player prefab.
/// Whatever system arrests/frees a player (police collision, a jail trigger zone,
/// etc) should call SetInJail server-side to update this.
/// </summary>
public class PlayerJailStatus : NetworkBehaviour
{
    private readonly NetworkVariable<bool> inJail = new NetworkVariable<bool>(
        false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool IsInJail => inJail.Value;

    /// <summary>Server-only.</summary>
    public void SetInJail(bool value)
    {
        if (!IsServer) return;
        inJail.Value = value;
    }
}

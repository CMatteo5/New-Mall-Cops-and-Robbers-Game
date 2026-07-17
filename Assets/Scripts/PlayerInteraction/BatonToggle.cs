using UnityEngine;
using Unity.Netcode;

public class BatonToggle : NetworkBehaviour
{
    [SerializeField] private GameObject baton;

    private PlayerTeam playerTeam;

    public override void OnNetworkSpawn()
    {
        // Run on EVERY client, not just the owner. The baton's visibility is
        // driven by the networked Team value (readable by everyone), so each
        // client can independently show/hide this player's baton. Keying off
        // IsOwner was the bug: non-owners never ran UpdateBaton, so the baton
        // sat at its prefab default (visible) for all other players.
        playerTeam = GetComponent<PlayerTeam>();

        if (playerTeam != null)
            playerTeam.Team.OnValueChanged += OnTeamChanged;

        UpdateBaton();
    }

    public override void OnNetworkDespawn()
    {
        if (playerTeam != null)
            playerTeam.Team.OnValueChanged -= OnTeamChanged;
    }

    private void OnTeamChanged(PlayerTeams oldTeam, PlayerTeams newTeam)
    {
        UpdateBaton();
    }

    private void UpdateBaton()
    {
        if (baton != null)
            baton.SetActive(playerTeam != null && playerTeam.IsCop);
    }
}
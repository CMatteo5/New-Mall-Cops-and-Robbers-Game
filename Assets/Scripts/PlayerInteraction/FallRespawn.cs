using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-side safety net: if a player falls below fallThresholdY (off the edge of
/// the map, through a gap, etc.), teleport them back to a valid spawn point instead
/// of falling forever. During an active round this sends them to their team's spawn;
/// otherwise (Lobby/Ended) it sends them to a default spawn point, since team spawns
/// aren't "official" until the host actually starts the game. Attach to the player
/// prefab alongside PlayerTeam and TeamSelector.
/// </summary>
[RequireComponent(typeof(TeamSelector))]
[RequireComponent(typeof(PlayerTeam))]
public class FallRespawn : NetworkBehaviour
{
    [Tooltip("If the player's Y position drops below this, they get teleported back to a spawn point.")]
    [SerializeField] private float fallThresholdY = -20f;

    private TeamSelector teamSelector;
    private PlayerTeam playerTeam;

    public override void OnNetworkSpawn()
    {
        teamSelector = GetComponent<TeamSelector>();
        playerTeam = GetComponent<PlayerTeam>();
    }

    private void Update()
    {
        if (!IsServer) return;
        if (transform.position.y >= fallThresholdY) return;

        if (SpawnManager.Instance == null) return;

        PlayerTeams teamForSpawn = GameTimer.CurrentPhase == GamePhase.InProgress && playerTeam != null
            ? playerTeam.Team.Value
            : PlayerTeams.None; // None -> SpawnManager falls back to a default spawn point

        teamSelector.ServerTeleportTo(SpawnManager.Instance.GetSpawnPointForTeam(teamForSpawn));
    }
}

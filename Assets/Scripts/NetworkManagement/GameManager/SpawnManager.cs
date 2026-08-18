using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Server-authoritative spawn point picker. Drag empty GameObjects placed around your
/// scene into the Cop / Robber / Default arrays in the Inspector - those are the
/// settable spawn locations. Attach this to the same persistent networked GameObject
/// as GameTimer / LobbyManager (it needs a NetworkObject).
/// </summary>
public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance { get; private set; }

    [Header("Cop team spawn points")]
    [SerializeField] private Transform[] copSpawnPoints;

    [Header("Robber team spawn points")]
    [SerializeField] private Transform[] robberSpawnPoints;

    [Header("Default spawn points (used before a player picks a team)")]
    [SerializeField] private Transform[] defaultSpawnPoints;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;
        if (!NetworkManager.Singleton.ConnectedClients.TryGetValue(clientId, out var client)) return;

        NetworkObject playerObject = client.PlayerObject;
        if (playerObject == null) return;

        TeamSelector ts = playerObject.GetComponent<TeamSelector>();
        if (ts != null) ts.ServerTeleportTo(RandomPoint(defaultSpawnPoints));
    }

    /// <summary>
    /// Server-only: move one player to their team's spawn area. Called automatically by
    /// TeamSelector right after a successful team assignment.
    /// </summary>
    public void RespawnPlayer(NetworkObject playerObject, PlayerTeams team)
    {
        if (!IsServer || playerObject == null) return;
        TeamSelector ts = playerObject.GetComponent<TeamSelector>();
        if (ts != null) ts.ServerTeleportTo(GetSpawnPointForTeam(team));
    }

    /// <summary>
    /// Server-only: teleport every connected player to their current team's spawn point.
    /// Handy to call at round start/restart (see GameTimer).
    /// </summary>
    public void RespawnAllPlayers()
    {
        if (!IsServer) return;
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            PlayerTeam pt = playerObject.GetComponent<PlayerTeam>();
            RespawnPlayer(playerObject, pt != null ? pt.Team.Value : PlayerTeams.None);
        }
    }

    public Vector3 GetSpawnPointForTeam(PlayerTeams team)
    {
        return team switch
        {
            PlayerTeams.Cop => RandomPoint(copSpawnPoints),
            PlayerTeams.Robber => RandomPoint(robberSpawnPoints),
            _ => RandomPoint(defaultSpawnPoints)
        };
    }

    private Vector3 RandomPoint(Transform[] points)
    {
        if (points == null || points.Length == 0) return transform.position;
        return points[Random.Range(0, points.Length)].position;
    }
}

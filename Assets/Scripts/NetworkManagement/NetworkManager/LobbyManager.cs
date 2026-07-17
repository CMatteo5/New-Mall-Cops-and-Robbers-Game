using Unity.Netcode;
using UnityEngine;


public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance;

    public int CopCount { get; private set; }
    public int RobberCount { get; private set; }
    public int PlayerCount { get; private set; }

    //[SerializeField] private string gameSceneName = "DevRoom";

    private bool IsServer => NetworkManager.Singleton != null &&
                             NetworkManager.Singleton.IsServer;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (!IsServer) return;
        PlayerCount++;
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (!IsServer) return;
        PlayerCount--;

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null) return;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (obj == null || obj.OwnerClientId != clientId) continue;

            PlayerTeam pt = obj.GetComponent<PlayerTeam>();
            if (pt == null) continue;

            if (pt.Team.Value == PlayerTeams.Cop) CopCount--;
            else if (pt.Team.Value == PlayerTeams.Robber) RobberCount--;
            pt.Team.Value = PlayerTeams.None;
        }
    }

    public int MaxCops => Mathf.FloorToInt(PlayerCount / 2f);
    public bool CanJoinCops => CopCount < MaxCops;

    public bool TryAssignTeam(NetworkObject player, PlayerTeams requestedTeam)
    {
        if (!IsServer) return false;

        PlayerTeam pt = player.GetComponent<PlayerTeam>();
        if (pt == null) return false;

        if (pt.Team.Value == PlayerTeams.Cop) CopCount--;
        else if (pt.Team.Value == PlayerTeams.Robber) RobberCount--;

        if (requestedTeam == PlayerTeams.Cop && !CanJoinCops)
        {
            if (pt.Team.Value == PlayerTeams.Cop) CopCount++;
            else if (pt.Team.Value == PlayerTeams.Robber) RobberCount++;
            return false;
        }

        pt.Team.Value = requestedTeam;
        if (requestedTeam == PlayerTeams.Cop) CopCount++;
        else if (requestedTeam == PlayerTeams.Robber) RobberCount++;

        return true;
    }

    public void LeaveTeam(NetworkObject player)
    {
        if (!IsServer) return;

        PlayerTeam pt = player.GetComponent<PlayerTeam>();
        if (pt == null) return;

        if (pt.Team.Value == PlayerTeams.Cop) CopCount--;
        else if (pt.Team.Value == PlayerTeams.Robber) RobberCount--;

        pt.Team.Value = PlayerTeams.None;
    }

    /// <summary>
    /// Called by GameTimer when a round starts and an unassigned player is
    /// dropped onto the Robbers, so the team count stays accurate.
    /// Server-only.
    /// </summary>
    public void RegisterRobber()
    {
        if (!IsServer) return;
        RobberCount++;
    }
}
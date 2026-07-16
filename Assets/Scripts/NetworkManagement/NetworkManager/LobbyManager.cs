// LobbyManager.cs
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    public NetworkVariable<int> CopCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> RobberCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<int> PlayerCount = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private string gameSceneName = "DevRoom";

    private void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
    }

    private void OnPlayerConnected(ulong clientId)
    {
        PlayerCount.Value++;
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        PlayerCount.Value--;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (obj.OwnerClientId == clientId)
            {
                PlayerTeam pt = obj.GetComponent<PlayerTeam>();
                if (pt != null)
                {
                    if (pt.Team.Value == PlayerTeams.Cop) CopCount.Value--;
                    else if (pt.Team.Value == PlayerTeams.Robber) RobberCount.Value--;
                    pt.Team.Value = PlayerTeams.None;
                }
            }
        }
    }

    public int MaxCops => Mathf.FloorToInt(PlayerCount.Value / 2f);
    public bool CanJoinCops => CopCount.Value < MaxCops;

    public bool TryAssignTeam(NetworkObject player, PlayerTeams requestedTeam)
    {
        if (!IsServer) return false;

        PlayerTeam pt = player.GetComponent<PlayerTeam>();
        if (pt == null) return false;

        if (pt.Team.Value == PlayerTeams.Cop) CopCount.Value--;
        else if (pt.Team.Value == PlayerTeams.Robber) RobberCount.Value--;

        if (requestedTeam == PlayerTeams.Cop && !CanJoinCops)
        {
            if (pt.Team.Value == PlayerTeams.Cop) CopCount.Value++;
            else if (pt.Team.Value == PlayerTeams.Robber) RobberCount.Value++;
            return false;
        }

        pt.Team.Value = requestedTeam;
        if (requestedTeam == PlayerTeams.Cop) CopCount.Value++;
        else if (requestedTeam == PlayerTeams.Robber) RobberCount.Value++;

        return true;
    }

    public void LeaveTeam(NetworkObject player)
    {
        if (!IsServer) return;

        PlayerTeam pt = player.GetComponent<PlayerTeam>();
        if (pt == null) return;

        if (pt.Team.Value == PlayerTeams.Cop) CopCount.Value--;
        else if (pt.Team.Value == PlayerTeams.Robber) RobberCount.Value--;

        pt.Team.Value = PlayerTeams.None;
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGameServerRpc()
    {
        if (!IsServer) return;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            PlayerTeam pt = obj.GetComponent<PlayerTeam>();
            if (pt != null && pt.Team.Value == PlayerTeams.None)
            {
                pt.Team.Value = PlayerTeams.Robber;
                RobberCount.Value++;
            }
        }

        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName, LoadSceneMode.Single);
    }
}
using Unity.Netcode;

public class PlayerTeam : NetworkBehaviour
{
    public NetworkVariable<PlayerTeams> Team = new NetworkVariable<PlayerTeams>(
        PlayerTeams.None,
        NetworkVariableReadPermission.Everyone,  // all clients can read
        NetworkVariableWritePermission.Server    // only server assigns teams
    );

    public bool IsCop => Team.Value == PlayerTeams.Cop;
    public bool IsRobber => Team.Value == PlayerTeams.Robber;
}
using UnityEngine;
using Unity.Netcode;

public class BatonToggle : NetworkBehaviour
{
    [SerializeField] private GameObject baton;

    private PlayerTeam playerTeam;

    public override void OnNetworkSpawn()
    {
        // PlayerTeam lives on the player ROOT, but this component may sit on a
        // child (the baton object). GetComponentInParent walks up the hierarchy
        // so it's found regardless of where BatonToggle is attached.
        playerTeam = GetComponentInParent<PlayerTeam>();

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

    private void Update()
    {
        UpdateBaton();
    }

    private void UpdateBaton()
    {
        if (baton != null)
            baton.SetActive(playerTeam != null && playerTeam.IsCop);
    }
}
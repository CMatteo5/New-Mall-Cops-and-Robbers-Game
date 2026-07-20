using Unity.Netcode;
using UnityEngine;

public class TeamWall : NetworkBehaviour
{
    [SerializeField] private PlayerTeams teamThatCanPassThrough;

    private Collider wallCollider;

    private void Awake()
    {
        wallCollider = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        PlayerTeam pt = other.GetComponent<PlayerTeam>();
        if (pt == null) return;

        if (pt.Team.Value == teamThatCanPassThrough)
            Physics.IgnoreCollision(other.GetComponent<Collider>(), wallCollider, true);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        NetworkObject netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsOwner) return;

        PlayerTeam pt = other.GetComponent<PlayerTeam>();
        if (pt == null) return;

        if (pt.Team.Value == teamThatCanPassThrough)
            Physics.IgnoreCollision(other.GetComponent<Collider>(), wallCollider, false);
    }
}
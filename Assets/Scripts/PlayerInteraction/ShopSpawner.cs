using UnityEngine;
using Unity.Netcode;

public class ShopSpawner : NetworkBehaviour
{
    [SerializeField] private ItemRegistry itemRegistry;

    public void RequestSpawn(string itemName, Vector3 position)
    {
        if (!IsOwner) return;
        SpawnItemServerRpc(itemName, position);
    }

    [ServerRpc]
    private void SpawnItemServerRpc(string itemName, Vector3 position)
    {
        ItemData item = null;
        foreach (var i in itemRegistry.items)
        {
            if (i.itemName == itemName)
            {
                item = i;
                break;
            }
        }

        if (item == null || item.prefab == null)
        {
            Debug.LogWarning($"ShopSpawner: Could not find item '{itemName}'");
            return;
        }

        GameObject spawned = Instantiate(item.prefab, position, Quaternion.identity);
        spawned.GetComponent<NetworkObject>().Spawn();
    }
}
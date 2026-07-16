using UnityEngine;
using UnityEngine.UI;

public class ShopPurchaseItem : MonoBehaviour
{
    private Button buttonA;
    [SerializeField] public ItemData purchaseItem;

    private void Start()
    {
        buttonA = GetComponent<Button>();
        buttonA.onClick.AddListener(OnPurchaseClicked);
    }

    private void OnPurchaseClicked()
    {
        // Find at click time instead of Start - ownership is guaranteed to be set by now
        PlayerWallet pw = null;
        PlayerCanBuy playerCanBuy = null;
        ShopSpawner spawner = null;

        foreach (var wallet in FindObjectsByType<PlayerWallet>(FindObjectsSortMode.None))
        {
            if (wallet.IsOwner)
            {
                pw = wallet;
                playerCanBuy = wallet.GetComponent<PlayerCanBuy>();
                spawner = wallet.GetComponent<ShopSpawner>();
                break;
            }
        }

        if (pw == null)
        {
            Debug.LogWarning("ShopPurchaseItem: Could not find local PlayerWallet!");
            return;
        }

        if (playerCanBuy == null || playerCanBuy.lastLocation == null)
        {
            Debug.LogWarning("ShopPurchaseItem: No spawn location set!");
            return;
        }

        if (!pw.CanAfford(purchaseItem.cost))
        {
            Debug.Log("Not enough money!");
            return;
        }

        pw.subtractMoney(purchaseItem.cost);

        if (spawner != null)
            spawner.RequestSpawn(purchaseItem.itemName, playerCanBuy.lastLocation.position);
        else
            Debug.LogWarning("ShopPurchaseItem: Could not find local ShopSpawner!");
    }
}
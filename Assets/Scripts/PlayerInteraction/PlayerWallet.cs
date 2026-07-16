using UnityEngine;
using Unity.Netcode;
using TMPro;

public class PlayerWallet : NetworkBehaviour
{
    private readonly NetworkVariable<int> money = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    [Header("UI Display")]
    public string moneyFormat = "Money: ${0}";
    private TextMeshProUGUI moneyText;
    [SerializeField] public TextMeshProUGUI shopText;

    public int Money => money.Value;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        GameObject textObj = GameObject.Find("MoneyText");
        if (textObj != null)
            moneyText = textObj.GetComponent<TextMeshProUGUI>();

        money.OnValueChanged += (oldVal, newVal) => UpdateDisplay();
        UpdateDisplay();
    }

    public void AddMoney(int amount)
    {
        if (!IsServer) return;
        money.Value += amount;
    }

    // Called by client - routes to server
    public void subtractMoney(int amount)
    {
        if (IsServer)
        {
            money.Value -= amount;
        }
        else
        {
            SubtractMoneyServerRpc(amount);
        }
    }

    [ServerRpc]
    private void SubtractMoneyServerRpc(int amount)
    {
        money.Value -= amount;
    }

    public bool CanAfford(int amount) => money.Value >= amount;

    private void UpdateDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = string.Format(moneyFormat, money.Value);
            if (shopText != null)
                shopText.text = string.Format(moneyFormat, money.Value);
        }
    }
}
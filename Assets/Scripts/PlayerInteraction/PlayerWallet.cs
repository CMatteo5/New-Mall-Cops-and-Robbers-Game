using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Per-player money. Attach to your player prefab. Each player has their own
/// server-authoritative balance, visible only to themselves (and the server) -
/// other players can't see how much you have.
/// </summary>
public class PlayerWallet : NetworkBehaviour
{
    private readonly NetworkVariable<int> money = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Owner, NetworkVariableWritePermission.Server);

    [Header("UI Display")]
    public string moneyFormat = "Money: ${0}";

    private TextMeshProUGUI moneyText;

    public override void OnNetworkSpawn()
    {
        // Only the local owner needs to display/track their own wallet UI.
        if (!IsOwner) return;

        GameObject textObj = GameObject.Find("MoneyText");
        if (textObj != null)
        {
            moneyText = textObj.GetComponent<TextMeshProUGUI>();
        }

        money.OnValueChanged += (oldVal, newVal) => UpdateDisplay();
        UpdateDisplay();
    }

    /// <summary>Server-only.</summary>
    public void AddMoney(int amount)
    {
        if (!IsServer) return;
        money.Value += amount;
    }

    private void UpdateDisplay()
    {
        if (moneyText != null)
        {
            moneyText.text = string.Format(moneyFormat, money.Value);
        }
    }
}

using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Networked singleton tracking shared money. Server writes, everyone reads and sees
/// the same live-updating total. Attach to a persistent scene object with a NetworkObject.
/// </summary>
public class CurrencyManager : NetworkBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    private readonly NetworkVariable<int> currentMoney = new NetworkVariable<int>(
        0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("UI Display")]
    public TextMeshProUGUI moneyText;
    public string moneyFormat = "Money: ${0}";

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        currentMoney.OnValueChanged += (oldVal, newVal) => UpdateMoneyDisplay();
        UpdateMoneyDisplay();
    }

    /// <summary>Server-only.</summary>
    public void AddMoney(int amount)
    {
        if (!IsServer) return;
        currentMoney.Value += amount;
    }

    private void UpdateMoneyDisplay()
    {
        if (moneyText != null)
            moneyText.text = string.Format(moneyFormat, currentMoney.Value);
    }
}

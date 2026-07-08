using UnityEngine;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Server-authoritative countdown timer, visible to all connected players.
/// When it reaches zero, the server checks every connected player's
/// PlayerJailStatus - if nobody is in jail, the players win; if anyone is,
/// they lose. Result is shown to everyone via a win/lose panel.
///
/// Attach to a persistent networked scene object (e.g. your GameManager,
/// which needs a NetworkObject component).
/// </summary>
public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Countdown length in seconds (e.g. 300 = 5 minutes).")]
    public float startingTime = 300f;

    private readonly NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private bool timerEnded;

    [Header("UI References (found automatically by name in the scene)")]
    public string timerTextObjectName = "TimerText";
    public string winLosePanelObjectName = "WinLosePanel";
    public string resultTextObjectName = "ResultText";

    private TextMeshProUGUI timerText;
    private GameObject winLosePanel;
    private TextMeshProUGUI resultText;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            timeRemaining.Value = startingTime;
            timerEnded = false;
        }

        // Every player (host and clients) needs to see the timer and the result screen.
        GameObject timerObj = GameObject.Find(timerTextObjectName);
        if (timerObj != null) timerText = timerObj.GetComponent<TextMeshProUGUI>();

        winLosePanel = GameObject.Find(winLosePanelObjectName);
        if (winLosePanel != null)
        {
            winLosePanel.SetActive(false);
            Transform resultTransform = winLosePanel.transform.Find(resultTextObjectName);
            if (resultTransform != null) resultText = resultTransform.GetComponent<TextMeshProUGUI>();
        }

        timeRemaining.OnValueChanged += (oldVal, newVal) => UpdateTimerDisplay(newVal);
        UpdateTimerDisplay(timeRemaining.Value);
    }

    void Update()
    {
        if (!IsServer || timerEnded) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
            timerEnded = true;
            EvaluateWinLose();
        }
    }

    private void UpdateTimerDisplay(float value)
    {
        if (timerText == null) return;

        int minutes = Mathf.FloorToInt(Mathf.Max(0f, value) / 60f);
        int seconds = Mathf.FloorToInt(Mathf.Max(0f, value) % 60f);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    /// <summary>Server-only: checks every connected player's jail status.</summary>
    private void EvaluateWinLose()
    {
        if (!IsServer) return;

        bool playersWin = true;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            PlayerJailStatus jailStatus = playerObject.GetComponent<PlayerJailStatus>();
            if (jailStatus != null && jailStatus.IsInJail)
            {
                playersWin = false;
                break;
            }
        }

        ShowResultClientRpc(playersWin);
    }

    [ClientRpc]
    private void ShowResultClientRpc(bool playersWin)
    {
        if (winLosePanel != null)
        {
            winLosePanel.SetActive(true);
        }
        if (resultText != null)
        {
            resultText.text = playersWin ? "You Win!" : "You Lose!";
        }
    }
}

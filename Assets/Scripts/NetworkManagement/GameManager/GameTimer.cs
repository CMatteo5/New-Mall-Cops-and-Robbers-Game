using UnityEngine;
using Unity.Netcode;
using Unity.Cinemachine;
using TMPro;

public enum GamePhase
{
    Lobby,       // players spawn at the default spawn point, can move/look around freely,
                 // pick teams; no timer running, no win/lose evaluated
    InProgress,  // round timer running, arrest/jail rules active
    Ended        // win/lose screen shown
}

/// <summary>
/// Server-authoritative game flow: lobby (free movement, team picking, no timer) ->
/// countdown -> win/lose screen -> restart. The round is started by a physical
/// StartGameButton object in the world (see StartGameButton.cs) instead of a UI
/// button - only the host's own player can trigger it, and only once every connected
/// player has picked Cop or Robber. Attach to a persistent networked scene object
/// (e.g. your GameManager).
/// </summary>
public class GameTimer : NetworkBehaviour
{
    public static GameTimer Instance { get; private set; }

    [Header("Timer Settings")]
    public float startingTime = 300f;

    private readonly NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<GamePhase> phase = new NetworkVariable<GamePhase>(
        GamePhase.Lobby, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Timer UI")]
    public string timerTextObjectName = "TimerText";

    [Header("Win/Lose Screen UI")]
    public string winLosePanelObjectName = "WinLosePanel";
    public string resultTextObjectName = "ResultText";
    public string restartButtonObjectName = "RestartButton";
    public string waitingForRestartTextObjectName = "WaitingForRestartText";

    [Header("Crosshair")]
    public string crosshairObjectName = "DefaultCrosshair";

    // Lets other scripts (like CustomPlayerMovement) check the current phase
    // without needing a direct reference to this NetworkBehaviour instance.
    public static GamePhase CurrentPhase { get; private set; } = GamePhase.Lobby;

    private TextMeshProUGUI timerText;

    private GameObject winLosePanel;
    private TextMeshProUGUI resultText;
    private UnityEngine.UI.Button restartButton;
    private GameObject waitingForRestartTextObj;

    private GameObject crosshair;

    private void Awake() => Instance = this;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            phase.Value = GamePhase.Lobby;
            timeRemaining.Value = startingTime;
        }

        FindUI();

        phase.OnValueChanged += (oldVal, newVal) => UpdatePhaseUI(newVal);
        timeRemaining.OnValueChanged += (oldVal, newVal) => UpdateTimerDisplay(newVal);

        UpdatePhaseUI(phase.Value);
        UpdateTimerDisplay(timeRemaining.Value);

        if (restartButton != null) restartButton.onClick.AddListener(OnRestartButtonPressed);
    }

    private void FindUI()
    {
        GameObject timerObj = GameObject.Find(timerTextObjectName);
        if (timerObj != null) timerText = timerObj.GetComponent<TextMeshProUGUI>();

        winLosePanel = GameObject.Find(winLosePanelObjectName);
        if (winLosePanel != null)
        {
            Transform resultT = winLosePanel.transform.Find(resultTextObjectName);
            if (resultT != null) resultText = resultT.GetComponent<TextMeshProUGUI>();

            Transform restartT = winLosePanel.transform.Find(restartButtonObjectName);
            if (restartT != null) restartButton = restartT.GetComponent<UnityEngine.UI.Button>();

            Transform waitRestartT = winLosePanel.transform.Find(waitingForRestartTextObjectName);
            if (waitRestartT != null) waitingForRestartTextObj = waitRestartT.gameObject;
        }

        crosshair = GameObject.Find(crosshairObjectName);
    }

    private void UpdatePhaseUI(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        if (winLosePanel != null)
            winLosePanel.SetActive(newPhase == GamePhase.Ended);

        if (restartButton != null)
            restartButton.gameObject.SetActive(isHost);

        if (waitingForRestartTextObj != null)
            waitingForRestartTextObj.SetActive(!isHost);

        // Crosshair makes sense any time the player is actually moving around - lobby
        // included now, not just an active round.
        bool controlsActive = newPhase == GamePhase.Lobby || newPhase == GamePhase.InProgress;

        if (crosshair != null)
            crosshair.SetActive(controlsActive);

        if (CameraRegistry.FirstPersonCamera != null)
        {
            var axisController = CameraRegistry.FirstPersonCamera.GetComponent<CinemachineInputAxisController>();
            if (axisController != null) axisController.enabled = controlsActive;
        }

        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        if (tpController != null) tpController.enabled = controlsActive;

        // Cursor is locked for character control during both Lobby and an active round,
        // and freed on the win/lose screen so the Restart button is clickable. (Escape
        // can still temporarily free the cursor mid-round via ServerLobbyUI, regardless
        // of this.)
        if (controlsActive)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Update()
    {
        if (!IsServer || phase.Value != GamePhase.InProgress) return;

        timeRemaining.Value -= Time.deltaTime;

        if (timeRemaining.Value <= 0f)
        {
            timeRemaining.Value = 0f;
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

    /// <summary>
    /// Called by StartGameButton when the host's own player touches it. Host-only,
    /// double-checked server-side, and only actually starts once every connected
    /// player has picked Cop or Robber.
    /// </summary>
    public void TryStartGame()
    {
        if (!IsServer || phase.Value != GamePhase.Lobby) return;
        if (!AllPlayersHaveChosenTeam()) return;

        // Send everyone to their team's spawn points for the round.
        SpawnManager.Instance?.RespawnAllPlayers();

        timeRemaining.Value = startingTime;
        phase.Value = GamePhase.InProgress;
    }

    /// <summary>
    /// Server-only: true only once there's at least one connected player and every
    /// connected player has a team other than None.
    /// </summary>
    private bool AllPlayersHaveChosenTeam()
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 0) return false;

        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            PlayerTeam pt = playerObject.GetComponent<PlayerTeam>();
            if (pt != null && pt.Team.Value == PlayerTeams.None) return false;
        }

        return true;
    }

    /// <summary>Wired to the Restart button. Host-only, double-checked server-side.</summary>
    public void OnRestartButtonPressed()
    {
        if (!IsServer || phase.Value != GamePhase.Ended) return;

        // Reset every connected player's jail status for the new round.
        foreach (NetworkClient client in NetworkManager.Singleton.ConnectedClientsList)
        {
            NetworkObject playerObject = client.PlayerObject;
            if (playerObject == null) continue;

            PlayerJailStatus jailStatus = playerObject.GetComponent<PlayerJailStatus>();
            if (jailStatus != null) jailStatus.SetInJail(false);
        }

        // Send everyone to their team's spawn points for the new round.
        SpawnManager.Instance?.RespawnAllPlayers();

        timeRemaining.Value = startingTime;
        phase.Value = GamePhase.InProgress;
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

        phase.Value = GamePhase.Ended;
        ShowResultClientRpc(playersWin);
    }

    [ClientRpc]
    private void ShowResultClientRpc(bool playersWin)
    {
        if (resultText != null)
        {
            resultText.text = playersWin ? "You Win!" : "You Lose!";
        }
    }
}

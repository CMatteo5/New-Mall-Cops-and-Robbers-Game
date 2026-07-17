using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Cinemachine;
using TMPro;

public enum GamePhase
{
    WaitingToStart,
    InProgress,
    Ended
}

/// <summary>
/// Server-authoritative game flow: start screen -> countdown -> win/lose screen -> restart.
/// Only the host can start or restart the game; other players see waiting screens instead.
/// Attach to a persistent networked scene object (e.g. your GameManager).
/// </summary>
public class GameTimer : NetworkBehaviour
{
    [Header("Timer Settings")]
    public float startingTime = 300f;

    private readonly NetworkVariable<float> timeRemaining = new NetworkVariable<float>(
        0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<GamePhase> phase = new NetworkVariable<GamePhase>(
        GamePhase.WaitingToStart, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [Header("Timer UI")]
    public string timerTextObjectName = "TimerText";

    [Header("Start Screen UI")]
    public string startScreenPanelObjectName = "StartScreenPanel";
    public string startGameButtonObjectName = "StartGameButton";
    public string waitingForHostTextObjectName = "WaitingForHostText";

    [Header("Win/Lose Screen UI")]
    public string winLosePanelObjectName = "WinLosePanel";
    public string resultTextObjectName = "ResultText";
    public string restartButtonObjectName = "RestartButton";
    public string waitingForRestartTextObjectName = "WaitingForRestartText";

    [Header("Crosshair")]
    public string crosshairObjectName = "DefaultCrosshair";

    // Lets other scripts (like CustomPlayerMovement) check the current phase
    // without needing a direct reference to this NetworkBehaviour instance.
    public static GamePhase CurrentPhase { get; private set; } = GamePhase.WaitingToStart;

    private TextMeshProUGUI timerText;

    private GameObject startScreenPanel;
    private Button startGameButton;
    private GameObject waitingForHostTextObj;

    private GameObject winLosePanel;
    private TextMeshProUGUI resultText;
    private Button restartButton;
    private GameObject waitingForRestartTextObj;

    private GameObject crosshair;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            phase.Value = GamePhase.WaitingToStart;
            timeRemaining.Value = startingTime;
        }

        FindUI();

        phase.OnValueChanged += (oldVal, newVal) => UpdatePhaseUI(newVal);
        timeRemaining.OnValueChanged += (oldVal, newVal) => UpdateTimerDisplay(newVal);

        UpdatePhaseUI(phase.Value);
        UpdateTimerDisplay(timeRemaining.Value);

        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartButtonPressed);
        if (restartButton != null) restartButton.onClick.AddListener(OnRestartButtonPressed);
    }

    private void FindUI()
    {
        GameObject timerObj = GameObject.Find(timerTextObjectName);
        if (timerObj != null) timerText = timerObj.GetComponent<TextMeshProUGUI>();

        startScreenPanel = GameObject.Find(startScreenPanelObjectName);
        if (startScreenPanel != null)
        {
            Transform btnT = startScreenPanel.transform.Find(startGameButtonObjectName);
            if (btnT != null) startGameButton = btnT.GetComponent<Button>();

            Transform waitT = startScreenPanel.transform.Find(waitingForHostTextObjectName);
            if (waitT != null) waitingForHostTextObj = waitT.gameObject;
        }

        winLosePanel = GameObject.Find(winLosePanelObjectName);
        if (winLosePanel != null)
        {
            Transform resultT = winLosePanel.transform.Find(resultTextObjectName);
            if (resultT != null) resultText = resultT.GetComponent<TextMeshProUGUI>();

            Transform restartT = winLosePanel.transform.Find(restartButtonObjectName);
            if (restartT != null) restartButton = restartT.GetComponent<Button>();

            Transform waitRestartT = winLosePanel.transform.Find(waitingForRestartTextObjectName);
            if (waitRestartT != null) waitingForRestartTextObj = waitRestartT.gameObject;
        }

        crosshair = GameObject.Find(crosshairObjectName);
    }

    private void UpdatePhaseUI(GamePhase newPhase)
    {
        CurrentPhase = newPhase;

        bool isHost = NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

        if (startScreenPanel != null)
            startScreenPanel.SetActive(newPhase == GamePhase.WaitingToStart);

        if (startGameButton != null)
            startGameButton.gameObject.SetActive(isHost);

        if (waitingForHostTextObj != null)
            waitingForHostTextObj.SetActive(!isHost);

        if (winLosePanel != null)
            winLosePanel.SetActive(newPhase == GamePhase.Ended);

        if (restartButton != null)
            restartButton.gameObject.SetActive(isHost);

        if (waitingForRestartTextObj != null)
            waitingForRestartTextObj.SetActive(!isHost);

        // Crosshair only makes sense during actual gameplay.
        if (crosshair != null)
            crosshair.SetActive(newPhase == GamePhase.InProgress);

        // Disable camera look input entirely outside of active gameplay,
        // so the mouse can't rotate the player's view on the start/win/lose screens.
        bool gameplayActive = newPhase == GamePhase.InProgress;

        if (CameraRegistry.FirstPersonCamera != null)
        {
            var axisController = CameraRegistry.FirstPersonCamera.GetComponent<CinemachineInputAxisController>();
            if (axisController != null) axisController.enabled = gameplayActive;
        }

        var tpController = FindFirstObjectByType<ThirdPersonCameraController>();
        if (tpController != null) tpController.enabled = gameplayActive;

        // Cursor should be free to click UI during the start/end screens,
        // and locked for normal gameplay once the round is in progress.
        if (newPhase == GamePhase.InProgress)
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

    /// <summary>Wired to the Start Game button. Host-only, double-checked server-side.</summary>
    public void OnStartButtonPressed()
    {
        if (!IsServer || phase.Value != GamePhase.WaitingToStart) return;

        // Any player who never picked a team is dropped onto the Robbers
        // when the round begins (mirrors the old LobbyManager.StartGame logic).
        AssignUnassignedPlayersToRobbers();

        timeRemaining.Value = startingTime;
        phase.Value = GamePhase.InProgress;
    }

    /// <summary>Server-only: everyone still on team None becomes a Robber at round start.</summary>
    private void AssignUnassignedPlayersToRobbers()
    {
        if (!IsServer) return;

        foreach (var obj in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            PlayerTeam pt = obj.GetComponent<PlayerTeam>();
            if (pt != null && pt.Team.Value == PlayerTeams.None)
            {
                pt.Team.Value = PlayerTeams.Robber;
                if (LobbyManager.Instance != null) LobbyManager.Instance.RegisterRobber();
            }
        }
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

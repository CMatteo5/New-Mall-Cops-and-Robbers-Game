using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;

/// <summary>
/// Pre-connection lobby menu, plus the always-available "connected" controls.
///
/// Host starts this machine as server+client, listening on the configured port.
/// Join connects to the typed IP/port. Once connected, the menu hides itself.
///
/// LobbyInfoText is a separate, independently-shown element (NOT nested inside
/// ConnectedPanel in the Hierarchy) that stays visible the whole time you're
/// connected and still picking teams, and hides itself once the match starts.
///
/// ConnectedPanel (Disconnect + Close) opens automatically the first time you
/// connect. From then on, Close hides it, and pressing Escape re-opens it -
/// which also frees the cursor even during gameplay, since GameTimer normally
/// locks/hides the cursor while a match is InProgress. Closing it again (or
/// pressing Escape while it's open) hands cursor control back to GameTimer's
/// normal gameplay locking.
/// </summary>
public class ServerLobbyUI : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private TMP_InputField ipInputField;
    [SerializeField] private TMP_InputField portInputField;
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [Header("Lobby Info (always shown while waiting to start)")]
    [SerializeField] private TextMeshProUGUI lobbyInfoText;

    [Header("Connected Panel (toggle with Close / Escape)")]
    [SerializeField] private GameObject connectedPanel;
    [SerializeField] private Button disconnectButton;
    [SerializeField] private Button closeButton;

    [Header("Defaults")]
    [SerializeField] private string defaultPort = "7777";

    private bool hasEverConnected;

    private void Awake()
    {
        if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
        if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
        if (disconnectButton != null) disconnectButton.onClick.AddListener(OnDisconnectClicked);
        if (closeButton != null) closeButton.onClick.AddListener(() => SetConnectedPanelOpen(false));
        if (portInputField != null && string.IsNullOrEmpty(portInputField.text))
            portInputField.text = defaultPort;
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnDisconnected;
            NetworkManager.Singleton.OnServerStarted += OnServerStarted;
        }
        RefreshView();
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnDisconnected;
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }

    private void Update()
    {
        HandleEscapeToggle();
        UpdateLobbyInfoText();
    }

    private bool IsConnected =>
        NetworkManager.Singleton != null &&
        (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

    private void HandleEscapeToggle()
    {
        if (!IsConnected || connectedPanel == null) return;
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;

        SetConnectedPanelOpen(!connectedPanel.activeSelf);
    }

    private void SetConnectedPanelOpen(bool open)
    {
        if (connectedPanel != null) connectedPanel.SetActive(open);

        if (open)
        {
            // Free the cursor so Disconnect/Close are actually clickable, even if
            // GameTimer currently has it locked for character control.
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (GameTimer.CurrentPhase == GamePhase.Lobby || GameTimer.CurrentPhase == GamePhase.InProgress)
        {
            // Hand cursor control back to normal gameplay locking - GameTimer treats
            // both Lobby and an active round as "controls active."
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            // Clear any lingering UI selection (e.g. the Close button you just
            // clicked) so a stray "selected" UI element can't intercept input.
            if (UnityEngine.EventSystems.EventSystem.current != null)
                UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void UpdateLobbyInfoText()
    {
        if (lobbyInfoText == null) return;

        bool show = IsConnected && GameTimer.CurrentPhase == GamePhase.Lobby;
        lobbyInfoText.gameObject.SetActive(show);

        if (show && LobbyManager.Instance != null)
        {
            lobbyInfoText.text = $"Players: {LobbyManager.Instance.PlayerCount}   " +
                                  $"Cops: {LobbyManager.Instance.CopCount}   " +
                                  $"Robbers: {LobbyManager.Instance.RobberCount}";
        }
    }

    private void OnHostClicked()
    {
        if (!ApplyPort()) return;

        SetStatus("Starting host...");
        bool started = NetworkManager.Singleton.StartHost();
        if (!started) SetStatus("Failed to start host - is the port already in use?");
    }

    private void OnJoinClicked()
    {
        if (!ApplyPort()) return;

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        string ip = string.IsNullOrWhiteSpace(ipInputField != null ? ipInputField.text : null)
            ? "127.0.0.1" : ipInputField.text.Trim();
        transport.SetConnectionData(ip, transport.ConnectionData.Port);

        SetStatus($"Connecting to {ip}...");
        bool started = NetworkManager.Singleton.StartClient();
        if (!started) SetStatus("Failed to start client.");
    }

    private void OnDisconnectClicked()
    {
        NetworkManager.Singleton.Shutdown();
        RefreshView();
        SetStatus("Disconnected.");
    }

    private bool ApplyPort()
    {
        var transport = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.GetComponent<UnityTransport>() : null;
        if (transport == null)
        {
            SetStatus("No UnityTransport found on the NetworkManager.");
            return false;
        }

        ushort port = transport.ConnectionData.Port;
        if (portInputField != null && ushort.TryParse(portInputField.text, out ushort parsed))
            port = parsed;

        transport.SetConnectionData(transport.ConnectionData.Address, port);
        return true;
    }

    private void OnServerStarted() => RefreshView();
    private void OnConnected(ulong clientId) => RefreshView();

    private void OnDisconnected(ulong clientId)
    {
        // Our own disconnect callback firing while we're not (or no longer) connected
        // means the connection attempt failed or the host dropped us.
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
        {
            SetStatus("Disconnected from server.");
            RefreshView();
        }
    }

    private void RefreshView()
    {
        bool connected = IsConnected;

        if (menuPanel != null) menuPanel.SetActive(!connected);

        if (connected && !hasEverConnected)
        {
            // Only force the panel open the FIRST time we connect - later calls to
            // RefreshView (e.g. another player joining) shouldn't reopen it if the
            // local player has since closed it themselves.
            hasEverConnected = true;
            SetConnectedPanelOpen(true);
        }
        else if (!connected)
        {
            hasEverConnected = false;
            if (connectedPanel != null) connectedPanel.SetActive(false);
        }
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log($"[ServerLobbyUI] {message}");
    }
}

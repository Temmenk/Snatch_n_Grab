using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PublicLobbyMenu : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private Button leaveButton;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject searchLobbyPanel;
    [SerializeField] private GameObject lobbyInfoPanel;

    [Header("Lobby browser")]
    [SerializeField] private Transform roomListPanel;
    [SerializeField] private Button lobbyEntryPrefab;
    [SerializeField] private TMP_InputField playerNameInput;
    [SerializeField] private TMP_Text playerListText;
    [SerializeField] private TMP_Text statusText;

    [Header("Settings")]
    [SerializeField] private string gameSceneName = "GameScene";
    [SerializeField] private string lobbyName = "Abrams Match";
    [SerializeField] private int maxPlayers = 4;

    private Lobby currentLobby;
    private bool isHost;
    private bool busy;
    private float heartbeatTimer;
    private float lobbyRefreshTimer;
    private readonly List<GameObject> lobbyEntries = new List<GameObject>();

    private async void Start()
    {
        SetStatus("Connecting to Unity Services...");
        SetBusy(true);
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            SetStatus("Ready");
        }
        catch (Exception e)
        {
            SetStatus("Unity Services failed: " + e.Message);
            Debug.LogException(e);
        }
        finally { SetBusy(false); }
        ShowPanel(mainMenuPanel);
    }

    private void Update()
    {
        if (currentLobby == null) return;

        if (isHost)
        {
            heartbeatTimer += Time.deltaTime;
            if (heartbeatTimer >= 15f)
            {
                heartbeatTimer = 0f;
                _ = SendHeartbeat();
            }
        }

        lobbyRefreshTimer += Time.deltaTime;
        if (lobbyRefreshTimer >= 3f)
        {
            lobbyRefreshTimer = 0f;
            _ = RefreshCurrentLobby();
        }
    }

    private async Task SendHeartbeat()
    {
        try
        {
            if (currentLobby != null && isHost)
                await LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
        }
        catch (Exception e) { Debug.LogWarning("Heartbeat: " + e.Message); }
    }

    private async Task RefreshCurrentLobby()
    {
        try
        {
            if (currentLobby == null) return;
            string id = currentLobby.Id;
            Lobby updated = await LobbyService.Instance.GetLobbyAsync(id);
            if (currentLobby == null || currentLobby.Id != id) return;
            currentLobby = updated;
            UpdatePlayerList();
        }
        catch (Exception e) { Debug.LogWarning("Lobby refresh: " + e.Message); }
    }

    public async void HostGame()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        SetStatus("Host from the Unity Editor or Windows build. Browser players can join.");
        return;
#endif
        if (busy || currentLobby != null || NetworkManager.Singleton == null) return;
        SetBusy(true);
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPlayers - 1);
            string code = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            transport.UseWebSockets = true;
            if (!NetworkManager.Singleton.StartHost())
                throw new Exception("StartHost failed.");

            currentLobby = await LobbyService.Instance.CreateLobbyAsync(
                lobbyName, maxPlayers, new CreateLobbyOptions
                {
                    IsPrivate = false,
                    Player = MakePlayer(),
                    Data = new Dictionary<string, DataObject>
                    {
                        { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, code) }
                    }
                });

            isHost = true;
            UpdatePlayerList();
            ShowPanel(lobbyInfoPanel);
            SetStatus("Public lobby created: " + currentLobby.Name);
            Debug.Log("Public lobby created: " + currentLobby.Name);
        }
        catch (Exception e)
        {
            SetStatus("Host failed: " + e.Message);
            Debug.LogException(e);
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                NetworkManager.Singleton.Shutdown();
        }
        finally { SetBusy(false); }
    }

    public async void BrowseLobbies()
    {
        ShowPanel(searchLobbyPanel);
        await RefreshLobbyList();
    }

    public async void RefreshLobbies() { await RefreshLobbyList(); }

    private async Task RefreshLobbyList()
    {
        if (busy) return;
        SetBusy(true);
        ClearEntries();
        try
        {
            QueryResponse response = await LobbyService.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions { Count = 25 });
            foreach (Lobby lobby in response.Results)
            {
                if (lobbyEntryPrefab == null || roomListPanel == null) break;
                Lobby foundLobby = lobby;
                Button entry = Instantiate(lobbyEntryPrefab, roomListPanel);
                lobbyEntries.Add(entry.gameObject);
                TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = lobby.Name + "  (" + lobby.Players.Count + "/" + lobby.MaxPlayers + ")";
                entry.onClick.AddListener(() => JoinLobby(foundLobby.Id));
            }
            SetStatus(response.Results.Count == 0 ? "No public matches yet." :
                response.Results.Count + " public match(es) found.");
        }
        catch (Exception e)
        {
            SetStatus("Search failed: " + e.Message);
            Debug.LogException(e);
        }
        finally { SetBusy(false); }
    }

    public async void JoinLobby(string lobbyId)
    {
        if (busy || currentLobby != null || NetworkManager.Singleton == null) return;
        SetBusy(true);
        try
        {
            currentLobby = await LobbyService.Instance.JoinLobbyByIdAsync(
                lobbyId, new JoinLobbyByIdOptions { Player = MakePlayer() });

            if (currentLobby.Data == null ||
                !currentLobby.Data.TryGetValue("RelayCode", out DataObject relayData))
                throw new Exception("This lobby has no Relay connection code.");

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(relayData.Value);
            UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            transport.SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "wss"));
            transport.UseWebSockets = true;
            if (!NetworkManager.Singleton.StartClient())
                throw new Exception("StartClient failed.");

            isHost = false;
            UpdatePlayerList();
            ShowPanel(lobbyInfoPanel);
            SetStatus("Connecting to " + currentLobby.Name + "...");
        }
        catch (Exception e)
        {
            SetStatus("Join failed: " + e.Message);
            Debug.LogException(e);
            await LeaveLobbyInternal();
        }
        finally { SetBusy(false); }
    }

    public void StartMatch()
    {
        if (!isHost || currentLobby == null || NetworkManager.Singleton == null ||
            !NetworkManager.Singleton.IsHost) return;
        NetworkManager.Singleton.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
    }

    public async void LeaveLobby()
    {
        if (busy) return;
        SetBusy(true);
        await LeaveLobbyInternal();
        ShowPanel(mainMenuPanel);
        SetStatus("Left lobby.");
        SetBusy(false);
    }

    private async Task LeaveLobbyInternal()
    {
        Lobby leaving = currentLobby;
        bool wasHost = isHost;
        currentLobby = null;
        isHost = false;
        try
        {
            if (leaving != null)
            {
                if (wasHost) await LobbyService.Instance.DeleteLobbyAsync(leaving.Id);
                else await LobbyService.Instance.RemovePlayerAsync(
                    leaving.Id, AuthenticationService.Instance.PlayerId);
            }
        }
        catch (Exception e) { Debug.LogWarning("Lobby cleanup: " + e.Message); }
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            NetworkManager.Singleton.Shutdown();
    }

    private Player MakePlayer()
    {
        string name = playerNameInput != null ? playerNameInput.text.Trim() : "";
        if (string.IsNullOrEmpty(name)) name = "Crewman";
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { "Name", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, name) }
            }
        };
    }

    private void UpdatePlayerList()
    {
        if (playerListText == null || currentLobby == null) return;
        string names = currentLobby.Name + "  (" + currentLobby.Players.Count + "/" +
            currentLobby.MaxPlayers + ")\n";
        foreach (Player player in currentLobby.Players)
        {
            string name = "Crewman";
            if (player.Data != null && player.Data.TryGetValue("Name", out PlayerDataObject data))
                name = data.Value;
            names += (player.Id == currentLobby.HostId ? "[HOST] " : "") + name + "\n";
        }
        playerListText.text = names;
        if (startGameButton != null) startGameButton.gameObject.SetActive(isHost);
    }

    private void ShowPanel(GameObject active)
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(active == mainMenuPanel);
        if (searchLobbyPanel != null) searchLobbyPanel.SetActive(active == searchLobbyPanel);
        if (lobbyInfoPanel != null) lobbyInfoPanel.SetActive(active == lobbyInfoPanel);
    }

    private void ClearEntries()
    {
        foreach (GameObject entry in lobbyEntries)
            if (entry != null) Destroy(entry);
        lobbyEntries.Clear();
    }

    private void SetBusy(bool value)
    {
        busy = value;
        if (hostGameButton != null) hostGameButton.interactable = !value;
        if (joinRoomButton != null) joinRoomButton.interactable = !value;
        if (refreshButton != null) refreshButton.interactable = !value;
        if (leaveButton != null) leaveButton.interactable = !value;
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
        Debug.Log("[Lobby] " + message);
    }
}

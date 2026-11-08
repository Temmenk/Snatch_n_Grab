using UnityEngine;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using TMPro;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SessionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
    [SerializeField] private Button startGameButton;
    [SerializeField] private string gameSceneName = "GameScene";

    private ISession session;
    private bool isStartingGame;

    private void Update()
    {
        if (startGameButton == null)
            return;

        var network = NetworkManager.Singleton;
        startGameButton.interactable = session != null && network != null
            && network.IsListening && network.IsHost && !isStartingGame;
    }

    public void StartGame()
    {
        var network = NetworkManager.Singleton;
        if (session == null || network == null || !network.IsListening
            || !network.IsHost || isStartingGame)
            return;

        isStartingGame = true;
        if (startGameButton != null)
            startGameButton.interactable = false;

        // Netcode loads the scene for every connected client, including the host.
        var status = network.SceneManager.LoadScene(gameSceneName, LoadSceneMode.Single);
        if (status != SceneEventProgressStatus.Started)
        {
            isStartingGame = false;
            Debug.LogError($"Could not load {gameSceneName}: {status}");
        }
    }

    async void Start()
    {
        try
        {
            await UnityServices.InitializeAsync();
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    public async void StartSessionAsHost()
    {

        try
        {
            await StartSessionAsHostAsync();
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    async Task StartSessionAsHostAsync()
    {
        var options = new SessionOptions
        {
            MaxPlayers = 4
        }.WithRelayNetwork(); // or WithDistributedAuthorityNetwork() to use Distributed Authority instead of Relay
        session = await MultiplayerService.Instance.CreateSessionAsync(options);
        Debug.Log($"Session {session.Id} created! Join code: {session.Code}");
    }
    
    public async void JoinSession()
    {
        string joinCode = joinCodeInput.text.Trim();

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            Debug.LogWarning("Enter a join code.");
            return;
        }

        try
        {
            await JoinSessionAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }
    
    async Task JoinSessionAsync(string joinCode)
    {
        session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
        Debug.Log($"Session {session.Id} Joined! Let's playyyy!!!");
    }


}

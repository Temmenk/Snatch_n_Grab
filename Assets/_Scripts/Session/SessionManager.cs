using UnityEngine;
using Unity.Services.Core;
using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using TMPro;

public class SessionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeInput;
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
        var session = await MultiplayerService.Instance.CreateSessionAsync(options);
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
        var session = await MultiplayerService.Instance.JoinSessionByCodeAsync(joinCode);
        Debug.Log($"Session {session.Id} Joined! Let's playyyy!!!");
    }


}

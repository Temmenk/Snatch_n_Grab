
using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;

public class RelayMenu : MonoBehaviour
{
    [Header("Menu UI")]
    [SerializeField] private Button hostGameButton;
    [SerializeField] private TMP_Text joinCodeText;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TMP_InputField joinCodeInput;
    private Lobby currentLobby;
    private float heartbeatTimer;

    private async void Start()
    {
        hostGameButton.interactable = false;
        joinRoomButton.interactable = false;

        try
        {
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            hostGameButton.interactable = true;
            joinRoomButton.interactable = true;
            Debug.Log("Unity Services ready.");
        }
        catch (Exception e)
        {
            Debug.LogError("Unity Services failed: " + e.Message);
        }
    }

    public async void HostGame()
    {
        hostGameButton.interactable = false;

        try
        {
            // Four players total, including the host.
            Allocation allocation =
                await RelayService.Instance.CreateAllocationAsync(3);

            string joinCode =
                await RelayService.Instance.GetJoinCodeAsync(
                    allocation.AllocationId);
                    currentLobby = await LobbyService.Instance.CreateLobbyAsync(
    "Abrams Match",
    4,
    new CreateLobbyOptions
    {
        IsPrivate = false,
        Data = new Dictionary<string, DataObject>
        {
            {
                "RelayCode",
                new DataObject(
                    DataObject.VisibilityOptions.Member,
                    joinCode)
            }
        }
    });

Debug.Log("Public lobby created: " + currentLobby.Name);

            UnityTransport transport =
                NetworkManager.Singleton.GetComponent<UnityTransport>();

            transport.SetRelayServerData(
                AllocationUtils.ToRelayServerData(allocation, "wss"));

            transport.UseWebSockets = true;

            if (!NetworkManager.Singleton.StartHost())
                throw new Exception("Netcode failed to start the host.");

            Debug.Log("Relay join code: " + joinCode);

            if (joinCodeText != null)
                joinCodeText.text = "JOIN CODE: " + joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError("Host failed: " + e.Message);
            hostGameButton.interactable = true;
        }
    }
    public async void JoinGame()
{
    string code = joinCodeInput.text.Trim().ToUpper();

    if (string.IsNullOrEmpty(code))
    {
        Debug.LogWarning("Enter a join code.");
        return;
    }

    joinRoomButton.interactable = false;

    try
    {
        JoinAllocation allocation =
            await RelayService.Instance.JoinAllocationAsync(code);

        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();

        transport.SetRelayServerData(
            AllocationUtils.ToRelayServerData(allocation, "wss"));

        transport.UseWebSockets = true;

        if (!NetworkManager.Singleton.StartClient())
            throw new Exception("Failed to start client.");

        Debug.Log("Joining Relay room: " + code);
    }
    catch (Exception e)
    {
        Debug.LogError("Join failed: " + e.Message);
        joinRoomButton.interactable = true;
    }
}
private async void Update()
{
    if (currentLobby == null)
        return;

    if (currentLobby.HostId != AuthenticationService.Instance.PlayerId)
        return;

    heartbeatTimer += Time.deltaTime;

    if (heartbeatTimer >= 15f)
    {
        heartbeatTimer = 0f;

        try
        {
            await LobbyService.Instance.SendHeartbeatPingAsync(
                currentLobby.Id);
        }
        catch (Exception e)
        {
            Debug.LogWarning("Lobby heartbeat failed: " + e.Message);
        }
    }
}
public async void SearchLobbies()
{
    try
    {
        QueryResponse response =
            await LobbyService.Instance.QueryLobbiesAsync(
                new QueryLobbiesOptions
                {
                    Count = 20
                });

        Debug.Log("Public lobbies found: " +
                  response.Results.Count);

        foreach (Lobby lobby in response.Results)
        {
            Debug.Log(
                lobby.Name + " | " +
                lobby.Players.Count + "/" +
                lobby.MaxPlayers + " players" +
                " | ID: " + lobby.Id);
        }
    }
    catch (Exception e)
    {
        Debug.LogError("Lobby search failed: " + e.Message);
    }
}
}

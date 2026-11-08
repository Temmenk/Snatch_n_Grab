using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [SerializeField]
    private MultiplayerUI m_multiplayerUI;
    [SerializeField]
    private GameObject m_playerPrefab;

    [SerializeField]
    private List<ResourcePallet> m_pallets;

    [SerializeField]
    private Vector3[] m_spawnPositions =
    {
        new Vector3(-2f, 0f, -2f),
        new Vector3(2f, 0f, -2f),
        new Vector3(-2f, 0f, 2f),
        new Vector3(2f, 0f, 2f)
    };

    private readonly Dictionary<ulong, int> m_spawnSlots = new();

    private void Start()
    {
        if (m_multiplayerUI != null)
        {
            m_multiplayerUI.OnStartHost += StartHost;
            m_multiplayerUI.OnStartClient += StartClient;
            m_multiplayerUI.OnDiconnectClient += DisconnectClient;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer == false)
            return;
        NetworkManager.OnClientConnectedCallback += SpawnPlayer;
        NetworkManager.OnClientDisconnectCallback += ReleaseSpawnSlot;
        NetworkManager.SceneManager.OnLoadEventCompleted += HandleSceneLoadCompleted;
        foreach (ResourcePallet pallet in m_pallets)
        {
            pallet.OnPalletFilled += CheckWinCondition;
        }
    }

    private void CheckWinCondition()
    {
        int points = 0;
        foreach(ResourcePallet pallet in m_pallets)
        {
            points += pallet.StackedResoruces;
        }
        if(points >= m_pallets.Count * 3)
        {
            NetworkManager.SceneManager.LoadScene(
                SceneManager.GetActiveScene().name, LoadSceneMode.Single);
        }
    }

    private void HandleSceneLoadCompleted(string sceneName, 
        LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        if (!IsServer || sceneName != gameObject.scene.name)
            return;

        foreach(ulong clientId in clientsCompleted)
        {
            SpawnPlayer(clientId);
        }
    }

    private void SpawnPlayer(ulong clientID)
    {
        if (!IsServer || !NetworkManager.ConnectedClients.TryGetValue(clientID, out var client)
            || client.PlayerObject != null)
            return;

        for (int slot = 0; slot < m_spawnPositions.Length; slot++)
        {
            if (m_spawnSlots.ContainsValue(slot))
                continue;

            GameObject player = Instantiate(m_playerPrefab, m_spawnPositions[slot],
                m_playerPrefab.transform.rotation);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientID, true);
            m_spawnSlots[clientID] = slot;
            return;
        }

        Debug.LogWarning($"No free spawn position for client {clientID}.");
    }

    private void ReleaseSpawnSlot(ulong clientID)
    {
        m_spawnSlots.Remove(clientID);
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            NetworkManager.OnClientConnectedCallback -= SpawnPlayer;
            NetworkManager.OnClientDisconnectCallback -= ReleaseSpawnSlot;
            NetworkManager.SceneManager.OnLoadEventCompleted -= HandleSceneLoadCompleted;
            foreach (ResourcePallet pallet in m_pallets)
            {
                pallet.OnPalletFilled -= CheckWinCondition;
            }
        }
        m_spawnSlots.Clear();

        base.OnNetworkDespawn();
    }

    private void DisconnectClient()
    {
        m_multiplayerUI.EnableButtons();
        NetworkManager.Shutdown();
    }

    private void StartClient()
    {
        if (NetworkManager.IsListening)
            return;

        m_multiplayerUI.DisableButtons();
        NetworkManager.StartClient();
    }

    private void StartHost()
    {
        if (NetworkManager.IsListening)
            return;

        m_multiplayerUI.DisableButtons();
        NetworkManager.StartHost();
    }
}

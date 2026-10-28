
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankCrewManager : NetworkBehaviour
{
    [Header("Gunner Station")]
    public Transform gunnerSeat;
    public TankGunner tankGunner;

    public NetworkVariable<ulong> gunnerClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool GunnerSeatAvailable =>
        gunnerClientId.Value == ulong.MaxValue;

    private Camera worldCamera;
    private CameraFollow cameraFollow;
    private PlayerController localPlayer;
    private bool operatingLocally;

    public override void OnNetworkSpawn()
    {
        gunnerClientId.OnValueChanged += OnGunnerChanged;
        OnGunnerChanged(ulong.MaxValue, gunnerClientId.Value);
    }

    public override void OnNetworkDespawn()
    {
        if (operatingLocally)
            ExitLocalGunner();

        gunnerClientId.OnValueChanged -= OnGunnerChanged;
    }

    [Rpc(SendTo.Server)]
    public void RequestGunnerSeatRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!GunnerSeatAvailable)
            return;

        // Check that the player is actually near the tank.
        if (!NetworkManager.ConnectedClients.TryGetValue(
            clientId, out var client))
            return;

        if (client.PlayerObject == null || gunnerSeat == null)
            return;

        if (Vector3.Distance(
            client.PlayerObject.transform.position,
            gunnerSeat.position) > 10f)
            return;

        gunnerClientId.Value = clientId;
        Debug.Log($"Client {clientId} entered gunner seat.");
    }

    [Rpc(SendTo.Server)]
    public void LeaveGunnerSeatRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (gunnerClientId.Value != clientId)
            return;

        gunnerClientId.Value = ulong.MaxValue;
        Debug.Log($"Client {clientId} left gunner seat.");
    }

    private void OnGunnerChanged(ulong previous, ulong current)
    {
        if (NetworkManager == null || !NetworkManager.IsListening)
            return;

        bool isMySeat = current == NetworkManager.LocalClientId;

        if (isMySeat && !operatingLocally)
            EnterLocalGunner();
        else if (!isMySeat && operatingLocally)
            ExitLocalGunner();
    }

    private void EnterLocalGunner()
    {
        if (tankGunner == null || tankGunner.gunnerCamera == null)
            return;

        if (NetworkManager.LocalClient == null ||
            NetworkManager.LocalClient.PlayerObject == null)
            return;

        localPlayer = NetworkManager.LocalClient.PlayerObject
            .GetComponent<PlayerController>();

        if (localPlayer == null)
            return;

        worldCamera = Camera.main;
        cameraFollow = localPlayer.GetComponent<CameraFollow>();

        localPlayer.isOperatingTank = true;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        if (worldCamera != null)
            worldCamera.enabled = false;

        tankGunner.gunnerCamera.enabled = true;
        tankGunner.controlsEnabled = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        operatingLocally = true;
        Debug.Log("Gunner camera activated.");
    }

    private void ExitLocalGunner()
    {
        if (tankGunner != null)
        {
            tankGunner.controlsEnabled = false;

            if (tankGunner.gunnerCamera != null)
                tankGunner.gunnerCamera.enabled = false;
        }

        if (worldCamera != null)
            worldCamera.enabled = true;

        if (cameraFollow != null)
            cameraFollow.enabled = true;

        if (localPlayer != null)
            localPlayer.isOperatingTank = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        operatingLocally = false;
        Debug.Log("Exited gunner station.");
    }

    private void Update()
    {
        if (!operatingLocally || Keyboard.current == null)
            return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
            LeaveGunnerSeatRpc();
    }
}

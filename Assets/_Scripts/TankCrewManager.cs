
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class TankCrewManager : NetworkBehaviour
{
    [Header("Gunner Station")]
    public Transform gunnerSeat;
    public TankGunner tankGunner;

    [Header("Commander Station")]
    public Transform commanderSeat;
    public CommanderCITV commanderCITV;

    public NetworkVariable<ulong> gunnerClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public NetworkVariable<ulong> commanderClientId = new(
        ulong.MaxValue,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public bool GunnerSeatAvailable =>
        gunnerClientId.Value == ulong.MaxValue;

    public bool CommanderSeatAvailable =>
        commanderClientId.Value == ulong.MaxValue;

    private enum CrewRole
    {
        None,
        Gunner,
        Commander
    }

    private CrewRole localRole = CrewRole.None;

    private Camera worldCamera;
    private AudioListener worldAudio;
    private CameraFollow cameraFollow;
    private PlayerController localPlayer;

    public override void OnNetworkSpawn()
    {
        gunnerClientId.OnValueChanged += OnSeatChanged;
        commanderClientId.OnValueChanged += OnSeatChanged;

        RefreshLocalSeat();
    }

    public override void OnNetworkDespawn()
    {
        gunnerClientId.OnValueChanged -= OnSeatChanged;
        commanderClientId.OnValueChanged -= OnSeatChanged;

        if (localRole != CrewRole.None)
            ExitLocalStation();
    }

    private bool PlayerNearSeat(ulong clientId, Transform seat)
    {
        if (seat == null)
            return false;

        if (!NetworkManager.ConnectedClients.TryGetValue(
            clientId, out var client))
            return false;

        if (client.PlayerObject == null)
            return false;

        return Vector3.Distance(
            client.PlayerObject.transform.position,
            seat.position) <= 10f;
    }

    [Rpc(SendTo.Server)]
    public void RequestGunnerSeatRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        if (!GunnerSeatAvailable ||
            commanderClientId.Value == id ||
            !PlayerNearSeat(id, gunnerSeat))
            return;

        gunnerClientId.Value = id;
        Debug.Log($"Client {id} entered gunner seat.");
    }

    [Rpc(SendTo.Server)]
    public void RequestCommanderSeatRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        if (!CommanderSeatAvailable ||
            gunnerClientId.Value == id ||
            !PlayerNearSeat(id, commanderSeat))
            return;

        commanderClientId.Value = id;
        Debug.Log($"Client {id} entered commander seat.");
    }

    [Rpc(SendTo.Server)]
    public void LeaveGunnerSeatRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        if (gunnerClientId.Value == id)
            gunnerClientId.Value = ulong.MaxValue;
    }

    [Rpc(SendTo.Server)]
    public void LeaveCommanderSeatRpc(RpcParams rpcParams = default)
    {
        ulong id = rpcParams.Receive.SenderClientId;

        if (commanderClientId.Value == id)
            commanderClientId.Value = ulong.MaxValue;
    }

    private void OnSeatChanged(ulong previous, ulong current)
    {
        RefreshLocalSeat();
    }

    private void RefreshLocalSeat()
    {
        if (NetworkManager == null || !NetworkManager.IsListening)
            return;

        ulong myId = NetworkManager.LocalClientId;

        CrewRole newRole = CrewRole.None;

        if (gunnerClientId.Value == myId)
            newRole = CrewRole.Gunner;
        else if (commanderClientId.Value == myId)
            newRole = CrewRole.Commander;

        if (newRole == localRole)
            return;

        if (localRole != CrewRole.None)
            ExitLocalStation();

        if (newRole != CrewRole.None)
            EnterLocalStation(newRole);
    }

    private void EnterLocalStation(CrewRole role)
    {
        if (NetworkManager.LocalClient == null ||
            NetworkManager.LocalClient.PlayerObject == null)
            return;

        localPlayer = NetworkManager.LocalClient.PlayerObject
            .GetComponent<PlayerController>();

        if (localPlayer == null)
            return;

        Camera stationCamera = GetStationCamera(role);

        if (stationCamera == null)
        {
            Debug.LogWarning("Crew station camera not assigned!");
            return;
        }

        worldCamera = Camera.main;
        cameraFollow = localPlayer.GetComponent<CameraFollow>();

        if (worldCamera != null)
            worldAudio = worldCamera.GetComponent<AudioListener>();

        localPlayer.isOperatingTank = true;

        if (cameraFollow != null)
            cameraFollow.enabled = false;

        if (worldCamera != null)
            worldCamera.enabled = false;

        if (worldAudio != null)
            worldAudio.enabled = false;

        stationCamera.enabled = true;

        AudioListener stationAudio =
            stationCamera.GetComponent<AudioListener>();

        if (stationAudio != null)
            stationAudio.enabled = true;

        if (role == CrewRole.Gunner && tankGunner != null)
            tankGunner.controlsEnabled = true;

        if (role == CrewRole.Commander && commanderCITV != null)
        {
            commanderCITV.controlsEnabled = true;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        localRole = role;

        Debug.Log($"Entered {role} station.");
    }

    private Camera GetStationCamera(CrewRole role)
    {
        if (role == CrewRole.Gunner)
            return tankGunner != null
                ? tankGunner.gunnerCamera : null;

        if (role == CrewRole.Commander)
            return commanderCITV != null
                ? commanderCITV.citvCamera : null;

        return null;
    }

    private void ExitLocalStation()
    {
        Camera stationCamera = GetStationCamera(localRole);

        if (localRole == CrewRole.Gunner && tankGunner != null)
            tankGunner.controlsEnabled = false;

        if (localRole == CrewRole.Commander && commanderCITV != null)
            commanderCITV.controlsEnabled = false;

        if (stationCamera != null)
        {
            stationCamera.enabled = false;

            AudioListener listener =
                stationCamera.GetComponent<AudioListener>();

            if (listener != null)
                listener.enabled = false;
        }

        if (worldCamera != null)
            worldCamera.enabled = true;

        if (worldAudio != null)
            worldAudio.enabled = true;

        if (cameraFollow != null)
            cameraFollow.enabled = true;

        if (localPlayer != null)
            localPlayer.isOperatingTank = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        localRole = CrewRole.None;
    }

    private void Update()
    {
        if (localRole == CrewRole.None ||
            Keyboard.current == null)
            return;

        if (!Keyboard.current.fKey.wasPressedThisFrame)
            return;

        if (localRole == CrewRole.Gunner)
            LeaveGunnerSeatRpc();
        else if (localRole == CrewRole.Commander)
            LeaveCommanderSeatRpc();
    }
}

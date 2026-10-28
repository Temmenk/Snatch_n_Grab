
using Unity.Netcode;
using UnityEngine;

public class CommanderCITVNetwork : NetworkBehaviour
{
    public TankCrewManager crew;
    public CommanderCITV commander;

    private NetworkVariable<Quaternion> sightRotation = new();

    private float nextSendTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer && commander != null &&
            commander.sightPivot != null)
        {
            sightRotation.Value =
                commander.sightPivot.localRotation;
        }
    }

    private bool IsLocalCommander()
    {
        return crew != null &&
               crew.commanderClientId.Value ==
               NetworkManager.LocalClientId;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || commander == null ||
            commander.sightPivot == null)
            return;

        if (IsLocalCommander())
        {
            // Send the commander's sight rotation 20 times/sec.
            if (Time.time >= nextSendTime)
            {
                nextSendTime = Time.time + 0.05f;

                if (IsServer)
                {
                    sightRotation.Value =
                        commander.sightPivot.localRotation;
                }
                else
                {
                    SendRotationRpc(
                        commander.sightPivot.localRotation
                    );
                }
            }

            return;
        }

        // Other players see the synchronized CITV.
        commander.sightPivot.localRotation =
            Quaternion.Slerp(
                commander.sightPivot.localRotation,
                sightRotation.Value,
                Time.deltaTime * 15f
            );
    }

    [Rpc(SendTo.Server)]
    private void SendRotationRpc(
        Quaternion rotation,
        RpcParams rpcParams = default)
    {
        if (crew == null ||
            crew.commanderClientId.Value !=
            rpcParams.Receive.SenderClientId)
            return;

        sightRotation.Value = rotation;
    }
}

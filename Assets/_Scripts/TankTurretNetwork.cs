
using Unity.Netcode;
using UnityEngine;

public class TankTurretNetwork : NetworkBehaviour
{
    public TankCrewManager crew;
    public TankTurret tankTurret;

    private NetworkVariable<Quaternion> turretRotation = new();
    private NetworkVariable<Quaternion> gunRotation = new();
    private NetworkVariable<Quaternion> sightRotation = new();

    private float nextSendTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SaveRotations();
        }
    }

    private bool IsLocalGunner()
    {
        return crew != null &&
               crew.gunnerClientId.Value ==
               NetworkManager.LocalClientId;
    }

    private void LateUpdate()
    {
        if (!IsSpawned || tankTurret == null)
            return;

        if (tankTurret.turret == null ||
            tankTurret.gun == null ||
            tankTurret.gunnerSightPivot == null)
            return;

        // The gunner controls their own view immediately.
        if (IsLocalGunner())
        {
            if (Time.time >= nextSendTime)
            {
                nextSendTime = Time.time + 0.05f;

                if (IsServer)
                    SaveRotations();
                else
                    SendRotationsRpc(
                        tankTurret.turret.localRotation,
                        tankTurret.gun.localRotation,
                        tankTurret.gunnerSightPivot.localRotation
                    );
            }

            return;
        }

        // Everyone else sees the synchronized turret.
        tankTurret.turret.localRotation =
            Quaternion.Slerp(
                tankTurret.turret.localRotation,
                turretRotation.Value,
                Time.deltaTime * 15f
            );

        tankTurret.gun.localRotation =
            Quaternion.Slerp(
                tankTurret.gun.localRotation,
                gunRotation.Value,
                Time.deltaTime * 15f
            );

        tankTurret.gunnerSightPivot.localRotation =
            Quaternion.Slerp(
                tankTurret.gunnerSightPivot.localRotation,
                sightRotation.Value,
                Time.deltaTime * 15f
            );
    }

    private void SaveRotations()
    {
        turretRotation.Value = tankTurret.turret.localRotation;
        gunRotation.Value = tankTurret.gun.localRotation;
        sightRotation.Value =
            tankTurret.gunnerSightPivot.localRotation;
    }

    [Rpc(SendTo.Server)]
    private void SendRotationsRpc(
        Quaternion turret,
        Quaternion gun,
        Quaternion sight,
        RpcParams rpcParams = default)
    {
        // Only the assigned gunner can update the tank.
        if (crew == null ||
            crew.gunnerClientId.Value !=
            rpcParams.Receive.SenderClientId)
            return;

        turretRotation.Value = turret;
        gunRotation.Value = gun;
        sightRotation.Value = sight;
    }
}

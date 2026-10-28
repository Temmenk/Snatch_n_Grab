
using Unity.Netcode;
using UnityEngine;

public class TankWeaponNetwork : NetworkBehaviour
{
    public TankCrewManager crew;
    public TankGunner gunner;

    public NetworkVariable<int> sabotAmmo = new();
    public NetworkVariable<int> heatAmmo = new();

    public NetworkVariable<float> reloadRemaining = new();

    private float nextFireTime;

    public override void OnNetworkSpawn()
    {
        if (IsServer && gunner != null)
        {
            sabotAmmo.Value = gunner.sabotRounds;
            heatAmmo.Value = gunner.heatRounds;
        }
    }

    private void Update()
    {
        if (!IsSpawned || gunner == null)
            return;

        // Keep the gunner HUD's ammunition in sync.
        gunner.sabotRounds = sabotAmmo.Value;
        gunner.heatRounds = heatAmmo.Value;

        if (IsServer)
        {
            reloadRemaining.Value =
                Mathf.Max(0f, nextFireTime - Time.time);
        }
    }

    public void RequestFire(TankGunner.AmmoType ammo)
    {
        if (!IsSpawned || crew == null)
            return;

        if (crew.gunnerClientId.Value !=
            NetworkManager.LocalClientId)
            return;

        FireRpc((int)ammo);
    }

    [Rpc(SendTo.Server)]
    private void FireRpc(
        int ammoIndex,
        RpcParams rpcParams = default)
    {
        if (crew == null || gunner == null ||
            gunner.muzzle == null)
            return;

        if (crew.gunnerClientId.Value !=
            rpcParams.Receive.SenderClientId)
            return;

        if (ammoIndex < 0 || ammoIndex > 1)
            return;

        if (Time.time < nextFireTime)
            return;

        TankGunner.AmmoType ammo =
            (TankGunner.AmmoType)ammoIndex;

        if (ammo == TankGunner.AmmoType.SABOT)
        {
            if (sabotAmmo.Value <= 0) return;
        }
        else
        {
            if (heatAmmo.Value <= 0) return;
        }

        TankProjectile prefab =
            ammo == TankGunner.AmmoType.SABOT
            ? gunner.sabotPrefab
            : gunner.heatPrefab;

        if (prefab == null)
            return;

        if (ammo == TankGunner.AmmoType.SABOT)
            sabotAmmo.Value--;
        else
            heatAmmo.Value--;

        nextFireTime = Time.time + gunner.reloadSeconds;

        float speed =
            ammo == TankGunner.AmmoType.SABOT
            ? gunner.sabotSpeed
            : gunner.heatSpeed;

        Vector3 position = gunner.muzzle.position;
        Vector3 direction = gunner.muzzle.forward;

        // Server simulates the actual projectile and damage.
        TankProjectile projectile =
            Instantiate(prefab, position,
                Quaternion.LookRotation(direction));

        projectile.Launch(
            direction,
            speed,
            ammo,
            transform.root,
            gunner.targetLayers
        );

        // All non-server clients create visual copies.
        ShowShotRpc(position, direction, ammoIndex, speed);

        Debug.Log($"SERVER FIRED: {ammo}");
    }

    [Rpc(SendTo.NotServer)]
    private void ShowShotRpc(
        Vector3 position,
        Vector3 direction,
        int ammoIndex,
        float speed)
    {
        if (gunner == null)
            return;

        TankGunner.AmmoType ammo =
            (TankGunner.AmmoType)ammoIndex;

        TankProjectile prefab =
            ammo == TankGunner.AmmoType.SABOT
            ? gunner.sabotPrefab
            : gunner.heatPrefab;

        if (prefab == null)
            return;

        TankProjectile projectile =
            Instantiate(prefab, position,
                Quaternion.LookRotation(direction));

        projectile.Launch(
            direction,
            speed,
            ammo,
            transform.root,
            gunner.targetLayers,
            true
        );
    }
}

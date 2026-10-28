using System;
using System.Collections;
using UnityEngine;

// Attach to the EnemyTank root. Existing TankHitbox and TankSpawner scripts work unchanged.
public class TankDamageSystem : MonoBehaviour
{
    public enum VehicleType { MainBattleTank, APC }
    public enum Component { Hull, LeftTrack, RightTrack, Engine, Turret, MainGun }

    [Header("Vehicle")]
    public VehicleType vehicleType = VehicleType.MainBattleTank;

    [Header("Visual Damage")]
    [Tooltip("Assign the Hull transform; its child renderers will darken on mobility kills.")]
    public Transform hullVisuals;
    [Tooltip("Assign the Turret transform; its child renderers will darken on firepower kills.")]
    public Transform turretVisuals;
    public Color disabledColor = new Color(0.22f, 0.22f, 0.22f, 1f);
    public Color destroyedColor = Color.red;

    [Header("Wreck Cleanup")]
    [Min(0f)] public float despawnDelay = 5f;

    // TankSpawner listens for this event to schedule the next enemy.
    public event Action<TankDamageSystem> OnDestroyed;

    public bool destroyed { get; private set; }
    public bool leftTrackDisabled { get; private set; }
    public bool rightTrackDisabled { get; private set; }
    public bool engineDisabled { get; private set; }
    public bool turretDisabled { get; private set; }
    public bool gunDisabled { get; private set; }

    public bool canMove => !destroyed && !leftTrackDisabled && !rightTrackDisabled && !engineDisabled;
    public bool canFire => !destroyed && !turretDisabled && !gunDisabled;

    // Game rules, not a physical armor-penetration simulation. No hit points.
    public void ApplyHit(Component part, TankGunner.AmmoType ammo)
    {
        if (destroyed) return;

        // For this prototype, either round destroys a lightly armored APC.
        if (vehicleType == VehicleType.APC)
        {
            DestroyVehicle();
            return;
        }

        switch (part)
        {
            case Component.LeftTrack:
                leftTrackDisabled = true;
                break;
            case Component.RightTrack:
                rightTrackDisabled = true;
                break;
            case Component.Engine:
                engineDisabled = true;
                break;
            case Component.Turret:
                turretDisabled = true;
                break;
            case Component.MainGun:
                gunDisabled = true;
                break;
            case Component.Hull:
                if (ammo == TankGunner.AmmoType.SABOT)
                {
                    DestroyVehicle();
                    return;
                }
                // HEAT to an MBT hull: mobility kill in our current game rules.
                engineDisabled = true;
                break;
        }

        // A tank that cannot move AND cannot shoot is out of the fight.
        if (!canMove && !canFire)
        {
            DestroyVehicle();
            return;
        }

        UpdateDamageColors();
        Debug.Log($"{name}: {ammo} hit {part} | Mobility: {canMove} | Firepower: {canFire} | Destroyed: {destroyed}");
    }

    void DestroyVehicle()
    {
        if (destroyed) return; // Prevent duplicate kills, events and despawn timers.

        destroyed = true;
        UpdateDamageColors();
        Debug.Log($"{name}: TARGET DESTROYED");
        OnDestroyed?.Invoke(this);
        StartCoroutine(RemoveWreck());
    }

    IEnumerator RemoveWreck()
    {
        yield return new WaitForSeconds(despawnDelay);
        Destroy(gameObject);
    }

    void UpdateDamageColors()
    {
        if (destroyed)
        {
            ColorRenderers(transform, destroyedColor);
            return;
        }

        if (!canMove && hullVisuals != null)
            ColorRenderers(hullVisuals, disabledColor);

        if (!canFire && turretVisuals != null)
            ColorRenderers(turretVisuals, disabledColor);
    }

    void ColorRenderers(Transform root, Color color)
    {
        foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            // renderer.materials creates runtime instances, leaving prefab/shared materials intact.
            foreach (Material material in renderer.materials)
            {
                if (material == null) continue;
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", color);
                else if (material.HasProperty("_Color"))
                    material.SetColor("_Color", color);
            }
        }
    }
}

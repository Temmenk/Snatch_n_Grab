using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TankHitbox : MonoBehaviour
{
    public TankDamageSystem damageSystem;
    public TankDamageSystem.Component component = TankDamageSystem.Component.Hull;

    void Awake()
    {
        if (damageSystem == null)
            damageSystem = GetComponentInParent<TankDamageSystem>();
    }

    public void ReceiveHit(TankGunner.AmmoType ammo)
    {
        if (damageSystem != null)
            damageSystem.ApplyHit(component, ammo);
        else
            Debug.LogWarning($"{name}: TankHitbox has no TankDamageSystem reference.");
    }
}

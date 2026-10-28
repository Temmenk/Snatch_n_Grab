using UnityEngine;

// Add to both projectile prefabs. Forward direction is local +Z.
public class TankProjectile : MonoBehaviour
{
    public float gravity = 9.81f;
    public float maxLifetime = 8f;
    public LayerMask hitLayers = ~0;
    public GameObject impactEffect;

    Vector3 velocity;
    TankGunner.AmmoType ammo;
    Transform shooterRoot;
    bool launched;

    public void Launch(Vector3 direction, float speed, TankGunner.AmmoType ammunition,
                       Transform owner, LayerMask layers)
    {
        velocity = direction.normalized * speed;
        ammo = ammunition;
        shooterRoot = owner;
        hitLayers = layers;
        launched = true;
        Destroy(gameObject, maxLifetime);
    }

    void Update()
    {
        if (!launched) return;

        float dt = Time.deltaTime;
        Vector3 displacement = velocity * dt + 0.5f * Vector3.down * gravity * dt * dt;
        float distance = displacement.magnitude;

        // Cast across the entire movement to avoid tunneling through targets.
        if (distance > 0f)
        {
            RaycastHit[] hits = Physics.RaycastAll(transform.position,
                displacement / distance, distance, hitLayers, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                if (shooterRoot != null && hit.collider.transform.IsChildOf(shooterRoot))
                    continue;

                TankHitbox hitbox = hit.collider.GetComponent<TankHitbox>();
                if (hitbox == null) hitbox = hit.collider.GetComponentInParent<TankHitbox>();
                if (hitbox != null) hitbox.ReceiveHit(ammo);

                if (impactEffect != null)
                    Destroy(Instantiate(impactEffect, hit.point,
                        Quaternion.LookRotation(hit.normal)), 5f);

                Debug.Log($"{ammo} impact: {hit.collider.name}");
                Destroy(gameObject);
                return;
            }
        }

        transform.position += displacement;
        velocity += Vector3.down * gravity * dt;
        if (velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.LookRotation(velocity);
    }
}

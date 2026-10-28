using UnityEngine;

public class TankTurret : MonoBehaviour
{
    public Transform turret;
    public Transform gun;
    public Transform gunnerSightPivot; // Child of Turret, at GPS doghouse
    public float traverseSpeed = 45f;
    public float elevationSpeed = 25f;
    public float minElevation = -10f, maxElevation = 20f;
    float elevation;
    float sightElevation;
    float ballisticOffset; // Retained until the next successful lase or commander designation.
    float horizontalOffset; // Indexed GPS-to-bore yaw offset (degrees).
    [Header("Lase Sight Movement")]
    [Tooltip("Small optional sight kick in degrees when lasing. Set 0 for fully stabilized GPS.")]
    [Range(0f, 0.2f)] public float laseSightKick = 0.02f;
    // TODO: Replace this simplified GPS counter-rotation / optional sight kick
    // with a proper independently stabilized sight and fire-control system.
    // The real Abrams main gun has no independent yaw axis.

    public void Aim(float yawInput, float pitchInput)
    {
        if (!turret || !gun) return;
        turret.Rotate(0f, yawInput * traverseSpeed * Time.deltaTime, 0f, Space.Self);
        sightElevation = Mathf.Clamp(sightElevation - pitchInput * elevationSpeed * Time.deltaTime,
                                     minElevation, maxElevation);
        elevation = Mathf.Clamp(sightElevation + ballisticOffset, minElevation, maxElevation);
        SyncSight();
    }

    // Slew turret and elevate gun so the muzzle points toward the lased world point.
    // This handles geometric sight-to-bore offset; ballistic drop comes later.
    public void SlewTo(Vector3 worldPoint)
    {
        if (!turret || !gun) return;
        ballisticOffset = 0f; // Commander designation overrides the stored correction.
        horizontalOffset = 0f;
        SyncSight();

        Vector3 flatDirection = worldPoint - turret.position;
        flatDirection.y = 0f;
        if (flatDirection.sqrMagnitude < 0.001f) return;

        // Convert world heading to local turret yaw, respecting Abrams rotation.
        Quaternion worldYaw = Quaternion.LookRotation(flatDirection, Vector3.up);
        Quaternion parentRotation = turret.parent ? turret.parent.rotation : Quaternion.identity;
        Quaternion localYaw = Quaternion.Inverse(parentRotation) * worldYaw;
        turret.localRotation = Quaternion.RotateTowards(
            turret.localRotation, localYaw, traverseSpeed * Time.deltaTime);

        // Recalculate pitch after turret rotation, from the muzzle's actual position.
        // Gun local +Z is the barrel's forward direction.
        Vector3 source = gun.position;
        Vector3 localTarget = turret.InverseTransformDirection(worldPoint - source);
        float desiredElevation = -Mathf.Atan2(localTarget.y,
            Mathf.Sqrt(localTarget.x * localTarget.x + localTarget.z * localTarget.z))
            * Mathf.Rad2Deg;
        elevation = Mathf.MoveTowards(elevation,
            Mathf.Clamp(desiredElevation, minElevation, maxElevation),
            elevationSpeed * Time.deltaTime);
        SyncSight();
    }

    // Compute a low-arc solution using the REAL world positions.
    // Lasing changes turret yaw; GPS pitch is unchanged.
    // Both yaw and pitch offsets remain indexed while tracking until the next lase.
    public bool SetBallisticElevation(Vector3 target, Vector3 muzzlePosition, float speed, float gravity)
    {
        if (!turret || !gun || speed <= 0f) return false;

        // Horizontal convergence is performed by rotating the TURRET, not the gun.
        // The GPS is counter-rotated relative to the turret to preserve the
        // indexed bore-to-sight angle while the gunner continues tracking.
        Vector3 toTarget = target - muzzlePosition;
        Vector3 localTarget = turret.InverseTransformDirection(toTarget);
        double forward = System.Math.Sqrt((double)localTarget.x * localTarget.x +
                                          (double)localTarget.z * localTarget.z);
        double height = toTarget.y;
        if (forward <= 0.01) return false;

        double angleRadians;
        if (gravity <= 0.0001f)
        {
            angleRadians = System.Math.Atan2(height, forward);
        }
        else
        {
            double vSquared = (double)speed * speed;
            double g = gravity;
            double discriminant = vSquared * vSquared -
                g * (g * forward * forward + 2.0 * height * vSquared);
            if (discriminant < 0.0) return false;

            // Low-arc branch; double precision avoids cancellation at high SABOT speeds.
            angleRadians = System.Math.Atan((vSquared - System.Math.Sqrt(discriminant)) /
                                            (g * forward));
        }

        float desiredPitch = -(float)(angleRadians * Mathf.Rad2Deg);
        if (desiredPitch < minElevation || desiredPitch > maxElevation) return false;

        // Store elevation relative to the gunner's sight, not a fixed world target.
        float yawDelta = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;
        // Existing sight yaw compensation must be retained when re-lasing.
        // The new correction is the old index plus the residual muzzle-to-target yaw.
        float newHorizontalOffset = horizontalOffset + yawDelta;
        ballisticOffset = desiredPitch - sightElevation;
        elevation = desiredPitch;

        // A small sight kick is optional, to reproduce the slight reacquisition
        // the gunner requested. It is NOT the actual convergence correction.
        // The actual convergence angle is held in the GPS relative to the turret.
        float kick = Mathf.Sign(yawDelta) * laseSightKick;
        turret.localRotation *= Quaternion.Euler(0f, yawDelta + kick, 0f);
        horizontalOffset = newHorizontalOffset;
        SyncSight();
        Debug.Log($"BALLISTIC INDEX: elevation {ballisticOffset:+0.000;-0.000;0.000} deg, " +
                  $"horizontal {horizontalOffset:+0.000;-0.000;0.000} deg");
        return true;
    }

    public void ClearBallisticSolution()
    {
        ballisticOffset = 0f;
        horizontalOffset = 0f;
        elevation = sightElevation;
        SyncSight();
    }

    void SyncSight()
    {
        if (gun != null) gun.localRotation = Quaternion.Euler(elevation, 0f, 0f);
        if (gunnerSightPivot != null)
            gunnerSightPivot.localRotation = Quaternion.Euler(sightElevation, -horizontalOffset, 0f);
    }
}

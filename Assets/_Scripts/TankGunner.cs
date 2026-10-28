using UnityEngine;
using UnityEngine.InputSystem;

public class TankGunner : MonoBehaviour
{
    public TankWeaponNetwork weaponNetwork;
    public enum AmmoType { SABOT, HEAT }
    public TankTurret tankTurret;
    public Camera gunnerCamera;
    public Transform muzzle;
    public LayerMask targetLayers = ~0;
    public float maxRange = 4000f;
    public float reloadSeconds = 5f;
    public int sabotRounds = 20;
    public int heatRounds = 15;
    public bool controlsEnabled = true;
    [Range(0.01f, 2f)] public float mouseSensitivity = 0.5f;
    [Header("Projectile Prefabs")]
    public TankProjectile sabotPrefab;
    public TankProjectile heatPrefab;
    public float sabotSpeed = 1500f;
    public float heatSpeed = 900f;

    [Header("Sight Zoom")]
    public float lowZoomFOV = 21.8f;
    public float highZoomFOV = 6.6f;
    bool highZoom;
    private bool cursorCaptured;

    public AmmoType selectedAmmo = AmmoType.SABOT;
    float nextFireTime;
    public float lastLasedRange { get; private set; }
    public bool hasLased { get; private set; }
    public Vector3 lasedTargetPoint { get; private set; }
    public float reloadRemaining => Mathf.Max(0f, nextFireTime - Time.time);
    public int selectedRounds => selectedAmmo == AmmoType.SABOT ? sabotRounds : heatRounds;

    void Start()
    {
        if (gunnerCamera != null) gunnerCamera.fieldOfView = lowZoomFOV;
    }

    void OnEnable()
    {
        if (controlsEnabled) CaptureCursor();
    }

    void OnDisable()
    {
        ReleaseCursor();
    }

    void CaptureCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        cursorCaptured = true;
    }

    void ReleaseCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        cursorCaptured = false;
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursor();
            return;
        }

        if (!controlsEnabled)
        {
            if (cursorCaptured) ReleaseCursor();
            return;
        }

        // Click the Game view to recapture the mouse after Escape.
        if (!cursorCaptured && Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame)
        {
            CaptureCursor();
            return;
        }

        if (!cursorCaptured) return;

        if (!controlsEnabled || !tankTurret || !gunnerCamera ||
            Mouse.current == null || Keyboard.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        // Commander has priority while Space is held.
        bool commanderSlewing = Keyboard.current.spaceKey.isPressed;
        if (!commanderSlewing && delta.sqrMagnitude > 0.001f)
        {
            tankTurret.Aim(delta.x * mouseSensitivity, delta.y * mouseSensitivity);
        }

        if (Keyboard.current.zKey.wasPressedThisFrame)
        {
            highZoom = !highZoom;
            gunnerCamera.fieldOfView = highZoom ? highZoomFOV : lowZoomFOV;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            selectedAmmo = AmmoType.SABOT; // Existing ballistic offset is intentionally unchanged.
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            selectedAmmo = AmmoType.HEAT; // Lase again to index a new solution.
        }
        if (!commanderSlewing && Mouse.current.rightButton.wasPressedThisFrame) Lase();
        if (Mouse.current.leftButton.wasPressedThisFrame) Fire();
    }

    void Lase()
    {
        if (gunnerCamera == null) return;

        Ray sightRay = new Ray(gunnerCamera.transform.position,
                               gunnerCamera.transform.forward);
        if (Physics.Raycast(sightRay, out RaycastHit hit, maxRange,
                            targetLayers, QueryTriggerInteraction.Ignore))
        {
            lastLasedRange = hit.distance;
            lasedTargetPoint = hit.point;
            hasLased = true;
            UpdateBallisticSolution();
            Debug.Log($"LASE: {lastLasedRange:F0} m on {hit.collider.name}");
        }
        else
        {
            lastLasedRange = maxRange;
            hasLased = false;
            // No return: preserve the last indexed correction until a successful lase.
            Debug.Log("LASE: NO RETURN (previous index retained)");
        }
    }

    void UpdateBallisticSolution()
    {
        if (!hasLased || tankTurret == null || muzzle == null) return;
        TankProjectile prefab = selectedAmmo == AmmoType.SABOT ? sabotPrefab : heatPrefab;
        if (prefab == null) return;
        float speed = selectedAmmo == AmmoType.SABOT ? sabotSpeed : heatSpeed;
        if (!tankTurret.SetBallisticElevation(lasedTargetPoint, muzzle.position, speed, prefab.gravity))
            Debug.LogWarning("No valid ballistic elevation for this target/range.");
    }

    void Fire()
{
    if (weaponNetwork == null)
    {
        Debug.LogWarning("TankWeaponNetwork is not assigned!");
        return;
    }

    weaponNetwork.RequestFire(selectedAmmo);
}
}

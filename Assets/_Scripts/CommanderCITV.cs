using UnityEngine;
using UnityEngine.InputSystem;

public class CommanderCITV : MonoBehaviour
{
    public Transform sightPivot;
    public Camera citvCamera;
    public TankTurret tankTurret;
    public float traverseSpeed = 65f;
    public float elevationSpeed = 40f;
    public float minPitch = -20f, maxPitch = 45f;
    public float designationRange = 4000f;
    public bool controlsEnabled = true;

    float pitch;
    bool designating;
    Vector3 designatedPoint;

    void Update()
    {
        if (!controlsEnabled || !sightPivot || !citvCamera || !tankTurret ||
            Mouse.current == null || Keyboard.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        sightPivot.Rotate(0f, delta.x * traverseSpeed * 0.01f * Time.deltaTime,
                          0f, Space.World);
        pitch = Mathf.Clamp(pitch - delta.y * elevationSpeed * 0.01f * Time.deltaTime,
                            minPitch, maxPitch);
        Vector3 angles = sightPivot.localEulerAngles;
        sightPivot.localEulerAngles = new Vector3(pitch, angles.y, 0f);

        // Space designates and commands the turret; TankGunner yields manual aiming while held.
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Ray ray = new Ray(citvCamera.transform.position, citvCamera.transform.forward);
            designatedPoint = Physics.Raycast(ray, out RaycastHit hit, designationRange)
                ? hit.point : ray.GetPoint(designationRange);
            designating = true;
        }
        if (Keyboard.current.spaceKey.wasReleasedThisFrame) designating = false;
        if (designating) tankTurret.SlewTo(designatedPoint);
    }
}


using UnityEngine;
using UnityEngine.InputSystem;

public class CommanderCITV : MonoBehaviour
{
    [Header("CITV References")]
    public Transform sightPivot;
    public Camera citvCamera;
    public TankTurret tankTurret;

    [Header("Mouse Controls")]
    [Range(0.01f, 2f)]
    public float mouseSensitivity = 0.5f;

    [Header("Sight Limits")]
    public float minPitch = -20f;
    public float maxPitch = 45f;

    [Header("Target Designation")]
    public float designationRange = 4000f;
    public bool enableDesignation = false;

    [Header("Station Controls")]
    public bool controlsEnabled = false;

    private float pitch;
    private bool cursorCaptured;

    private bool designating;
    private Vector3 designatedPoint;

    void Start()
    {
        if (sightPivot != null)
        {
            float startingPitch = sightPivot.localEulerAngles.x;

            if (startingPitch > 180f)
                startingPitch -= 360f;

            pitch = Mathf.Clamp(
                startingPitch,
                minPitch,
                maxPitch
            );
        }
    }

    void OnDisable()
    {
        ReleaseCursor();
        designating = false;
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
        if (!controlsEnabled)
        {
            if (cursorCaptured)
                ReleaseCursor();

            designating = false;
            return;
        }

        if (Mouse.current == null ||
            Keyboard.current == null ||
            sightPivot == null ||
            citvCamera == null)
            return;

        // Escape releases the mouse.
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ReleaseCursor();
            return;
        }

        // Capture mouse when entering the station.
        if (!cursorCaptured)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
                CaptureCursor();

            return;
        }

        // Mouse delta is already frame-based.
        // Do not multiply by Time.deltaTime.
        Vector2 delta = Mouse.current.delta.ReadValue();

        // Horizontal CITV rotation.
        sightPivot.Rotate(
            0f,
            delta.x * mouseSensitivity,
            0f,
            Space.World
        );

        // Vertical CITV rotation.
        pitch = Mathf.Clamp(
            pitch - delta.y * mouseSensitivity,
            minPitch,
            maxPitch
        );

        Vector3 angles = sightPivot.localEulerAngles;

        sightPivot.localEulerAngles = new Vector3(
            pitch,
            angles.y,
            0f
        );

        // Target designation will be networked next.
        if (!enableDesignation || tankTurret == null)
            return;

        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Ray ray = new Ray(
                citvCamera.transform.position,
                citvCamera.transform.forward
            );

            designatedPoint = Physics.Raycast(
                ray,
                out RaycastHit hit,
                designationRange
            )
                ? hit.point
                : ray.GetPoint(designationRange);

            designating = true;
        }

        if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            designating = false;

        if (designating)
            tankTurret.SlewTo(designatedPoint);
    }
}

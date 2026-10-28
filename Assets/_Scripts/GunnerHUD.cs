using UnityEngine;

// Gunner HUD with a reticle texture based on the supplied sight reference.
// Place AbramsReticle.png in Assets/Resources/AbramsReticle.png.
public class GunnerHUD : MonoBehaviour
{
    public TankGunner gunner;
    public Texture2D reticleTexture;
    [Range(0.2f, 2f)] public float reticleScale = 1f;
    public Color readoutColor = new Color(1f, 0.15f, 0.15f, 1f);

    private GUIStyle readoutStyle;
    private GUIStyle headingStyle;

    void Awake()
    {
        if (reticleTexture == null)
            reticleTexture = Resources.Load<Texture2D>("AbramsReticle");
    }

    void OnGUI()
    {
        if (gunner == null || !gunner.controlsEnabled ||
            gunner.gunnerCamera == null || !gunner.gunnerCamera.isActiveAndEnabled)
            return;

        EnsureStyles();

        if (reticleTexture != null)
        {
            float width = Mathf.Min(Screen.width * 0.85f, reticleTexture.width * reticleScale);
            float height = width * reticleTexture.height / reticleTexture.width;
            GUI.DrawTexture(new Rect((Screen.width - width) * 0.5f,
                                     (Screen.height - height) * 0.5f,
                                     width, height), reticleTexture, ScaleMode.StretchToFill, true);
        }

        GUI.Label(new Rect(24, 18, 350, 32), "GPS  |  GUNNER", headingStyle);
        GUI.Label(new Rect(24, 55, 350, 30), gunner.hasLased
            ? $"RANGE  {gunner.lastLasedRange:0} m"
            : "RANGE  ---- m", readoutStyle);
        GUI.Label(new Rect(24, 88, 350, 30),
            $"INDEX: {gunner.selectedAmmo}  ({gunner.selectedRounds} RDS)", readoutStyle);
        string status = gunner.selectedRounds <= 0 ? "EMPTY" :
            gunner.reloadRemaining > 0 ? $"RELOAD  {gunner.reloadRemaining:0.0}s" : "READY";
        GUI.Label(new Rect(24, 121, 350, 30), status, readoutStyle);
        GUI.Label(new Rect(24, Screen.height - 42, 700, 30),
            "1 SABOT   2 HEAT   RMB LASE   LMB FIRE", readoutStyle);
    }

    void EnsureStyles()
    {
        if (readoutStyle != null) return;
        readoutStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };
        readoutStyle.normal.textColor = readoutColor;
        headingStyle = new GUIStyle(readoutStyle) { fontSize = 22 };
    }
}

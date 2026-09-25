using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Simple third-person controller for the Catmurai model.
/// WASD / arrows / left stick = move (camera relative), Space / gamepad South = jump,
/// J / left mouse / gamepad West = sword slash.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class CatmuraiPlayerController : MonoBehaviour
{
    public Animator animator;
    public Transform model;

    [Header("Movement")]
    public float moveSpeed = 2.4f;   // 2026-09-24: was 3.4 (Lisvi Senpai chose 2.4 so the short legs can keep up)
    [Tooltip("How fast the character speeds up / slows down (m/s per second).")]
    public float acceleration = 16f;
    public float deceleration = 22f;
    [Tooltip("Ground speed the Run clip is authored for at playback speed 1 (Anim_Run_ue.fbx, 2026-09-24). The Run state is sped up/slowed down to match the real speed so the feet stay planted.")]
    public float runClipSpeed = 0.85f;
    public float turnSpeed = 720f;
    public float gravity = -20f;

    [Header("Jump")]
    [Tooltip("Peak jump height in metres.")]
    public float jumpHeight = 1.1f;
    [Tooltip("Grace time after walking off a ledge during which a jump still works.")]
    public float coyoteTime = 0.12f;
    [Tooltip("A jump pressed this long before landing still triggers on landing.")]
    public float jumpBuffer = 0.12f;
    [Tooltip("Gravity multiplier while falling (snappier landings).")]
    public float fallGravityMultiplier = 1.6f;
    [Range(0f, 1f)] public float airControl = 0.85f;

    [Header("Slash")]
    public float slashDuration = 0.95f;
    [Range(0f, 1f)] public float slashMoveFactor = 0.2f;

    [Header("Slash hit")]
    [Tooltip("Fraction of slashDuration elapsed when the hit is checked (roughly the moment the blade connects).")]
    [Range(0f, 1f)] public float slashImpactFraction = 0.35f;
    public float slashRange = 1.6f; // 2026-09-24: was 1.1, raised to match the 3x katana (~1.5 m blade)
    public float slashArcDegrees = 110f;
    public float slashDamage = 12f;

    [Header("Health")]
    public Health health;

    [Header("Secondary motion")]
    public bool useSpringBones = true;

    [Header("Model")]
    [Tooltip("Extra yaw applied to the visual model so its forward matches the controller forward.")]
    public float modelYawOffset = 0f;

    CharacterController cc;
    float verticalVelocity;
    float slashTimer;
    bool slashHitApplied;
    float lastGroundedTime = -10f;
    float lastJumpPressTime = -10f;
    bool jumping;

    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int SlashHash = Animator.StringToHash("Slash");
    static readonly int RunSpeedHash = Animator.StringToHash("RunSpeed");
    Vector3 planarVelocity;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
        if (animator)
        {
            animator.applyRootMotion = false;
            if (useSpringBones) CatmuraiSpringBones.AddDefault(animator.gameObject);
        }
        if (!health) health = GetComponent<Health>();
        if (!health) health = gameObject.AddComponent<Health>();
    }

    void Start()
    {
        if (model) model.localRotation = Quaternion.Euler(0f, modelYawOffset, 0f);
    }

    void Update()
    {
        var kb = Keyboard.current;
        var ms = Mouse.current;
        var gp = Gamepad.current;

        Vector2 input = Vector2.zero;
        if (kb != null)
        {
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) input.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) input.y -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) input.x += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) input.x -= 1f;
        }
        if (gp != null) input += gp.leftStick.ReadValue();
        input = Vector2.ClampMagnitude(input, 1f);

        bool slashPressed =
            (kb != null && kb.jKey.wasPressedThisFrame) ||
            (ms != null && ms.leftButton.wasPressedThisFrame) ||
            (gp != null && gp.buttonWest.wasPressedThisFrame);

        bool jumpPressed =
            (kb != null && kb.spaceKey.wasPressedThisFrame) ||
            (gp != null && gp.buttonSouth.wasPressedThisFrame);
        bool jumpHeld =
            (kb != null && kb.spaceKey.isPressed) ||
            (gp != null && gp.buttonSouth.isPressed);
        if (jumpPressed) lastJumpPressTime = Time.time;

        if (slashPressed && slashTimer <= 0f)
        {
            slashTimer = slashDuration;
            slashHitApplied = false;
            if (animator) animator.SetTrigger(SlashHash);
            Debug.Log("[Catmurai] Slash!");
        }
        if (slashTimer > 0f)
        {
            slashTimer -= Time.deltaTime;
            float elapsed = slashDuration - Mathf.Max(slashTimer, 0f);
            if (!slashHitApplied && elapsed >= slashDuration * slashImpactFraction)
            {
                slashHitApplied = true;
                ApplySlashHit();
            }
        }
        bool slashing = slashTimer > 0f;

        // camera-relative movement on the ground plane
        Transform cam = Camera.main ? Camera.main.transform : null;
        Vector3 fwd = cam ? Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized : Vector3.forward;
        Vector3 right = cam ? Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized : Vector3.right;
        Vector3 dir = fwd * input.y + right * input.x;

        if (dir.sqrMagnitude > 0.0001f && !slashing)
        {
            Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeed * Time.deltaTime);
        }

        bool grounded = cc.isGrounded;
        Vector3 targetVelocity = dir * moveSpeed * (slashing ? slashMoveFactor : 1f) * (grounded ? 1f : airControl);
        float rate = targetVelocity.sqrMagnitude > planarVelocity.sqrMagnitude ? acceleration : deceleration;
        planarVelocity = Vector3.MoveTowards(planarVelocity, targetVelocity, rate * Time.deltaTime);
        Vector3 horizontal = planarVelocity;

        if (grounded)
        {
            lastGroundedTime = Time.time;
            if (verticalVelocity < 0f) { verticalVelocity = -1f; jumping = false; }
        }

        // jump (with coyote time + input buffer)
        bool canJump = Time.time - lastGroundedTime <= coyoteTime && !jumping;
        if (canJump && Time.time - lastJumpPressTime <= jumpBuffer)
        {
            verticalVelocity = Mathf.Sqrt(2f * jumpHeight * -gravity);
            jumping = true;
            lastJumpPressTime = -10f;
            lastGroundedTime = -10f;
            Debug.Log("[Catmurai] Jump!");
        }

        // falling or jump released early -> heavier gravity for a snappier arc
        float g = gravity;
        if (verticalVelocity < 0f || (jumping && !jumpHeld)) g *= fallGravityMultiplier;
        verticalVelocity += g * Time.deltaTime;

        cc.Move((horizontal + Vector3.up * verticalVelocity) * Time.deltaTime);

        if (animator)
        {
            float planarSpeed = new Vector3(cc.velocity.x, 0f, cc.velocity.z).magnitude;
            float target = (slashing || !grounded) ? 0f : Mathf.Clamp01(planarSpeed / Mathf.Max(0.01f, moveSpeed));
            animator.SetFloat(SpeedHash, target, 0.06f, Time.deltaTime);
            // play the Run clip at the rate that matches the real ground speed (no foot sliding)
            animator.SetFloat(RunSpeedHash, Mathf.Clamp(planarSpeed / Mathf.Max(0.05f, runClipSpeed), 0.8f, 4f));
        }
    }

    /// <summary>
    /// Damages every live Health in range and within the slash arc in front of the player.
    /// Uses the Health static registry rather than physics colliders, so enemy prefabs don't need hitbox setup.
    /// </summary>
    void ApplySlashHit()
    {
        Vector3 origin = transform.position;
        for (int i = 0; i < Health.All.Count; i++)
        {
            var h = Health.All[i];
            if (!h || h.IsDead || h == health) continue;
            Vector3 to = h.transform.position - origin; to.y = 0f;
            float dist = to.magnitude;
            if (dist > slashRange) continue;
            if (dist > 0.05f && Vector3.Angle(transform.forward, to) > slashArcDegrees * 0.5f) continue;
            h.TakeDamage(slashDamage);
        }
    }

    [Header("Debug")]
    public bool showHud = true;

    void OnGUI()
    {
        if (!showHud || !animator) return;
        var clips = animator.GetCurrentAnimatorClipInfo(0);
        string clip = clips.Length > 0 ? clips[0].clip.name : "(none)";
        var st = animator.GetCurrentAnimatorStateInfo(0);
        string hp = health ? $"  hp={health.current:0}/{health.maxHealth:0}" : "";
        GUI.Label(new Rect(10, 10, 700, 24),
            "Catmurai  clip: " + clip + "  t=" + st.normalizedTime.ToString("0.00") +
            "  speed=" + animator.GetFloat(SpeedHash).ToString("0.00") + hp +
            "   [WASD move, Space jump, J / click = slash]");
    }
}

using UnityEngine;

/// <summary>
/// Bleep (temp name): first survival-game enemy. A floating shadow-cat ball that drifts toward the player,
/// bobs, squashes, sways, and lunges to attack in range. Purely procedural (no skeleton).
/// Visual = child with the core mesh, Smoke = child with the smoke cards.
/// </summary>
[RequireComponent(typeof(Health))]
public class BleepEnemy : MonoBehaviour
{
    public Transform target;
    public Transform visual;
    public Transform smoke;
    public Health health;

    [Header("Movement")]
    public float speed = 1.3f;
    public float hoverHeight = 0.42f;
    public float bobAmplitude = 0.06f;
    public float bobFrequency = 2.0f;
    public float turnSpeed = 300f;
    public float stopDistance = 0.55f;

    [Header("Aggro (0 = always chase, like the test scene)")]
    [Tooltip("Start chasing the player when closer than this. 0 = always chase.")]
    public float detectRange = 0f;
    [Tooltip("Stop chasing when the player is this far from the Bleep's home. 0 = no leash.")]
    public float leashRange = 0f;
    [Tooltip("Idle drifting radius around home when not chasing.")]
    public float wanderRadius = 2.5f;
    [HideInInspector] public Vector3 home;
    [HideInInspector] public bool hasHome;

    [Header("Ground following")]
    [Tooltip("Hover above the ground below (raycast). Off = old behaviour, fixed world height.")]
    public bool followGround = true;
    public float groundProbe = 40f;

    [Header("Look")]
    [Tooltip("Extra yaw of the visual so the painted face looks along +Z of this object.")]
    public float visualYaw = 0f;
    public float squash = 0.04f;
    public float swayDegrees = 5f;

    [Header("Attack")]
    public float attackRange = 0.75f;
    public float attackDamage = 8f;
    public float attackCooldown = 1.6f;
    public float attackWindup = 0.28f;
    public float attackLunge = 0.18f;

    [Header("Reactions")]
    public Color hitFlashColor = new Color(1f, 1f, 1f, 1f);
    public float hitFlashDuration = 0.12f;
    public float deathFadeDuration = 0.6f;

    float phase;
    Vector3 visualBaseScale = Vector3.one;
    Vector3 basePos;

    float attackTimer;      // counts down to next allowed attack
    float attackWindTimer;  // >0 while winding up / lunging
    bool attackApplied;
    bool dying;
    float deathTimer;

    bool aggro;
    Vector3 wanderPoint;
    float wanderTimer;
    float groundY;

    Material[] visualMats;
    Color[] visualBaseColors;
    float hitFlashTimer;

    void Start()
    {
        phase = Random.value * 20f;
        if (!health) health = GetComponent<Health>();
        if (!target)
        {
            var p = GameObject.Find("Player");
            if (p) target = p.transform;
        }
        if (!hasHome) { home = transform.position; hasHome = true; }
        wanderPoint = home;
        groundY = followGround ? GroundHeight(transform.position, transform.position.y - hoverHeight) : 0f;
        if (visual)
        {
            visualBaseScale = visual.localScale;
            var renderers = visual.GetComponentsInChildren<Renderer>();
            var mats = new System.Collections.Generic.List<Material>();
            var cols = new System.Collections.Generic.List<Color>();
            foreach (var r in renderers)
            {
                foreach (var m in r.materials) // instances (per-object copies)
                {
                    mats.Add(m);
                    cols.Add(m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : Color.white);
                }
            }
            visualMats = mats.ToArray();
            visualBaseColors = cols.ToArray();
        }
        if (health)
        {
            health.OnDamaged += OnDamaged;
            health.OnDeath += OnDeath;
        }
    }

    void OnDamaged(float amount)
    {
        hitFlashTimer = hitFlashDuration;
    }

    void OnDeath()
    {
        dying = true;
        deathTimer = 0f;
    }

    void Update()
    {
        if (dying)
        {
            UpdateDeath();
            return;
        }

        float t = Time.time + phase;
        Vector3 pos = transform.position;
        float distToTarget = -1f;
        Vector3 toTarget = Vector3.zero;

        if (target)
        {
            toTarget = target.position - pos; toTarget.y = 0f;
            distToTarget = toTarget.magnitude;
            var th0 = target.GetComponent<Health>();
            bool targetAlive = !th0 || !th0.IsDead;
            bool leashed = leashRange > 0f && Vector3.Distance(Flat(home), Flat(target.position)) > leashRange;
            if (detectRange <= 0f) aggro = targetAlive && !leashed;
            else if (!aggro) aggro = targetAlive && !leashed && distToTarget <= detectRange;
            else if (!targetAlive || leashed || distToTarget > detectRange * 1.6f) aggro = false;
        }
        else aggro = false;

        bool inAttack = attackWindTimer > 0f;
        if (aggro)
        {
            if (!inAttack && distToTarget > stopDistance) pos += toTarget / Mathf.Max(distToTarget, 0.0001f) * speed * Time.deltaTime;
            if (distToTarget > 0.05f) Face(toTarget);
        }
        else
        {
            // idle: drift between random points around home
            wanderTimer -= Time.deltaTime;
            Vector3 toW = wanderPoint - pos; toW.y = 0f;
            if (wanderTimer <= 0f || toW.magnitude < 0.2f)
            {
                Vector2 r = Random.insideUnitCircle * wanderRadius;
                wanderPoint = home + new Vector3(r.x, 0f, r.y);
                wanderTimer = Random.Range(2.5f, 5f);
            }
            if (toW.magnitude > 0.2f)
            {
                pos += toW.normalized * speed * 0.4f * Time.deltaTime;
                Face(toW);
            }
        }

        if (followGround)
        {
            float g = GroundHeight(pos, groundY);
            groundY = Mathf.Lerp(groundY, g, 1f - Mathf.Exp(-6f * Time.deltaTime));
        }
        pos.y = groundY + hoverHeight + Mathf.Sin(t * bobFrequency) * bobAmplitude;
        transform.position = pos;
        basePos = pos;

        // attack state machine
        if (attackTimer > 0f) attackTimer -= Time.deltaTime;
        if (attackWindTimer > 0f)
        {
            attackWindTimer -= Time.deltaTime;
            float elapsed = attackWindup - Mathf.Max(attackWindTimer, 0f);
            if (!attackApplied && elapsed >= attackWindup * 0.7f)
            {
                attackApplied = true;
                if (target && distToTarget >= 0f && distToTarget <= attackRange + 0.15f)
                {
                    var th = target.GetComponent<Health>();
                    if (th) th.TakeDamage(attackDamage);
                }
            }
        }
        else if (aggro && target && distToTarget >= 0f && distToTarget <= attackRange && attackTimer <= 0f)
        {
            attackWindTimer = attackWindup;
            attackApplied = false;
            attackTimer = attackCooldown;
        }

        if (visual)
        {
            float lunge = attackWindTimer > 0f ? Mathf.Sin((1f - attackWindTimer / attackWindup) * Mathf.PI) * attackLunge : 0f;
            float s = 1f + Mathf.Sin(t * bobFrequency * 2f) * squash;
            visual.localScale = new Vector3(visualBaseScale.x * (2f - s), visualBaseScale.y * s, visualBaseScale.z * (2f - s));
            visual.localRotation = Quaternion.Euler(0f, visualYaw, Mathf.Sin(t * 1.3f) * swayDegrees);
            visual.localPosition = new Vector3(0f, 0f, lunge);
        }
        if (smoke)
        {
            smoke.localRotation = Quaternion.Euler(0f, visualYaw + Mathf.Sin(t * 0.9f) * 8f, Mathf.Sin(t * 1.7f) * 3f);
            float ss = 1f + Mathf.Sin(t * 2.3f) * 0.03f;
            smoke.localScale = Vector3.one * ss * visualBaseScale.x;
        }

        UpdateHitFlash();
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v; }

    void Face(Vector3 dir)
    {
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) return;
        var want = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, want, turnSpeed * Time.deltaTime);
    }

    static readonly RaycastHit[] hits = new RaycastHit[16];

    /// Height of the visible ground under p. Ignores triggers, the player (CharacterController)
    /// and renderer-less helper colliders (invisible rails / edge walls). Falls back to 'fallback'
    /// over empty sky, so a Bleep drifting past an island edge keeps its height.
    float GroundHeight(Vector3 p, float fallback)
    {
        Vector3 origin = new Vector3(p.x, p.y + 1.5f, p.z);
        int n = Physics.RaycastNonAlloc(origin, Vector3.down, hits, groundProbe, ~0, QueryTriggerInteraction.Ignore);
        float best = float.MaxValue, y = fallback;
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c is CharacterController) continue;
            if (target && c.transform.IsChildOf(target)) continue;
            if (!(c is TerrainCollider) && !c.GetComponent<Renderer>()) continue;
            if (hits[i].distance < best) { best = hits[i].distance; y = hits[i].point.y; }
        }
        return y;
    }

    void UpdateHitFlash()
    {
        if (hitFlashTimer <= 0f || visualMats == null) return;
        hitFlashTimer -= Time.deltaTime;
        float k = Mathf.Clamp01(hitFlashTimer / hitFlashDuration);
        for (int i = 0; i < visualMats.Length; i++)
        {
            if (!visualMats[i] || !visualMats[i].HasProperty("_BaseColor")) continue;
            visualMats[i].SetColor("_BaseColor", Color.Lerp(visualBaseColors[i], hitFlashColor, k));
        }
    }

    void UpdateDeath()
    {
        deathTimer += Time.deltaTime;
        float k = Mathf.Clamp01(deathTimer / deathFadeDuration);
        float s = 1f - k;
        transform.localScale = Vector3.one * s;
        transform.position = basePos + Vector3.up * (k * 0.3f);
        if (k >= 1f) Destroy(gameObject);
    }
}

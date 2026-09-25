using UnityEngine;
using UnityEngine.SceneManagement;

// 2026-09-25 (Claude): face animation for Catmurai.
// Drives the "Blink" and "MouthOpen" blend shapes on the LOD0 body mesh (v10.8):
//  - random blinks (sometimes a double blink),
//  - the mouth opens a little on each breath; faster, deeper panting while running,
//  - a short open-mouth "shout" during the Slash state.
// Added automatically to every CatmuraiPlayerController at runtime, so no prefab or scene edit is needed.
// LOD1-3 have no face shapes; the component simply skips meshes without them.
[DisallowMultipleComponent]
public class CatmuraiFaceAnimator : MonoBehaviour
{
    [Header("Blink")]
    public Vector2 blinkInterval = new Vector2(2.2f, 5.5f);
    public float closeTime = 0.06f, holdTime = 0.05f, openTime = 0.11f;
    [Range(0, 1)] public float doubleBlinkChance = 0.2f;

    [Header("Breathing mouth")]
    public float breathPeriod = 3.4f;
    [Range(0, 1)] public float breathOpen = 0.28f;
    public float runBreathPeriod = 0.6f;
    [Range(0, 1)] public float runOpen = 0.6f;
    [Range(0, 1)] public float slashOpen = 0.9f;
    public float mouthSmoothing = 12f;

    [Header("Ears (calmer spring settings, applied to CatmuraiSpringBones)")]
    public bool calmEars = true;
    [Range(0, 1)] public float earStiffness = 0.6f;
    [Range(0, 1)] public float earDrag = 0.35f;
    [Range(0, 90)] public float earMaxAngle = 6f;

    public Animator animator;
    bool earsDone;

    SkinnedMeshRenderer[] smrs = new SkinnedMeshRenderer[0];
    int[] blinkIdx, mouthIdx;
    float nextBlink, blinkClock = -1f, breathPhase, mouthNow, runBlend, refreshTimer;
    int extraBlinks;
    static readonly int SpeedHash = Animator.StringToHash("Speed");
    static readonly int SlashState = Animator.StringToHash("Slash");

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        AttachAll();
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    static void OnSceneLoaded(Scene s, LoadSceneMode m) => AttachAll();
    static void AttachAll()
    {
        foreach (var p in FindObjectsByType<CatmuraiPlayerController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!p.GetComponent<CatmuraiFaceAnimator>()) p.gameObject.AddComponent<CatmuraiFaceAnimator>();
    }

    void Start()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
        Refresh();
        nextBlink = Time.time + Random.Range(blinkInterval.x, blinkInterval.y);
        breathPhase = Random.value;
    }

    void Refresh()
    {
        var all = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        var list = new System.Collections.Generic.List<SkinnedMeshRenderer>();
        var bl = new System.Collections.Generic.List<int>(); var mo = new System.Collections.Generic.List<int>();
        foreach (var r in all)
        {
            if (!r.sharedMesh) continue;
            int b = r.sharedMesh.GetBlendShapeIndex("Blink"), m = r.sharedMesh.GetBlendShapeIndex("MouthOpen");
            if (b < 0 && m < 0) continue;
            list.Add(r); bl.Add(b); mo.Add(m);
        }
        smrs = list.ToArray(); blinkIdx = bl.ToArray(); mouthIdx = mo.ToArray();
        if (calmEars && !earsDone)
        {
            // the ear tips swayed up to 20 deg and bent the ear like rubber; ears are stiff cartilage
            foreach (var sb in GetComponentsInChildren<CatmuraiSpringBones>(true))
                foreach (var g in sb.groups)
                    if (g.name == "Ears") { g.stiffness = earStiffness; g.drag = earDrag; g.gravity = 0f; g.maxAngle = earMaxAngle; earsDone = true; }
        }
    }

    void LateUpdate()
    {
        // meshes can be swapped (Tail Fix, LOD setup) - look again now and then
        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f) { refreshTimer = (smrs.Length == 0 || (calmEars && !earsDone)) ? 1f : 5f; Refresh(); }
        if (smrs.Length == 0) return;

        // --- blink
        float blink = 0f;
        if (blinkClock < 0f && Time.time >= nextBlink) blinkClock = 0f;
        if (blinkClock >= 0f)
        {
            blinkClock += Time.deltaTime;
            float t = blinkClock;
            if (t < closeTime) blink = t / closeTime;
            else if (t < closeTime + holdTime) blink = 1f;
            else if (t < closeTime + holdTime + openTime) blink = 1f - (t - closeTime - holdTime) / openTime;
            else
            {
                blinkClock = -1f;
                if (extraBlinks > 0) { extraBlinks--; nextBlink = Time.time + 0.12f; }
                else
                {
                    extraBlinks = Random.value < doubleBlinkChance ? 1 : 0;
                    nextBlink = Time.time + Random.Range(blinkInterval.x, blinkInterval.y);
                }
            }
            blink = Mathf.SmoothStep(0f, 1f, blink);
        }

        // --- breathing / panting mouth
        float speed = animator ? animator.GetFloat(SpeedHash) : 0f;
        runBlend = Mathf.MoveTowards(runBlend, speed > 0.25f ? 1f : 0f, Time.deltaTime * 1.5f);
        float period = Mathf.Lerp(breathPeriod, runBreathPeriod, runBlend);
        breathPhase = (breathPhase + Time.deltaTime / Mathf.Max(0.1f, period)) % 1f;
        float wave = 0.5f - 0.5f * Mathf.Cos(breathPhase * Mathf.PI * 2f);      // 0..1, open on the out-breath
        float target = Mathf.Lerp(breathOpen, runOpen, runBlend) * wave * wave;
        if (animator && animator.GetCurrentAnimatorStateInfo(0).shortNameHash == SlashState) target = slashOpen;
        mouthNow = Mathf.Lerp(mouthNow, target, 1f - Mathf.Exp(-mouthSmoothing * Time.deltaTime));

        for (int i = 0; i < smrs.Length; i++)
        {
            var r = smrs[i]; if (!r) continue;
            if (blinkIdx[i] >= 0) r.SetBlendShapeWeight(blinkIdx[i], blink * 100f);
            if (mouthIdx[i] >= 0) r.SetBlendShapeWeight(mouthIdx[i], mouthNow * 100f);
        }
    }
}

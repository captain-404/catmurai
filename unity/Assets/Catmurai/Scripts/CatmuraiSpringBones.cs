using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Lightweight Verlet spring-bone secondary motion for the Catmurai cape, tail, ears and ribbons.
/// Runs in LateUpdate (after the Animator). Each bone chain keeps a world-space "tail" particle that lags behind
/// the animated rest pose, is pulled back by stiffness, damped by drag and pushed by gravity/wind.
/// Bone names (UE skeleton): cape_c/l/r_01-03, tail_01-06, ear_l/r_01-02, ribbon_l/r_01-03.
/// </summary>
[DefaultExecutionOrder(1000)]
public class CatmuraiSpringBones : MonoBehaviour
{
    [System.Serializable]
    public class Group
    {
        public string name = "group";
        [Tooltip("Bone name prefixes, e.g. cape_l_")] public string[] prefixes;
        [Range(0f, 1f)] public float stiffness = 0.1f;   // pull towards the animated pose (per 60 fps frame)
        [Range(0f, 1f)] public float drag = 0.1f;        // velocity damping (per 60 fps frame)
        public float gravity = -3f;                      // m/s^2 (world Y)
        [Range(0f, 90f)] public float maxAngle = 60f;    // max deviation from the animated pose, degrees
        [Range(0f, 1f)] public float rootInfluence = 1f; // 0 = first bone of chain stays rigid, 1 = fully sprung
        [Tooltip("Last N bones of each chain get no spring of their own: they simply follow the rest of the chain.")]
        [Range(0, 10)] public int rigidTipBones = 0;
        [Tooltip("Max angle at the chain tip as a fraction of Max Angle (1 or 0 = same everywhere). Stops the swing from adding up along long chains.")]
        [Range(0f, 1f)] public float tipAngleScale = 1f;
    }

    public Vector3 wind = Vector3.zero;
    public float windTurbulence = 0f;
    public bool enableSprings = true;
    public List<Group> groups = new List<Group>();

    class Bone
    {
        public Transform t, child, parent;
        public Vector3 localAxis; public float length;
        public Vector3 tail, prevTail; public bool init;
        public Group g; public int index, count;
        public Quaternion baseLocal, lastApplied; public bool hasLast;
    }
    readonly List<Bone> bones = new List<Bone>();
    bool built;

    public static CatmuraiSpringBones AddDefault(GameObject go)
    {
        var sb = go.GetComponent<CatmuraiSpringBones>();
        if (!sb) sb = go.AddComponent<CatmuraiSpringBones>();
        return sb;
    }

    void Reset() { ApplyDefaults(); }

    void ApplyDefaults()
    {
        groups = new List<Group>
        {
            new Group{ name="Cape",   prefixes=new[]{"cape_c_","cape_l_","cape_r_"}, stiffness=0.07f, drag=0.10f, gravity=-6f,  maxAngle=55f, rootInfluence=0.5f },
            new Group{ name="Tail",   prefixes=new[]{"tail_"},                        stiffness=0.25f, drag=0.20f, gravity=-1f,  maxAngle=20f, rootInfluence=0.2f, rigidTipBones=2, tipAngleScale=0.4f },
            new Group{ name="Ears",   prefixes=new[]{"ear_l_","ear_r_"},              stiffness=0.30f, drag=0.25f, gravity=-1f,  maxAngle=20f, rootInfluence=0.0f },
            new Group{ name="Ribbons",prefixes=new[]{"ribbon_l_","ribbon_r_"},        stiffness=0.05f, drag=0.06f, gravity=-4f,  maxAngle=70f, rootInfluence=0.6f },
        };
    }

    void Awake()
    {
        if (groups == null || groups.Count == 0) ApplyDefaults();
        Build();
    }

    void Build()
    {
        bones.Clear();
        var all = GetComponentsInChildren<Transform>(true);
        var byName = new Dictionary<string, Transform>();
        foreach (var t in all) if (!byName.ContainsKey(t.name)) byName[t.name] = t;

        foreach (var g in groups)
        {
            foreach (var pre in g.prefixes)
            {
                // chains are named prefix + "01", "02", ...
                int first = bones.Count;
                for (int i = 1; i < 20; i++)
                {
                    string nm = pre + i.ToString("00");
                    if (!byName.TryGetValue(nm, out var t)) break;
                    var b = new Bone { t = t, g = g, index = i - 1, parent = t.parent };
                    string nextName = pre + (i + 1).ToString("00");
                    if (byName.TryGetValue(nextName, out var c)) b.child = c;
                    bones.Add(b);
                }
                for (int k = first; k < bones.Count; k++) bones[k].count = bones.Count - first;
            }
        }
        // per-bone axis / length (from the bind pose)
        foreach (var b in bones)
        {
            if (b.child)
            {
                Vector3 d = b.child.position - b.t.position;
                b.length = d.magnitude;
                b.localAxis = b.t.InverseTransformDirection(d).normalized;
            }
            else
            {
                // tip bone: continue the parent's direction
                var prev = bones.Find(x => x.child == b.t);
                if (prev != null)
                {
                    b.length = prev.length * 0.9f;
                    b.localAxis = b.t.InverseTransformDirection((b.t.position - prev.t.position).normalized);
                }
                else { b.length = 0.05f; b.localAxis = Vector3.down; }
            }
        }
        built = true;
    }

    void LateUpdate()
    {
        if (!built || !enableSprings) return;
        float dt = Mathf.Min(Time.deltaTime, 1f / 30f);
        if (dt <= 0f) return;
        float f60 = dt * 60f;

        // bones list is ordered root -> tip per chain
        foreach (var b in bones)
        {
            // Tip bones marked rigid keep their animated local rotation, so they just follow their (sprung) parent.
            if (b.index >= b.count - b.g.rigidTipBones) continue;

            // If the Animator did not rewrite this bone this frame (un-animated bone), undo our own previous
            // rotation so the spring never accumulates on top of itself.
            if (b.hasLast && Mathf.Abs(Quaternion.Dot(b.t.localRotation, b.lastApplied)) > 0.99999f) b.t.localRotation = b.baseLocal;
            else b.baseLocal = b.t.localRotation;

            float infl = b.index == 0 ? b.g.rootInfluence : 1f;
            Vector3 origin = b.t.position;
            Vector3 restDir = b.t.TransformDirection(b.localAxis);   // animated pose direction
            Vector3 restTail = origin + restDir * b.length;

            if (!b.init) { b.tail = restTail; b.prevTail = restTail; b.init = true; }

            Vector3 vel = (b.tail - b.prevTail) * Mathf.Pow(1f - b.g.drag, f60);
            Vector3 acc = new Vector3(0f, b.g.gravity, 0f) + wind;
            if (windTurbulence > 0f)
                acc += wind.normalized * (Mathf.PerlinNoise(Time.time * 1.7f + b.index, b.t.GetInstanceID() * 0.13f) - 0.5f) * windTurbulence;
            Vector3 next = b.tail + vel + acc * dt * dt;
            next += (restTail - next) * (1f - Mathf.Pow(1f - b.g.stiffness, f60));

            // keep bone length
            Vector3 dir = next - origin;
            if (dir.sqrMagnitude < 1e-10f) dir = restDir;
            dir.Normalize();
            // angle limit around the animated pose
            float ang = Vector3.Angle(restDir, dir);
            float maxA = b.g.maxAngle * (b.count > 1 ? Mathf.Lerp(1f, b.g.tipAngleScale > 0f ? b.g.tipAngleScale : 1f, b.index / (float)(b.count - 1)) : 1f);
            if (ang > maxA) dir = Vector3.Slerp(restDir, dir, maxA / ang).normalized;
            next = origin + dir * b.length;

            b.prevTail = b.tail;
            b.tail = next;

            // apply rotation (blend by root influence)
            Vector3 useDir = infl >= 1f ? dir : Vector3.Slerp(restDir, dir, infl).normalized;
            b.t.rotation = Quaternion.FromToRotation(restDir, useDir) * b.t.rotation;
            b.lastApplied = b.t.localRotation; b.hasLast = true;
        }
    }

    void OnDisable()
    {
        foreach (var b in bones) { b.init = false; b.hasLast = false; }
    }
}

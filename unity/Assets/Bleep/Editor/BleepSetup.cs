#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click Bleep enemy setup: texture import settings, URP materials (emissive face, alpha smoke), prefab and 4 test spawns.
/// Menu: Bleep / Build Enemy Test. Runs automatically once after the files are imported.
/// </summary>
public static class BleepSetup
{
    const string Root = "Assets/Bleep";
    const string CorePath = Root + "/Models/Bleep_Core.fbx";
    const string SmokePath = Root + "/Models/Bleep_Smoke.fbx";
    const string PrefabPath = Root + "/Bleep.prefab";

    [InitializeOnLoadMethod]
    static void AutoRun()
    {
        if (SessionState.GetBool("BleepSetupTried", false)) return;
        if (!File.Exists(CorePath) || File.Exists(PrefabPath)) return;
        SessionState.SetBool("BleepSetupTried", true);
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Bleep/Build Enemy Test")]
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();
            SetupTextures();
            var mats = MakeMaterials();
            BuildPrefabAndScene(mats);
            Debug.Log("[Bleep] Setup finished: 4 Bleeps spawned around the player.");
        }
        catch (System.Exception e) { Debug.LogError("[Bleep] Setup failed: " + e); }
    }

    static void SetupTextures()
    {
        foreach (var n in new[] { "bleep_basecolor", "bleep_emissive", "smoke_tail", "smoke_wisp" })
        {
            string p = Root + "/Textures/" + n + ".png";
            var ti = AssetImporter.GetAtPath(p) as TextureImporter;
            if (ti == null) continue;
            ti.textureType = TextureImporterType.Default;
            ti.sRGBTexture = true;
            ti.alphaIsTransparency = n.StartsWith("smoke");
            ti.mipmapEnabled = true;
            ti.maxTextureSize = n.StartsWith("bleep") ? 2048 : 1024;
            ti.wrapMode = n.StartsWith("bleep") ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.SaveAndReimport();
        }
    }

    static Texture2D Tex(string n) { return AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + n + ".png"); }

    static Material Mat(string name, string shader)
    {
        string p = Root + "/Materials/" + name + ".mat";
        Directory.CreateDirectory(Root + "/Materials");
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null) { m = new Material(Shader.Find(shader)); AssetDatabase.CreateAsset(m, p); }
        m.shader = Shader.Find(shader);
        return m;
    }

    static void Lit(Material m, Texture2D baseMap, Texture2D emissive, Color baseColor, Color emitColor, float smooth)
    {
        m.SetColor("_BaseColor", baseColor);
        if (baseMap) m.SetTexture("_BaseMap", baseMap);
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emitColor);
        if (emissive) m.SetTexture("_EmissionMap", emissive);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
    }

    static void Smoke(Material m, Texture2D tex)
    {
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", new Color(1f, 1f, 1f, 1f));
        m.SetFloat("_Surface", 1f);
        m.SetFloat("_Blend", 0f);
        m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_ZWrite", 0f);
        m.SetFloat("_Cull", 0f);
        m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        m.SetOverrideTag("RenderType", "Transparent");
        m.renderQueue = (int)RenderQueue.Transparent;
        m.SetShaderPassEnabled("ShadowCaster", false);
        EditorUtility.SetDirty(m);
    }

    static System.Collections.Generic.Dictionary<string, Material> MakeMaterials()
    {
        var d = new System.Collections.Generic.Dictionary<string, Material>();
        var body = Mat("Bleep_Body", "Universal Render Pipeline/Lit");
        Lit(body, Tex("bleep_basecolor"), Tex("bleep_emissive"), Color.white, Color.white * 1.6f, 0.35f);
        var eo = Mat("Bleep_EarOuter", "Universal Render Pipeline/Lit");
        Lit(eo, null, null, new Color(0.04f, 0.02f, 0.07f), Color.black, 0.3f);
        var ei = Mat("Bleep_EarInner", "Universal Render Pipeline/Lit");
        Lit(ei, null, null, new Color(0.12f, 0.03f, 0.3f), new Color(0.55f, 0.18f, 1.4f), 0.2f);
        var st = Mat("Bleep_SmokeTail", "Universal Render Pipeline/Unlit"); Smoke(st, Tex("smoke_tail"));
        var sw = Mat("Bleep_SmokeWisp", "Universal Render Pipeline/Unlit"); Smoke(sw, Tex("smoke_wisp"));
        d["Bleep_Body"] = body; d["Bleep_EarOuter"] = eo; d["Bleep_EarInner"] = ei; d["Bleep_SmokeTail"] = st; d["Bleep_SmokeWisp"] = sw;
        AssetDatabase.SaveAssets();
        return d;
    }

    static void AssignMats(GameObject go, System.Collections.Generic.Dictionary<string, Material> mats)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            var cur = r.sharedMaterials;
            for (int i = 0; i < cur.Length; i++)
            {
                string n = cur[i] ? cur[i].name : "";
                if (mats.TryGetValue(n, out var m)) cur[i] = m;
                else Debug.LogWarning("[Bleep] no material mapping for slot '" + n + "' on " + r.name);
            }
            r.sharedMaterials = cur;
            r.shadowCastingMode = ShadowCastingMode.Off;
        }
    }

    static void BuildPrefabAndScene(System.Collections.Generic.Dictionary<string, Material> mats)
    {
        var core = AssetDatabase.LoadAssetAtPath<GameObject>(CorePath);
        var smoke = AssetDatabase.LoadAssetAtPath<GameObject>(SmokePath);
        if (core == null) { Debug.LogError("[Bleep] missing " + CorePath); return; }

        var root = new GameObject("Bleep");
        var vis = (GameObject)PrefabUtility.InstantiatePrefab(core); vis.name = "Visual";
        vis.transform.SetParent(root.transform, false); vis.transform.localScale = Vector3.one * 0.5f;
        AssignMats(vis, mats);
        GameObject sm = null;
        if (smoke != null)
        {
            sm = (GameObject)PrefabUtility.InstantiatePrefab(smoke); sm.name = "Smoke";
            sm.transform.SetParent(root.transform, false); sm.transform.localScale = Vector3.one * 0.5f;
            AssignMats(sm, mats);
        }
        var be = root.AddComponent<BleepEnemy>(); // also adds Health via [RequireComponent]
        be.visual = vis.transform; be.smoke = sm ? sm.transform : null;
        be.health = root.GetComponent<Health>();
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);

        var scene = SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects().Where(g => g.name.StartsWith("Bleep")).ToArray()) Object.DestroyImmediate(go);
        for (int i = 0; i < 4; i++)
        {
            float a = Mathf.Deg2Rad * (45f + i * 90f);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.name = "Bleep" + i;
            inst.transform.position = new Vector3(Mathf.Sin(a) * 4f, 0.42f, Mathf.Cos(a) * 4f + 0.5f);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
#endif

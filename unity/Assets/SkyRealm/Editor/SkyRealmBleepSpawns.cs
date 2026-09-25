#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Places Bleep spawn points (glowing summoning circles) around the Sky Realm.
/// Called by Build World; also available as menu Catmurai / Sky Realm / Add Bleep Spawners
/// (updates the open scene without rebuilding the world).
/// </summary>
public static class SkyRealmBleepSpawns
{
    const string BleepPrefab = "Assets/Bleep/Bleep.prefab";
    const string RuneMat = "Assets/SkyRealm/Materials/SR_RuneGlow.mat";

    // (position on the walkable ground, max alive)
    static readonly (Vector3 pos, int max)[] Points =
    {
        (new Vector3(-4.6f, 1.5f, 2.6f), 2),   // rune plaza, west half
        (new Vector3(4.6f, 1.5f, 2.6f), 2),    // rune plaza, east half
        (new Vector3(21.6f, 1f, 2.8f), 2),     // east shrine island
        (new Vector3(-20.6f, 2.5f, 4.4f), 2),  // west ruins island
        (new Vector3(-5f, 5f, 19.2f), 2),      // shrine terrace, left
        (new Vector3(5f, 5f, 19.2f), 2),       // shrine terrace, right
    };

    [MenuItem("Catmurai/Sky Realm/Add Bleep Spawners")]
    public static void AddMenu()
    {
        Add();
        var scene = EditorSceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    public static void Add()
    {
        var old = GameObject.Find("BleepSpawners");
        if (old) Object.DestroyImmediate(old);

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BleepPrefab);
        if (!prefab) { Debug.LogWarning("[SkyRealm] Bleep prefab not found at " + BleepPrefab + " (run Bleep > Build Enemy Test)."); return; }
        var mat = AssetDatabase.LoadAssetAtPath<Material>(RuneMat);

        var root = new GameObject("BleepSpawners").transform;
        int i = 0;
        foreach (var (pos, max) in Points)
        {
            var sp = new GameObject("BleepSpawn_" + i++);
            sp.transform.SetParent(root, false);
            sp.transform.position = pos;

            // summoning circle
            var circle = GameObject.CreatePrimitive(PrimitiveType.Quad);
            circle.name = "SummonCircle";
            Object.DestroyImmediate(circle.GetComponent<Collider>());
            circle.transform.SetParent(sp.transform, false);
            circle.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            circle.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            circle.transform.localScale = Vector3.one * 2.6f;
            var mr = circle.GetComponent<MeshRenderer>();
            if (mat) mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var spin = circle.AddComponent<SkyRealmSpin>();
            spin.axis = Vector3.forward;
            spin.degreesPerSecond = 20f;

            var lgo = new GameObject("SummonLight");
            lgo.transform.SetParent(sp.transform, false);
            lgo.transform.localPosition = new Vector3(0f, 0.8f, 0f);
            var l = lgo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(0.72f, 0.38f, 1f);
            l.intensity = 1.5f;
            l.range = 4f;
            l.shadows = LightShadows.None;

            var s = sp.AddComponent<BleepSpawner>();
            s.bleepPrefab = prefab;
            s.maxAlive = max;
            s.glow = l;
            s.firstSpawnDelay = 1f + i * 0.4f;
        }
        Debug.Log("[SkyRealm] Added " + Points.Length + " Bleep spawners.");
    }
}
#endif

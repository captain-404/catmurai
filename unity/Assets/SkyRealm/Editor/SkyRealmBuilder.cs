#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

/// <summary>
/// Builds "Sky Realm": a moonlit world of floating islands for Catmurai (purple night, rune plaza,
/// torii, stone lanterns, rope bridges, crystals, waterfalls, sea of clouds, giant Catmurai statue).
/// Menu: Catmurai / Sky Realm / Build World.  Output scene: Assets/SkyRealm/SkyRealm.unity.
/// Textures: CC0 from Poly Haven (downloaded on first build) + procedurally painted ones.
/// </summary>
public static partial class SkyRealmBuilder
{
    const string ScenePath = Root + "/SkyRealm.unity";
    const string LogPath = "Logs/SkyRealmBuild.log";
    static readonly Vector3 Spawn = new Vector3(0f, 0.05f, -28f);

    static void Log(string msg)
    {
        Debug.Log("[SkyRealm] " + msg);
        try { Directory.CreateDirectory("Logs"); File.AppendAllText(LogPath, System.DateTime.Now.ToString("HH:mm:ss") + " " + msg + "\n"); } catch { }
    }

    [MenuItem("Catmurai/Sky Realm/Build World")]
    public static void BuildMenu() { Build(false); }

    [MenuItem("Catmurai/Sky Realm/Build World (repaint generated textures)")]
    public static void BuildRepaint() { Build(true); }

    [MenuItem("Catmurai/Sky Realm/Re-download CC0 Textures")]
    public static void Redownload() { EnsureDirs(); DownloadPolyHaven(true); }

    public static void Build(bool repaint)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try { File.WriteAllText(LogPath, ""); } catch { }
        Log("Build started");
        try
        {
            EnsureDirs();
            DownloadPolyHaven(false);
            GenerateTextures(repaint);
            CreateMaterials();
            EnsureForwardPlus();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            BeginMeshes();
            rng = new System.Random(404);
            lightCount = 0;
            EditorUtility.DisplayProgressBar("Sky Realm", "Baking Catmurai statue", 0.1f);
            BakeCatStatue();
            EditorUtility.DisplayProgressBar("Sky Realm", "Sky, moon and lighting", 0.2f);
            BuildEnvironment();
            EditorUtility.DisplayProgressBar("Sky Realm", "Building islands", 0.35f);
            BuildWorld();
            EditorUtility.DisplayProgressBar("Sky Realm", "Clouds and petals", 0.8f);
            BuildAtmosphere();
            BuildPlayerAndCamera();
            SkyRealmBleepSpawns.Add();
            CatmuraiUIBuilder.AddToOpenScene();

            AssetDatabase.SaveAssets();
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            Log("Scene saved: " + ScenePath + "  lights=" + lightCount + "  meshes=" + meshCounter);
            EditorUtility.DisplayProgressBar("Sky Realm", "Capturing preview screenshots", 0.95f);
            CaptureShots();
            Log("Build finished");
        }
        catch (System.Exception e)
        {
            Log("BUILD FAILED: " + e);
            Debug.LogException(e);
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    static void AddToBuildSettings()
    {
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        if (list.Exists(s => s.path == ScenePath)) return;
        list.Add(new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = list.ToArray();
    }

    static void EnsureForwardPlus()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData"))
        {
            var rd = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
            if (rd != null && rd.renderingMode != UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus)
            {
                rd.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus;
                EditorUtility.SetDirty(rd);
                Log("Renderer " + rd.name + " switched to Forward+ (many lantern lights)");
            }
        }
        AssetDatabase.SaveAssets();
    }

    // ------------------------------------------------------------------ environment
    static void BuildEnvironment()
    {
        RenderSettings.skybox = M["Sky"];
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.48f, 0.42f, 0.80f);
        RenderSettings.ambientEquatorColor = new Color(0.36f, 0.28f, 0.60f);
        RenderSettings.ambientGroundColor = new Color(0.26f, 0.20f, 0.42f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogColor = new Color(0.50f, 0.43f, 0.78f);
        RenderSettings.fogStartDistance = 45f;
        RenderSettings.fogEndDistance = 300f;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;

        var env = new GameObject("Environment").transform;

        var sunGo = new GameObject("Moonlight");
        sunGo.transform.SetParent(env, false);
        sunGo.transform.rotation = Quaternion.Euler(42f, -28f, 0f);
        var sun = sunGo.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(0.76f, 0.70f, 1f);
        sun.intensity = 1.15f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.7f;
        RenderSettings.sun = sun;

        // Moon: an unfogged disc far away + halo (the sky texture already has a matching glow).
        Vector3 mpos = MoonDir * 900f;
        MeshGO("MoonHalo", env, QuadMesh(520f, 520f, false), new[] { M["MoonHalo"] }, MoonDir * 910f, Quaternion.LookRotation(MoonDir), Vector3.one, false, false);
        MeshGO("Moon", env, QuadMesh(150f, 150f, false), new[] { M["Moon"] }, mpos, Quaternion.LookRotation(MoonDir), Vector3.one, false, false);

        // Endless lavender floor far below the cloud sea (fog blends it into the horizon).
        MeshGO("CloudFloor", env, FlatQuadMesh(6000f), new[] { M["Underworld"] }, new Vector3(0f, -60f, 0f), Quaternion.identity, Vector3.one, false, false);

        // Post-processing
        string pp = Root + "/SkyRealm_PostFX.asset";
        AssetDatabase.DeleteAsset(pp);
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(prof, pp);
        var bloom = prof.Add<Bloom>(true);
        bloom.threshold.value = 0.9f;
        bloom.intensity.value = 1.3f;
        bloom.scatter.value = 0.72f;
        bloom.tint.value = new Color(0.88f, 0.78f, 1f);
        var tm = prof.Add<Tonemapping>(true);
        tm.mode.value = TonemappingMode.ACES;
        var ca = prof.Add<ColorAdjustments>(true);
        ca.postExposure.value = 0.45f;
        ca.contrast.value = 12f;
        ca.saturation.value = 20f;
        ca.colorFilter.value = new Color(0.98f, 0.94f, 1f);
        var vg = prof.Add<Vignette>(true);
        vg.intensity.value = 0.26f;
        vg.smoothness.value = 0.5f;
        foreach (var c in prof.components) AssetDatabase.AddObjectToAsset(c, prof);
        EditorUtility.SetDirty(prof);
        AssetDatabase.SaveAssets();
        var vgo = new GameObject("PostFX");
        vgo.transform.SetParent(env, false);
        var vol = vgo.AddComponent<Volume>();
        vol.isGlobal = true;
        vol.priority = 1f;
        vol.sharedProfile = prof;
    }

    // ------------------------------------------------------------------ islands
    static Isle MakeIsland(string name, Transform parent, Vector3 c, float R, float depth, int seed, Material topMat, bool walk, float wobble, float bump, int spikes)
    {
        var mesh = IslandMesh(seed, R, depth, bump, wobble, out var rad);
        var go = MeshGO(name, parent, mesh, new[] { topMat, M["Cliff"] }, c, Quaternion.identity, Vector3.one, walk);
        var isle = new Isle { c = c, R = R, rad = rad, t = go.transform };
        for (int k = 0; k < spikes; k++)
        {
            float ang = Rf(0f, Mathf.PI * 2f), off = Rf(0.1f, 0.4f) * R;
            var sm = IslandMesh(seed * 31 + k, R * Rf(0.16f, 0.28f), depth * Rf(0.45f, 0.8f), 0f, 1.3f, out _);
            MeshGO("Spike", go.transform, sm, new[] { M["Cliff"], M["Cliff"] }, new Vector3(Mathf.Cos(ang) * off, -depth * Rf(0.35f, 0.55f), Mathf.Sin(ang) * off), Quaternion.identity, Vector3.one, false);
        }
        return isle;
    }

    static void CliffCrystals(Isle isle, Transform parent, float depth, int count, bool lights)
    {
        for (int k = 0; k < count; k++)
        {
            float ang = Rf(0f, Mathf.PI * 2f);
            float down = Rf(1.0f, Mathf.Min(4f, depth * 0.3f));
            float r = isle.Edge(ang) * Mathf.Pow(1f - down / depth, 1.5f) * 0.97f;
            var dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
            var p = isle.c + dir * r + Vector3.down * down;
            var rot = Quaternion.FromToRotation(Vector3.up, (dir + Vector3.up * Rf(0.2f, 0.9f)).normalized);
            CrystalCluster(parent, p, rot, Rf(0.8f, 1.4f), lights && k == 0);
        }
    }

    static void Bushes(Isle isle, Transform parent, int count)
    {
        for (int k = 0; k < count; k++)
        {
            float ang = Rf(0f, Mathf.PI * 2f);
            Bush(parent, isle.EdgePoint(ang, Rf(0.6f, 1.4f)), Rf(0.8f, 1.3f));
        }
    }

    static Vector3 Flat(Vector3 v) { v.y = 0f; return v.normalized; }

    static void BuildWorld()
    {
        var world = new GameObject("SkyRealm").transform;
        var A = Group("Landing", world, Vector3.zero, Quaternion.identity, 1f);
        var P = Group("RunePlaza", world, Vector3.zero, Quaternion.identity, 1f);
        var N = Group("ShrineTerrace", world, Vector3.zero, Quaternion.identity, 1f);
        var E = Group("EastShrine", world, Vector3.zero, Quaternion.identity, 1f);
        var W = Group("WestRuins", world, Vector3.zero, Quaternion.identity, 1f);
        var D = Group("DistantIslands", world, Vector3.zero, Quaternion.identity, 1f);
        const float PI = Mathf.PI;

        var land = MakeIsland("LandingIsland", A, new Vector3(0f, 0f, -24f), 7.5f, 13f, 11, M["Floor"], true, 0.6f, 0f, 3);
        var plaza = MakeIsland("PlazaIsland", P, new Vector3(0f, 1.5f, 0f), 9.6f, 17f, 22, M["Floor"], true, 0f, 0f, 4);
        var north = MakeIsland("TerraceIsland", N, new Vector3(0f, 5f, 25f), 10f, 18f, 33, M["Floor"], true, 0.5f, 0f, 4);
        var east = MakeIsland("EastIsland", E, new Vector3(21f, 1f, 2f), 6.5f, 12f, 44, M["Moss"], true, 0.8f, 0f, 3);
        var west = MakeIsland("WestIsland", W, new Vector3(-21f, 2.5f, 5f), 7f, 14f, 55, M["Moss"], true, 0.8f, 0f, 3);
        var mains = new List<Isle> { land, plaza, north, east, west };

        // ---------------- Landing (spawn)
        Torii(A, land.EdgePoint(PI / 2f, 1.6f), Quaternion.identity, 1f, true);
        foreach (float z in new[] { -20.5f, -24f, -27.2f })
            foreach (float x in new[] { -1.9f, 1.9f })
                StoneLantern(A, new Vector3(x, 0f, z), 1f, z != -24f);
        CatStatue(A, new Vector3(-4.4f, 0f, -22.3f), Vector3.right, 1.9f, true);
        FireBowl(A, new Vector3(-3.1f, 0f, -20.6f), 0.7f, true, true);
        CherryTree(A, new Vector3(5.3f, 0f, -20.0f), 1.05f);
        CherryTree(A, new Vector3(4.2f, 0f, -28.4f), 0.95f);
        Arch(A, new Vector3(-4.2f, 0f, -27.6f), Quaternion.LookRotation(new Vector3(1f, 0f, 0.35f)), 0.7f, false, true);
        Obelisk(A, new Vector3(3.6f, 0f, -25f), 0.7f, false);
        Bushes(land, A, 8);
        CliffCrystals(land, A, 13f, 3, true);
        PlankBridge(A, "MainBridge", land.EdgePoint(PI / 2f, 0.4f), plaza.EdgePoint(-PI / 2f, 0.45f), 1.8f, 0.35f, true);
        EdgeWalls(land, A, new[] { PI / 2f }, 1.0f);

        // ---------------- Rune plaza
        Vector3 pc = plaza.c;
        var rune = MeshGO("RuneCircle", P, FlatQuadMesh(15f), new[] { M["RuneGlow"] }, pc + Vector3.up * 0.03f, Quaternion.identity, Vector3.one, false, false);
        rune.AddComponent<SkyRealmSpin>().degreesPerSecond = 2.5f;
        MeshGO("PawSigil", P, FlatQuadMesh(6.2f), new[] { M["PawGlow"] }, pc + Vector3.up * 0.04f, Quaternion.identity, Vector3.one, false, false);
        PointLight(P, pc + Vector3.up * 2.5f, PurpleLight, 3.5f, 11f, false);
        float[] entries = { 0f, 90f, 180f, 270f };
        for (float ang = 0f; ang < 360f; ang += 7.5f)
        {
            bool gap = false;
            foreach (var e in entries) if (Mathf.Abs(Mathf.DeltaAngle(ang, e)) < 11f) gap = true;
            if (gap) continue;
            float a = ang * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            var tan = new Vector3(-dir.z, 0f, dir.x);
            Box("Curb", P, pc + dir * 9.25f + Vector3.up * 0.22f, new Vector3(0.5f, 0.45f, 1.28f), M["StoneWall"], Quaternion.LookRotation(tan), true);
        }
        for (int k = 0; k < 8; k++)
        {
            float ang = 22.5f + 45f * k;
            if (Mathf.Approximately(ang, 67.5f) || Mathf.Approximately(ang, 112.5f)) continue;
            float a = ang * Mathf.Deg2Rad;
            Obelisk(P, pc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.1f, 1f, true);
        }
        foreach (float ang in new[] { 67.5f, 112.5f })
        {
            float a = ang * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Arch(P, pc + dir * 7.3f, Quaternion.LookRotation(-dir), 0.8f, true, false);
        }
        foreach (float e in new[] { 0f, 180f, 270f })
            foreach (float off in new[] { -10f, 10f })
            {
                float a = (e + off) * Mathf.Deg2Rad;
                StoneLantern(P, pc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.5f, 1f, true);
            }
        foreach (float ang in new[] { 78f, 102f })
        {
            float a = ang * Mathf.Deg2Rad;
            CatStatue(P, pc + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 8.55f, Vector3.back, 1.5f, true);
        }
        foreach (float ang in new[] { 238f, 254f, 286f, 302f, 20f, 160f })
        {
            float a = ang * Mathf.Deg2Rad;
            var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
            Banner(P, pc + dir * 9.62f + Vector3.up * 0.3f, Quaternion.LookRotation(-dir), 0.9f, 2.4f);
        }
        foreach (float ang in new[] { 205f, 335f })
        {
            float a = ang * Mathf.Deg2Rad;
            Waterfall(P, plaza.EdgePoint(a, -0.05f) + Vector3.down * 0.2f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), 1.6f, 42f);
        }
        CliffCrystals(plaza, P, 17f, 5, true);
        EdgeWalls(plaza, P, new[] { 0f, PI / 2f, PI, -PI / 2f }, 1.0f);

        // ---------------- Floating stairs to the shrine terrace
        Vector3 sa = plaza.EdgePoint(PI / 2f, 0.3f), sb = north.EdgePoint(-PI / 2f, 0.6f);
        FloatingStairs(N, sa, sb, 3f, 18);

        // ---------------- Shrine terrace with the giant Catmurai statue
        Vector3 nc = north.c;
        CatStatue(N, nc + new Vector3(0f, 0f, 3.5f), Vector3.back, 9f, true);
        MeshGO("PedestalBeam", N, QuadMesh(0.3f, 2.1f, false), new[] { M["Portal"] }, nc + new Vector3(0f, 1.1f, 3.5f - 3.39f), Quaternion.identity, Vector3.one, false, false);
        MeshGO("PedestalPaw", N, QuadMesh(1.5f, 1.5f, false), new[] { M["PawGlow"] }, nc + new Vector3(0f, 1.1f, 3.5f - 3.40f), Quaternion.identity, Vector3.one, false, false);
        var rune2 = MeshGO("ShrineRune", N, FlatQuadMesh(7f), new[] { M["RuneGlow"] }, nc + new Vector3(0f, 0.03f, -3.2f), Quaternion.identity, Vector3.one, false, false);
        rune2.AddComponent<SkyRealmSpin>().degreesPerSecond = -4f;
        PointLight(N, nc + new Vector3(0f, 4f, -1.5f), PurpleLight, 4f, 14f, false);
        foreach (float x in new[] { -3.3f, 3.3f })
        {
            FireBowl(N, nc + new Vector3(x, 0f, -3.2f), 1f, true, true);
            StoneLantern(N, nc + new Vector3(x * 0.68f, 0f, -7.5f), 1f, true);
            Arch(N, nc + new Vector3(x * 1.8f, 0f, 1.2f), Quaternion.LookRotation(Vector3.back), 0.95f, true, false);
            Obelisk(N, nc + new Vector3(x * 2.2f, 0f, -5f), 1f, x < 0f);
            CherryTree(N, nc + new Vector3(x * 2.3f, 0f, 6.5f), 1.3f);
        }
        Bushes(north, N, 10);
        CliffCrystals(north, N, 18f, 4, true);
        foreach (float ang in new[] { 62f, 118f })
        {
            float a = ang * Mathf.Deg2Rad;
            Waterfall(N, north.EdgePoint(a, -0.05f) + Vector3.down * 0.2f, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)), 2.2f, 48f);
        }
        EdgeWalls(north, N, new[] { -PI / 2f }, 1.7f);

        // ---------------- East shrine island
        {
            Vector3 dirPE = Flat(east.c - plaza.c);
            Vector3 a = plaza.EdgePoint(plaza.AngleTo(east.c), 0.45f);
            Vector3 b = east.EdgePoint(east.AngleTo(plaza.c), 0.5f);
            PlankBridge(E, "EastBridge", a, b, 1.6f, -0.45f, true);
            Torii(E, b + dirPE * 1.7f, Quaternion.LookRotation(dirPE), 0.9f, true);
            Pagoda(E, east.c + new Vector3(1.8f, 0f, 0.6f), Quaternion.LookRotation(dirPE), 0.8f, 2);
            var side = Vector3.Cross(Vector3.up, dirPE);
            StoneLantern(E, b + dirPE * 3.2f + side * 1.4f, 1f, true);
            StoneLantern(E, b + dirPE * 3.2f - side * 1.4f, 1f, false);
            CherryTree(E, east.c + new Vector3(-1.5f, 0f, 3.6f), 1.0f);
            CrystalCluster(E, east.c + new Vector3(-1.2f, 0f, -3.5f), Quaternion.identity, 1.1f, true);
            Bushes(east, E, 6);
            CliffCrystals(east, E, 12f, 3, false);
            Waterfall(E, east.EdgePoint(0.15f, -0.05f) + Vector3.down * 0.2f, new Vector3(Mathf.Cos(0.15f), 0f, Mathf.Sin(0.15f)), 1.4f, 36f);
            EdgeWalls(east, E, new[] { east.AngleTo(plaza.c) }, 1.0f);
        }

        // ---------------- West ruins island
        {
            Vector3 dirPW = Flat(west.c - plaza.c);
            Vector3 a = plaza.EdgePoint(plaza.AngleTo(west.c), 0.45f);
            Vector3 b = west.EdgePoint(west.AngleTo(plaza.c), 0.5f);
            PlankBridge(W, "WestBridge", a, b, 1.6f, -0.35f, true);
            Arch(W, west.c + new Vector3(0.5f, 0f, -2.6f), Quaternion.LookRotation(Vector3.right), 0.85f, false, false);
            Arch(W, west.c + new Vector3(-2.2f, 0f, 2.4f), Quaternion.LookRotation(new Vector3(1f, 0f, -0.4f)), 0.85f, false, true);
            CrystalCluster(W, west.c + new Vector3(-3.2f, 0f, -1.2f), Quaternion.identity, 1.6f, true);
            CatStatue(W, west.c + new Vector3(1.8f, 0f, 3.2f), -dirPW, 1.6f, true);
            FireBowl(W, west.c + new Vector3(2.6f, 0f, 1.6f), 0.8f, true, true);
            CherryTree(W, west.c + new Vector3(-3.5f, 0f, 3.8f), 1.1f);
            Bushes(west, W, 7);
            CliffCrystals(west, W, 14f, 4, true);
            foreach (float ang in new[] { 170f, 200f })
            {
                float r = ang * Mathf.Deg2Rad;
                Waterfall(W, west.EdgePoint(r, -0.05f) + Vector3.down * 0.2f, new Vector3(Mathf.Cos(r), 0f, Mathf.Sin(r)), 1.5f, 40f);
            }
            EdgeWalls(west, W, new[] { west.AngleTo(plaza.c) }, 1.0f);
        }

        // ---------------- Near decorative islands
        {
            var se = MakeIsland("SE_Isle", D, new Vector3(15f, -3f, -19f), 4f, 9f, 66, M["Moss"], false, 1f, 0.3f, 2);
            Torii(D, se.c + new Vector3(0f, 0f, 0.5f), Quaternion.LookRotation(new Vector3(-1f, 0f, 0.3f)), 0.8f, false);
            StoneLantern(D, se.c + new Vector3(-1.5f, 0f, -1.5f), 0.9f, true);
            Bush(D, se.c + new Vector3(1.8f, 0f, -1f), 1f);
            CliffCrystals(se, D, 9f, 2, false);
            mains.Add(se);
            var sw = MakeIsland("SW_Isle", D, new Vector3(-16f, 4f, -17f), 5f, 11f, 77, M["Moss"], false, 1f, 0.3f, 2);
            Arch(D, sw.c + new Vector3(0.5f, 0f, 0f), Quaternion.LookRotation(new Vector3(1f, 0f, -0.5f)), 0.8f, false, true);
            CrystalCluster(D, sw.c + new Vector3(-1.8f, 0f, 1.2f), Quaternion.identity, 1.2f, true);
            CherryTree(D, sw.c + new Vector3(2f, 0f, 2f), 0.9f);
            Waterfall(D, sw.EdgePoint(PI * 1.25f, -0.05f) + Vector3.down * 0.2f, new Vector3(-0.7f, 0f, -0.7f), 1.1f, 30f);
            mains.Add(sw);
        }

        // ---------------- High island to the north, reached by floating slabs (decorative)
        {
            var hi = MakeIsland("HighShrineIsle", D, new Vector3(0f, 17f, 64f), 8f, 16f, 88, M["Floor"], false, 0.7f, 0f, 3);
            Pagoda(D, hi.c + new Vector3(0f, 0f, 1f), Quaternion.LookRotation(Vector3.forward), 1.2f, 3);
            Torii(D, hi.c + new Vector3(0f, 0f, -5f), Quaternion.identity, 1.1f, false);
            Obelisk(D, hi.c + new Vector3(-4.5f, 0f, -3f), 1f, false);
            Obelisk(D, hi.c + new Vector3(4.5f, 0f, -3f), 1f, false);
            Waterfall(D, hi.EdgePoint(-PI / 2f + 0.5f, -0.05f) + Vector3.down * 0.2f, new Vector3(0.45f, 0f, -0.9f), 2f, 55f);
            CliffCrystals(hi, D, 16f, 3, false);
            mains.Add(hi);
            Vector3 s0 = north.EdgePoint(PI / 2f, 1f) + Vector3.up * 0.2f, s1 = hi.EdgePoint(-PI / 2f, 0.5f);
            for (int i = 0; i < 12; i++)
            {
                float t = (i + 0.5f) / 12f;
                var p = Vector3.Lerp(s0, s1, t) + new Vector3(Mathf.Sin(t * 6f) * 1.2f, 0f, 0f);
                var slab = Box("FloatingSlab", D, p, new Vector3(1.6f, 0.3f, 1.3f), M["Blocks"], Quaternion.Euler(0f, Rf(-15f, 15f), 0f), false);
                var fl = slab.AddComponent<SkyRealmFloat>(); fl.amplitude = 0.15f; fl.speed = 0.8f; fl.phase = i * 0.7f;
            }
        }

        // ---------------- Distant islands ring
        for (int i = 0; i < 34; i++)
        {
            float ang = i / 34f * 360f + Rf(-4f, 4f);
            float dist = Rf(46f, 150f);
            float a = ang * Mathf.Deg2Rad;
            var c = new Vector3(Mathf.Cos(a) * dist, Rf(-14f, 8f) + dist * 0.1f, Mathf.Sin(a) * dist);
            float R = Rf(3f, 10f) * (dist > 100f ? 1.3f : 1f);
            bool clash = false;
            foreach (var m in mains) if (Vector3.Distance(Flat2(m.c), Flat2(c)) < m.R + R + 6f) clash = true;
            if (clash) continue;
            var isle = MakeIsland("FarIsle_" + i, D, c, R, R * Rf(1.6f, 2.4f), 1000 + i, rng.NextDouble() < 0.6 ? M["Moss"] : M["Floor"], false, 1f, R * 0.06f, 2);
            mains.Add(isle);
            Vector3 toCenter = Flat(-c);
            float s = Mathf.Clamp(R / 6f, 0.7f, 1.6f);
            switch (i % 6)
            {
                case 0:
                    Torii(D, c + Vector3.up * R * 0.03f, Quaternion.LookRotation(toCenter), s, false);
                    StoneLantern(D, c + Vector3.Cross(Vector3.up, toCenter) * 2f * s, s, false);
                    break;
                case 1: Pagoda(D, c + Vector3.up * R * 0.03f, Quaternion.LookRotation(-toCenter), s, R > 6f ? 3 : 2); break;
                case 2:
                    Arch(D, c, Quaternion.LookRotation(toCenter), s, false, false);
                    Obelisk(D, c + Vector3.Cross(Vector3.up, toCenter) * 3.5f * s, s * 0.8f, false);
                    break;
                case 3: CrystalCluster(D, c, Quaternion.identity, s * 1.6f, false); break;
                case 4:
                    CherryTree(D, c, s * 1.2f);
                    Bush(D, c + new Vector3(1.5f, 0f, 1f), s);
                    break;
                default:
                    Obelisk(D, c + Vector3.Cross(Vector3.up, toCenter) * 1.8f * s, s, false);
                    Obelisk(D, c - Vector3.Cross(Vector3.up, toCenter) * 1.8f * s, s, false);
                    break;
            }
            CliffCrystals(isle, D, R * 2f, Ri(0, 3), false);
            if (i % 4 == 1)
            {
                float wa = Mathf.Atan2(toCenter.z, toCenter.x) + Rf(-0.6f, 0.6f);
                Waterfall(D, isle.EdgePoint(wa, -0.05f) + Vector3.down * 0.2f, new Vector3(Mathf.Cos(wa), 0f, Mathf.Sin(wa)), Mathf.Clamp(R * 0.25f, 1f, 2.5f), 45f);
            }
        }

        // ---------------- Floating debris rocks
        for (int i = 0; i < 70; i++)
        {
            float a = Rf(0f, PI * 2f), dist = Rf(12f, 95f);
            var c = new Vector3(Mathf.Cos(a) * dist, Rf(-8f, 22f), Mathf.Sin(a) * dist);
            float R = Rf(0.5f, 2.4f);
            bool clash = false;
            foreach (var m in mains) if (Vector3.Distance(Flat2(m.c), Flat2(c)) < m.R + R + 3f && Mathf.Abs(m.c.y - c.y) < 25f) clash = true;
            if (clash) continue;
            var mesh = IslandMesh(5000 + i, R, R * Rf(1.2f, 2.2f), R * 0.2f, 1.4f, out _);
            var rock = MeshGO("Debris", D, mesh, new[] { rng.NextDouble() < 0.5 ? M["Moss"] : M["Cliff"], M["Cliff"] }, c, Quaternion.Euler(Rf(-12f, 12f), Rf(0f, 360f), Rf(-12f, 12f)), Vector3.one, false);
            var fl = rock.AddComponent<SkyRealmFloat>();
            fl.amplitude = Rf(0.2f, 0.7f); fl.speed = Rf(0.15f, 0.4f); fl.phase = Rf(0f, 6f); fl.spin = Rf(-3f, 3f);
            if (rng.NextDouble() < 0.25) CrystalCluster(rock.transform, new Vector3(0f, R * 0.15f, 0f), Quaternion.identity, 0.5f, false);
        }
    }

    static Vector3 Flat2(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

    // ------------------------------------------------------------------ clouds, petals, motes
    static void BuildAtmosphere()
    {
        var atm = new GameObject("Atmosphere").transform;

        var sea = PS("CloudSea", atm, new Vector3(0f, -32f, 0f), M["Cloud"]);
        {
            var m = sea.main;
            m.duration = 60f; m.prewarm = true;
            m.startLifetime = 90f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.4f);
            m.startSize = new ParticleSystem.MinMaxCurve(30f, 70f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(0.8f, 0.74f, 1f, 0.6f), new Color(0.62f, 0.54f, 0.9f, 0.5f));
            m.maxParticles = 1200;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = sea.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(700f, 10f, 700f);
            var em = sea.emission; em.rateOverTime = 10f;
            var col = sea.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.1f, 0.85f));
            var r = sea.GetComponent<ParticleSystemRenderer>(); r.sortMode = ParticleSystemSortMode.Distance; r.maxParticleSize = 5f;
        }

        var wisps = PS("CloudWisps", atm, new Vector3(0f, -16f, 10f), M["Cloud"]);
        {
            var m = wisps.main;
            m.duration = 60f; m.prewarm = true;
            m.startLifetime = 70f;
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.6f);
            m.startSize = new ParticleSystem.MinMaxCurve(8f, 16f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new Color(0.82f, 0.76f, 1f, 0.16f);
            m.maxParticles = 200;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = wisps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(180f, 6f, 180f);
            var em = wisps.emission; em.rateOverTime = 1.0f;
            var col = wisps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.15f, 0.8f));
            var r = wisps.GetComponent<ParticleSystemRenderer>(); r.sortMode = ParticleSystemSortMode.Distance; r.maxParticleSize = 5f;
        }

        var petals = PS("BlossomPetals", atm, new Vector3(0f, 16f, 2f), M["Petal"]);
        {
            var m = petals.main;
            m.duration = 20f; m.prewarm = true;
            m.startLifetime = 22f;
            m.startSpeed = 0f;
            m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.13f);
            m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.72f, 0.95f), new Color(0.82f, 0.55f, 1f));
            m.gravityModifier = 0.012f;
            m.maxParticles = 700;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = petals.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(70f, 1f, 80f);
            var em = petals.emission; em.rateOverTime = 26f;
            var n = petals.noise; n.enabled = true; n.strength = 0.6f; n.frequency = 0.25f; n.scrollSpeed = 0.2f;
            var rot = petals.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-1.5f, 1.5f);
            var col = petals.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.05f, 0.9f));
        }

        var motes = PS("MagicMotes", atm, new Vector3(0f, 4f, 6f), M["Mote"]);
        {
            var m = motes.main;
            m.duration = 10f; m.prewarm = true;
            m.startLifetime = new ParticleSystem.MinMaxCurve(5f, 9f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.05f, 0.25f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
            m.maxParticles = 300;
            m.simulationSpace = ParticleSystemSimulationSpace.World;
            var sh = motes.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 26f;
            var em = motes.emission; em.rateOverTime = 26f;
            var n = motes.noise; n.enabled = true; n.strength = 0.4f; n.frequency = 0.3f;
            var col = motes.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.2f, 0.7f));
        }
    }

    // ------------------------------------------------------------------ player & camera
    static void BuildPlayerAndCamera()
    {
        GameObject player = null;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Catmurai/Player.prefab");
        if (prefab != null)
        {
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            player.transform.SetPositionAndRotation(Spawn, Quaternion.identity);
            var rs = player.AddComponent<SkyRealmRespawn>();
            rs.spawnPoint = Spawn;
            rs.killY = -22f;
        }
        else Log("Player.prefab not found: scene starts in cinematic camera mode");

        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3000f;
        cam.fieldOfView = 55f;
        cam.allowHDR = true;
        camGo.AddComponent<AudioListener>();
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        camGo.transform.SetPositionAndRotation(Spawn + new Vector3(0f, 2.5f, -4.5f), Quaternion.Euler(18f, 0f, 0f));
        var fc = camGo.AddComponent<CatmuraiFollowCamera>();
        fc.target = player ? player.transform : null;
        fc.distance = 4.6f;
        fc.height = 1.1f;
        fc.pitch = 18f;
        var rig = camGo.AddComponent<SkyRealmCameraRig>();
        rig.follow = fc;
        rig.cinematic = player == null;
    }

    // ------------------------------------------------------------------ preview screenshots (Recordings/SkyRealm_*.png)
    [MenuItem("Catmurai/Sky Realm/Capture Preview Screenshots")]
    public static void CaptureShots()
    {
        Directory.CreateDirectory("Recordings");
        DynamicGI.UpdateEnvironment();
        foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            ps.Simulate(ps.main.prewarm ? 40f : 3f, false, true);
        Shot("vista", new Vector3(0f, 16f, -46f), new Vector3(0f, 3f, 8f), 52f);
        Shot("player", new Vector3(0f, 2.7f, -33f), new Vector3(0f, 2f, -18f), 55f);
        Shot("plaza", new Vector3(-12f, 7f, -13f), new Vector3(0f, 2f, 3f), 55f);
        Shot("shrine", new Vector3(0f, 4f, 5f), new Vector3(0f, 10f, 28f), 62f);
        Shot("east", new Vector3(6f, 5f, -9f), new Vector3(21f, 2f, 2f), 55f);
        Shot("west", new Vector3(-6f, 6f, -10f), new Vector3(-21f, 3f, 5f), 55f);
        Log("Screenshots written to Recordings/SkyRealm_*.png");
    }

    static void Shot(string name, Vector3 pos, Vector3 look, float fov)
    {
        var go = new GameObject("ShotCam") { hideFlags = HideFlags.HideAndDontSave };
        var cam = go.AddComponent<Camera>();
        go.transform.position = pos;
        go.transform.LookAt(look);
        cam.fieldOfView = fov;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 3000f;
        cam.allowHDR = true;
        var data = cam.GetUniversalAdditionalCameraData();
        data.renderPostProcessing = true;
        data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        cam.targetTexture = rt;
        cam.Render();
        var prev = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        RenderTexture.active = prev;
        File.WriteAllBytes("Recordings/SkyRealm_" + name + ".png", tex.EncodeToPNG());
        cam.targetTexture = null;
        Object.DestroyImmediate(rt);
        Object.DestroyImmediate(tex);
        Object.DestroyImmediate(go);
    }
}
#endif

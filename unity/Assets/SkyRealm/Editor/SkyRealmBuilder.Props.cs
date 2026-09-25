#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Part 3 of the Sky Realm builder: props (torii, lanterns, arches, obelisks, trees, crystals, bridges, VFX).
public static partial class SkyRealmBuilder
{
    static System.Random rng;
    static int lightCount;

    static float Rf(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
    static int Ri(int a, int b) { return rng.Next(a, b); }

    static Transform Group(string name, Transform parent, Vector3 pos, Quaternion rot, float s)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = Vector3.one * s;
        return go.transform;
    }

    static GameObject MeshGO(string name, Transform parent, Mesh mesh, Material[] mats, Vector3 pos, Quaternion rot, Vector3 scale, bool meshCollider, bool shadows = true)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        var mr = go.AddComponent<MeshRenderer>();
        mr.sharedMaterials = mats;
        if (!shadows) { mr.shadowCastingMode = ShadowCastingMode.Off; }
        if (meshCollider) go.AddComponent<MeshCollider>().sharedMesh = mesh;
        go.isStatic = false;
        return go;
    }

    static GameObject Box(string name, Transform p, Vector3 pos, Vector3 size, Material m, Quaternion rot, bool col)
    {
        var go = MeshGO(name, p, BoxMesh(size), new[] { m }, pos, rot, Vector3.one, false);
        if (col) go.AddComponent<BoxCollider>().size = size;
        return go;
    }

    static GameObject Box(string name, Transform p, Vector3 pos, Vector3 size, Material m, bool col = false)
    {
        return Box(name, p, pos, size, m, Quaternion.identity, col);
    }

    static GameObject Cyl(string name, Transform p, Vector3 pos, float r, float h, Material m, int seg = 12, bool col = false)
    {
        var go = MeshGO(name, p, CylMesh(r, h, seg), new[] { m }, pos, Quaternion.identity, Vector3.one, false);
        if (col)
        {
            var cc = go.AddComponent<CapsuleCollider>();
            cc.radius = r; cc.height = h; cc.center = new Vector3(0f, h / 2f, 0f);
        }
        return go;
    }

    static void InvisibleCollider(string name, Transform p, Vector3 pos, Quaternion rot, Vector3 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.AddComponent<BoxCollider>().size = size;
    }

    static Light PointLight(Transform p, Vector3 pos, Color c, float intensity, float range, bool flicker)
    {
        var go = new GameObject("Light");
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = c;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        if (flicker)
        {
            var f = go.AddComponent<SkyRealmFlicker>();
            f.baseIntensity = intensity;
            f.seed = Rf(0f, 100f);
        }
        lightCount++;
        return l;
    }

    static readonly Color WarmLight = new Color(1f, 0.62f, 0.32f);
    static readonly Color PurpleLight = new Color(0.72f, 0.38f, 1f);

    // ------------------------------------------------------------------ particles
    static ParticleSystem PS(string name, Transform p, Vector3 pos, Material mat)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p, false);
        go.transform.localPosition = pos;
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var r = go.GetComponent<ParticleSystemRenderer>();
        r.sharedMaterial = mat;
        r.shadowCastingMode = ShadowCastingMode.Off;
        r.receiveShadows = false;
        var main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startColor = Color.white;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        return ps;
    }

    static Gradient FadeGradient(float inT, float outT)
    {
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, inT), new GradientAlphaKey(1f, outT), new GradientAlphaKey(0f, 1f) });
        return g;
    }

    static void Flame(Transform p, Vector3 pos, float size, bool purple)
    {
        var ps = PS("Flame", p, pos, purple ? M["FlameP"] : M["FlameW"]);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.45f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.4f * size, 0.9f * size);
        main.startSize = new ParticleSystem.MinMaxCurve(0.28f * size, 0.55f * size);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 6f; sh.radius = 0.1f * size;
        var em = ps.emission; em.rateOverTime = 34f;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.15f, 0.5f));
        var sz = ps.sizeOverLifetime; sz.enabled = true; sz.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.15f));
    }

    // ------------------------------------------------------------------ props
    static void Torii(Transform parent, Vector3 pos, Quaternion rot, float s, bool banners)
    {
        var g = Group("Torii", parent, pos, rot, s);
        foreach (float x in new[] { -1.35f, 1.35f })
        {
            Cyl("Pillar", g, new Vector3(x, 0f, 0f), 0.16f, 3.35f, M["Lacquer"], 12, true);
            Cyl("Foot", g, new Vector3(x, 0f, 0f), 0.21f, 0.32f, M["DarkWood"], 12);
        }
        Box("Kasagi", g, new Vector3(0f, 3.47f, 0f), new Vector3(3.7f, 0.22f, 0.4f), M["DarkWood"]);
        Box("KasagiEndL", g, new Vector3(-2.0f, 3.53f, 0f), new Vector3(0.55f, 0.22f, 0.4f), M["DarkWood"], Quaternion.Euler(0, 0, -12f), false);
        Box("KasagiEndR", g, new Vector3(2.0f, 3.53f, 0f), new Vector3(0.55f, 0.22f, 0.4f), M["DarkWood"], Quaternion.Euler(0, 0, 12f), false);
        Box("Shimaki", g, new Vector3(0f, 3.28f, 0f), new Vector3(3.6f, 0.16f, 0.32f), M["Lacquer"]);
        Box("Nuki", g, new Vector3(0f, 2.72f, 0f), new Vector3(3.4f, 0.16f, 0.14f), M["Lacquer"]);
        Box("Gakuzuka", g, new Vector3(0f, 3.0f, 0f), new Vector3(0.12f, 0.42f, 0.12f), M["Lacquer"]);
        Box("Plaque", g, new Vector3(0f, 3.0f, -0.09f), new Vector3(0.36f, 0.46f, 0.05f), M["DarkWood"]);
        if (banners)
            foreach (float x in new[] { -1.35f, 1.35f })
                Banner(g, new Vector3(x, 2.6f, -0.2f), Quaternion.identity, 0.6f, 1.55f);
    }

    static void Banner(Transform parent, Vector3 topPos, Quaternion rot, float w, float h)
    {
        var g = Group("Banner", parent, topPos, rot, 1f);
        Box("Rod", g, new Vector3(0f, 0.03f, 0f), new Vector3(w + 0.14f, 0.05f, 0.05f), M["DarkWood"]);
        var cloth = MeshGO("Cloth", g, QuadMesh(w, h, true), new[] { M["Banner"] }, Vector3.zero, Quaternion.identity, Vector3.one, false);
        var sway = cloth.AddComponent<SkyRealmSway>();
        sway.phase = Rf(0f, 6f);
        sway.degrees = Rf(2.5f, 4.5f);
    }

    static void StoneLantern(Transform parent, Vector3 pos, float s, bool light)
    {
        var g = Group("StoneLantern", parent, pos, Quaternion.Euler(0f, Rf(-6f, 6f), 0f), s);
        Box("Base", g, new Vector3(0f, 0.1f, 0f), new Vector3(0.62f, 0.2f, 0.62f), M["StoneWall"]);
        Cyl("Shaft", g, new Vector3(0f, 0.2f, 0f), 0.13f, 0.6f, M["Blocks"], 8);
        Box("Platform", g, new Vector3(0f, 0.88f, 0f), new Vector3(0.62f, 0.16f, 0.62f), M["StoneWall"]);
        Box("FireCore", g, new Vector3(0f, 1.13f, 0f), new Vector3(0.3f, 0.3f, 0.3f), M["LanternGlow"]);
        foreach (float cx in new[] { -0.17f, 0.17f })
            foreach (float cz in new[] { -0.17f, 0.17f })
                Box("FirePost", g, new Vector3(cx, 1.13f, cz), new Vector3(0.08f, 0.34f, 0.08f), M["StoneWall"]);
        MeshGO("Roof", g, ConeMesh(0.56f, 0.34f, 4, 45f), new[] { M["StoneWall"] }, new Vector3(0f, 1.3f, 0f), Quaternion.identity, Vector3.one, false);
        MeshGO("Finial", g, BlobMesh(3), new[] { M["StoneWall"] }, new Vector3(0f, 1.68f, 0f), Quaternion.identity, Vector3.one * 0.08f, false);
        var bc = g.gameObject.AddComponent<BoxCollider>();
        bc.center = new Vector3(0f, 0.85f, 0f); bc.size = new Vector3(0.6f, 1.7f, 0.6f);
        if (light) PointLight(g, new Vector3(0f, 1.15f, 0f), WarmLight, 3.2f, 7f, true);
    }

    static void HangingLantern(Transform parent, Vector3 pos)
    {
        Box("Lantern", parent, pos, new Vector3(0.16f, 0.24f, 0.16f), M["LanternGlow"]);
        Box("LanternCap", parent, pos + Vector3.up * 0.14f, new Vector3(0.22f, 0.04f, 0.22f), M["DarkWood"]);
    }

    static void Obelisk(Transform parent, Vector3 pos, float s, bool light)
    {
        var g = Group("Obelisk", parent, pos, Quaternion.Euler(0f, Rf(0f, 90f), 0f), s);
        Box("Base", g, new Vector3(0f, 0.2f, 0f), new Vector3(1.1f, 0.4f, 1.1f), M["Blocks"]);
        Box("Base2", g, new Vector3(0f, 0.55f, 0f), new Vector3(0.85f, 0.3f, 0.85f), M["Blocks"]);
        Box("Shaft", g, new Vector3(0f, 2.2f, 0f), new Vector3(0.56f, 3.0f, 0.56f), M["Blocks"], true);
        Box("Band1", g, new Vector3(0f, 1.25f, 0f), new Vector3(0.6f, 0.08f, 0.6f), M["RuneStone"]);
        Box("Band2", g, new Vector3(0f, 3.25f, 0f), new Vector3(0.6f, 0.08f, 0.6f), M["RuneStone"]);
        MeshGO("Cap", g, ConeMesh(0.48f, 0.55f, 4, 45f), new[] { M["Roof"] }, new Vector3(0f, 3.7f, 0f), Quaternion.identity, Vector3.one, false);
        var cr = MeshGO("FloatCrystal", g, CrystalMesh(), new[] { M["Crystal"] }, new Vector3(0f, 4.55f, 0f), Quaternion.identity, new Vector3(1.1f, 0.6f, 1.1f), false, false);
        var sp = cr.AddComponent<SkyRealmFloat>(); sp.amplitude = 0.12f; sp.speed = 1.3f; sp.phase = Rf(0f, 6f); sp.spin = 30f;
        Flame(g, new Vector3(0f, 4.3f, 0f), 1f, true);
        if (light) PointLight(g, new Vector3(0f, 4.4f, 0f), PurpleLight, 3.2f, 8f, true);
    }

    static void FireBowl(Transform parent, Vector3 pos, float s, bool purple, bool light)
    {
        var g = Group("FireBowl", parent, pos, Quaternion.identity, s);
        for (int i = 0; i < 3; i++)
        {
            float a = i * 120f * Mathf.Deg2Rad;
            Box("Leg", g, new Vector3(Mathf.Cos(a) * 0.3f, 0.3f, Mathf.Sin(a) * 0.3f), new Vector3(0.07f, 0.62f, 0.07f), M["Metal"], Quaternion.Euler(Mathf.Sin(a) * 10f, 0f, -Mathf.Cos(a) * 10f), false);
        }
        Cyl("Bowl", g, new Vector3(0f, 0.58f, 0f), 0.46f, 0.26f, M["Metal"], 14, true);
        Cyl("Embers", g, new Vector3(0f, 0.8f, 0f), 0.38f, 0.05f, purple ? M["Crystal"] : M["LanternGlow"], 14);
        Flame(g, new Vector3(0f, 0.85f, 0f), 1.5f, purple);
        if (light) PointLight(g, new Vector3(0f, 1.3f, 0f), purple ? PurpleLight : WarmLight, 3f, 8f, true);
    }

    static void Arch(Transform parent, Vector3 pos, Quaternion rot, float s, bool portal, bool broken)
    {
        var g = Group(broken ? "ArchRuin" : "Arch", parent, pos, rot, s);
        float w = 1.3f, ph = 3.2f, R = w + 0.4f;
        foreach (float sx in new[] { -1f, 1f })
        {
            float h = (broken && sx > 0f) ? ph * 0.55f : ph;
            Box("Plinth", g, new Vector3(sx * R, 0.25f, 0f), new Vector3(1.15f, 0.5f, 1.15f), M["Blocks"], true);
            Box("Pillar", g, new Vector3(sx * R, 0.5f + h / 2f, 0f), new Vector3(0.8f, h, 0.8f), M["Blocks"], true);
            if (!(broken && sx > 0f))
                Box("Capital", g, new Vector3(sx * R, 0.5f + h + 0.12f, 0f), new Vector3(1.0f, 0.24f, 1.0f), M["Blocks"]);
        }
        Vector3 c = new Vector3(0f, 0.5f + ph + 0.24f, 0f);
        int n = 9;
        for (int i = 0; i < n; i++)
        {
            if (broken && i < 4) continue;
            float th = (i + 0.5f) / n * Mathf.PI;
            var p = c + new Vector3(Mathf.Cos(th) * R, Mathf.Sin(th) * R, 0f);
            Box("Voussoir", g, p, new Vector3(Mathf.PI * R / n * 1.04f, 0.8f, 0.8f), M["Blocks"], Quaternion.Euler(0f, 0f, th * Mathf.Rad2Deg + 90f), false);
        }
        if (broken)
            for (int i = 0; i < 4; i++)
                Box("Rubble", g, new Vector3(Rf(0.6f, 2.8f), 0.2f, Rf(-1.2f, 1.2f)), new Vector3(Rf(0.3f, 0.7f), Rf(0.25f, 0.45f), Rf(0.3f, 0.7f)), M["Blocks"], Quaternion.Euler(Rf(-15, 15), Rf(0, 90), Rf(-15, 15)), true);
        if (portal)
        {
            MeshGO("Portal", g, QuadMesh(2f * w + 0.1f, ph + R * 0.95f, false), new[] { M["Portal"] }, new Vector3(0f, 0.5f + (ph + R * 0.95f) / 2f, 0f), Quaternion.identity, Vector3.one, false, false);
            PointLight(g, new Vector3(0f, 2.2f, 0f), PurpleLight, 2.2f, 6f, false);
        }
    }

    static void Pagoda(Transform parent, Vector3 pos, Quaternion rot, float s, int tiers)
    {
        var g = Group("Pagoda", parent, pos, rot, s);
        Box("Base", g, new Vector3(0f, 0.3f, 0f), new Vector3(3.4f, 0.6f, 3.4f), M["StoneWall"], true);
        float y = 0.6f, w = 2.4f;
        for (int t = 0; t < tiers; t++)
        {
            float h = 1.5f - t * 0.15f;
            Box("Body", g, new Vector3(0f, y + h / 2f, 0f), new Vector3(w, h, w), M["DarkWood"], true);
            Box("Window", g, new Vector3(0f, y + h * 0.55f, -w / 2f - 0.01f), new Vector3(w * 0.4f, h * 0.35f, 0.04f), M["LanternGlow"]);
            MeshGO("Roof", g, ConeMesh(w * 0.95f, 0.75f, 4, 45f), new[] { M["Roof"] }, new Vector3(0f, y + h, 0f), Quaternion.identity, Vector3.one, false);
            y += h + 0.45f;
            w *= 0.8f;
        }
        Cyl("Spire", g, new Vector3(0f, y - 0.2f, 0f), 0.06f, 1.4f, M["Metal"], 6);
    }

    static void CherryTree(Transform parent, Vector3 pos, float s)
    {
        var g = Group("BlossomTree", parent, pos, Quaternion.Euler(0f, Rf(0f, 360f), 0f), s);
        var unit = CylMesh(1f, 1f, 8);
        Vector3 p = Vector3.zero;
        Vector3 dir = Vector3.up;
        float r = 0.2f;
        var tips = new List<Vector3>();
        for (int i = 0; i < 4; i++)
        {
            dir = (dir + new Vector3(Rf(-0.35f, 0.35f), 0f, Rf(-0.35f, 0.35f))).normalized;
            float len = Rf(0.6f, 0.9f);
            MeshGO("Trunk", g, unit, new[] { M["DarkWood"] }, p, Quaternion.FromToRotation(Vector3.up, dir), new Vector3(r, len, r), false);
            p += dir * len;
            r *= 0.8f;
        }
        var colGo = g.gameObject.AddComponent<CapsuleCollider>();
        colGo.radius = 0.25f; colGo.height = 2.5f; colGo.center = new Vector3(0f, 1.25f, 0f);
        tips.Add(p);
        for (int b = 0; b < 4; b++)
        {
            float a = b * 90f * Mathf.Deg2Rad + Rf(-0.4f, 0.4f);
            var bd = new Vector3(Mathf.Cos(a), Rf(0.3f, 0.8f), Mathf.Sin(a)).normalized;
            float len = Rf(0.9f, 1.4f);
            Vector3 start = p - Vector3.up * Rf(0.1f, 0.5f);
            MeshGO("Branch", g, unit, new[] { M["DarkWood"] }, start, Quaternion.FromToRotation(Vector3.up, bd), new Vector3(0.07f, len, 0.07f), false);
            tips.Add(start + bd * len);
        }
        foreach (var t in tips)
            for (int k = 0; k < 5; k++)
            {
                float fr = Rf(0.4f, 0.75f);
                MeshGO("Blossom", g, BlobMesh(Ri(0, 6)), new[] { M["Blossom"] }, t + new Vector3(Rf(-0.7f, 0.7f), Rf(-0.2f, 0.5f), Rf(-0.7f, 0.7f)), Quaternion.Euler(Rf(0, 360), Rf(0, 360), 0f), new Vector3(fr, fr * 0.75f, fr), false);
            }
    }

    static void Bush(Transform parent, Vector3 pos, float s)
    {
        var g = Group("FlowerBush", parent, pos, Quaternion.Euler(0f, Rf(0f, 360f), 0f), s);
        for (int i = 0; i < 6; i++)
        {
            float r = Rf(0.12f, 0.24f);
            MeshGO("Clump", g, BlobMesh(Ri(0, 6)), new[] { M["Bush"] }, new Vector3(Rf(-0.45f, 0.45f), r * 0.55f, Rf(-0.45f, 0.45f)), Quaternion.identity, new Vector3(r, r * 0.8f, r), false, false);
        }
    }

    static void CrystalCluster(Transform parent, Vector3 pos, Quaternion rot, float s, bool light)
    {
        var g = Group("Crystals", parent, pos, rot, s);
        int n = Ri(4, 7);
        for (int i = 0; i < n; i++)
        {
            float h = i == 0 ? 2.2f : Rf(0.6f, 1.6f);
            var q = Quaternion.Euler(Rf(-28f, 28f), Rf(0f, 360f), Rf(-28f, 28f));
            MeshGO("Crystal", g, CrystalMesh(), new[] { M["Crystal"] }, new Vector3(Rf(-0.35f, 0.35f), 0f, Rf(-0.35f, 0.35f)), q, new Vector3(h * 0.7f, h, h * 0.7f), false, false);
        }
        if (light) PointLight(g, new Vector3(0f, 1.2f, 0f), PurpleLight, 2.5f, 6f, false);
    }

    static void CatStatue(Transform parent, Vector3 pos, Vector3 faceDir, float height, bool pedestal)
    {
        var g = Group("CatStatue", parent, pos, Quaternion.identity, 1f);
        float top = 0f;
        if (pedestal)
        {
            float pw = Mathf.Max(0.9f, height * 0.75f);
            Box("Pedestal", g, new Vector3(0f, height * 0.12f, 0f), new Vector3(pw, height * 0.24f, pw), M["Blocks"], true);
            Box("Pedestal2", g, new Vector3(0f, height * 0.24f + height * 0.04f, 0f), new Vector3(pw * 0.85f, height * 0.08f, pw * 0.85f), M["StoneWall"]);
            Box("PawPlate", g, new Vector3(0f, height * 0.12f, pw / 2f + 0.01f), new Vector3(pw * 0.45f, height * 0.12f, 0.03f), M["RuneStone"], Quaternion.identity, false);
            top = height * 0.32f;
            g.localRotation = Quaternion.LookRotation(faceDir);
        }
        if (catStatueMesh == null) { Obelisk(g, new Vector3(0f, top, 0f), height / 4.5f, false); return; }
        float k = height / Mathf.Max(0.01f, catStatueMesh.bounds.size.y);
        var mats = new Material[catStatueMesh.subMeshCount];
        for (int i = 0; i < mats.Length; i++) mats[i] = M["Statue"];
        // The model faces -Z, so point -Z at faceDir.
        var st = MeshGO("Catmurai", parent, catStatueMesh, mats, pos + Vector3.up * (top - catStatueBottom * k), Quaternion.LookRotation(-faceDir), Vector3.one * k, false);
        var bc = st.AddComponent<BoxCollider>();
        bc.center = catStatueMesh.bounds.center; bc.size = catStatueMesh.bounds.size * 0.6f;
    }

    static void Waterfall(Transform parent, Vector3 top, Vector3 outward, float width, float height)
    {
        outward.y = 0f; outward.Normalize();
        MeshGO("Waterfall", parent, WaterfallMesh(width, height), new[] { M["Water"] }, top, Quaternion.LookRotation(outward), Vector3.one, false, false);
        var ps = PS("Spray", parent, top + outward * 0.5f + Vector3.down * 0.3f, M["Cloud"]);
        var main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 2.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
        main.startColor = new Color(0.9f, 0.9f, 1f, 0.35f);
        main.gravityModifier = 0.05f;
        main.maxParticles = 60;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Box; sh.scale = new Vector3(width, 0.2f, 0.2f);
        ps.transform.rotation = Quaternion.LookRotation(outward);
        var em = ps.emission; em.rateOverTime = 12f;
        var col = ps.colorOverLifetime; col.enabled = true; col.color = new ParticleSystem.MinMaxGradient(FadeGradient(0.2f, 0.6f));
    }

    /// Plank bridge from a to b. curve > 0 arches up, < 0 sags. Adds walkable colliders and side walls.
    static void PlankBridge(Transform parent, string name, Vector3 a, Vector3 b, float width, float curve, bool lanterns)
    {
        var g = Group(name, parent, Vector3.zero, Quaternion.identity, 1f);
        float L = Vector3.Distance(a, b);
        int n = Mathf.Max(4, Mathf.CeilToInt(L / 0.34f));
        Vector3 P(float t) { return Vector3.Lerp(a, b, t) + Vector3.up * (curve * 4f * t * (1f - t)); }
        Vector3 side = Vector3.Cross(Vector3.up, (b - a).normalized).normalized;
        for (int i = 0; i < n; i++)
        {
            Vector3 p0 = P(i / (float)n), p1 = P((i + 1) / (float)n);
            Vector3 mid = (p0 + p1) * 0.5f;
            float seg = Vector3.Distance(p0, p1);
            var rot = Quaternion.LookRotation(p1 - p0, Vector3.up);
            Box("Plank", g, mid + Vector3.up * Rf(-0.01f, 0.01f), new Vector3(width, 0.06f, seg * 0.86f), M["Wood"], rot * Quaternion.Euler(0f, Rf(-2f, 2f), 0f), false);
            foreach (float sx in new[] { -1f, 1f })
                Box("Stringer", g, mid + rot * new Vector3(sx * (width / 2f - 0.06f), -0.07f, 0f), new Vector3(0.09f, 0.1f, seg * 1.02f), M["DarkWood"], rot, false);
            InvisibleCollider("Walk", g, mid + rot * new Vector3(0f, -0.02f, 0f), rot, new Vector3(width, 0.1f, seg * 1.1f));
            InvisibleCollider("RailL", g, mid + rot * new Vector3(-(width / 2f + 0.06f), 0.6f, 0f), rot, new Vector3(0.1f, 1.3f, seg * 1.1f));
            InvisibleCollider("RailR", g, mid + rot * new Vector3((width / 2f + 0.06f), 0.6f, 0f), rot, new Vector3(0.1f, 1.3f, seg * 1.1f));
        }
        // posts + ropes
        int posts = Mathf.Max(2, Mathf.RoundToInt(L / 1.8f));
        var ropeL = new List<Vector3>(); var ropeR = new List<Vector3>();
        for (int i = 0; i <= posts; i++)
        {
            float t = i / (float)posts;
            Vector3 c = P(t);
            foreach (float sx in new[] { -1f, 1f })
            {
                Vector3 pp = c + side * sx * (width / 2f + 0.04f) - Vector3.up * 0.1f;
                bool end = i == 0 || i == posts;
                Cyl("Post", g, pp, end ? 0.08f : 0.05f, end ? 1.35f : 1.05f, M["DarkWood"], 8);
                if (end && lanterns) HangingLantern(g, pp + Vector3.up * 1.2f + side * sx * 0.12f);
            }
        }
        for (int i = 0; i <= 24; i++)
        {
            float t = i / 24f;
            Vector3 c = P(t) + Vector3.up * (0.9f - 0.12f * Mathf.Sin(t * posts * Mathf.PI) * Mathf.Sin(t * posts * Mathf.PI));
            ropeL.Add(c - side * (width / 2f + 0.04f));
            ropeR.Add(c + side * (width / 2f + 0.04f));
        }
        Rope(g, ropeL); Rope(g, ropeR);
        if (lanterns)
        {
            PointLight(g, P(0f) + Vector3.up * 1.1f, WarmLight, 1.6f, 5f, true);
            PointLight(g, P(1f) + Vector3.up * 1.1f, WarmLight, 1.6f, 5f, true);
        }
    }

    static void Rope(Transform parent, List<Vector3> pts)
    {
        var go = new GameObject("Rope");
        go.transform.SetParent(parent, false);
        var lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = false;
        lr.positionCount = pts.Count;
        lr.SetPositions(pts.ToArray());
        lr.widthMultiplier = 0.04f;
        lr.sharedMaterial = M["Rope"];
        lr.shadowCastingMode = ShadowCastingMode.Off;
        lr.numCapVertices = 2;
    }

    // Floating stone steps between two points with one hidden ramp collider.
    static void FloatingStairs(Transform parent, Vector3 a, Vector3 b, float width, int steps)
    {
        var g = Group("FloatingStairs", parent, Vector3.zero, Quaternion.identity, 1f);
        Vector3 flat = b - a; flat.y = 0f;
        var yaw = Quaternion.LookRotation(flat.normalized);
        float depth = flat.magnitude / steps;
        for (int i = 0; i < steps; i++)
        {
            float t = (i + 0.5f) / steps;
            Vector3 p = Vector3.Lerp(a, b, t);
            var st = Box("Step", g, p - Vector3.up * 0.14f, new Vector3(width, 0.28f, depth * 0.92f), M["Blocks"], yaw * Quaternion.Euler(0f, Rf(-2f, 2f), 0f), false);
            if (i % 3 == 1)
                MeshGO("StepRock", st.transform, IslandMesh(900 + i, 0.5f, 1.4f, 0f, 1f, out _), new[] { M["Cliff"], M["Cliff"] }, new Vector3(Rf(-0.4f, 0.4f), -0.14f, 0f), Quaternion.identity, Vector3.one, false);
        }
        Vector3 d = b - a;
        var rot = Quaternion.LookRotation(d.normalized, Vector3.up);
        Vector3 up = rot * Vector3.up;
        InvisibleCollider("Ramp", g, (a + b) * 0.5f - up * 0.1f, rot, new Vector3(width, 0.2f, d.magnitude + 0.6f));
        InvisibleCollider("RampWallL", g, (a + b) * 0.5f + rot * new Vector3(-(width / 2f + 0.1f), 0.6f, 0f), rot, new Vector3(0.2f, 1.4f, d.magnitude + 0.6f));
        InvisibleCollider("RampWallR", g, (a + b) * 0.5f + rot * new Vector3((width / 2f + 0.1f), 0.6f, 0f), rot, new Vector3(0.2f, 1.4f, d.magnitude + 0.6f));
    }

    // Invisible walls along a walkable island edge, leaving gaps toward the given angles (bridges).
    static void EdgeWalls(Isle isle, Transform parent, float[] gapAngles, float gapHalfWidth)
    {
        var g = Group("EdgeWalls", parent, Vector3.zero, Quaternion.identity, 1f);
        int n = 40;
        for (int i = 0; i < n; i++)
        {
            float a0 = i / (float)n * Mathf.PI * 2f, a1 = (i + 1) / (float)n * Mathf.PI * 2f;
            Vector3 p0 = isle.EdgePoint(a0, 0.35f), p1 = isle.EdgePoint(a1, 0.35f);
            Vector3 mid = (p0 + p1) * 0.5f;
            bool skip = false;
            foreach (var ga in gapAngles)
            {
                Vector3 gp = isle.EdgePoint(ga, 0.35f);
                if (Vector3.Distance(mid, gp) < gapHalfWidth) skip = true;
            }
            if (skip) continue;
            InvisibleCollider("Edge", g, mid + Vector3.up * 1f, Quaternion.LookRotation(p1 - p0), new Vector3(0.3f, 2f, Vector3.Distance(p0, p1) + 0.15f));
        }
    }
}
#endif

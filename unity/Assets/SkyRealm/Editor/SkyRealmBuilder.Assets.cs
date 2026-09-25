#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

// Part 1 of the Sky Realm builder: downloads CC0 textures, generates procedural textures, creates materials.
public static partial class SkyRealmBuilder
{
    const string Root = "Assets/SkyRealm";
    const string TexDir = Root + "/Textures";
    const string PhDir = TexDir + "/PolyHaven";
    const string GenDir = TexDir + "/Generated";
    const string MatDir = Root + "/Materials";
    const string GeneratedDir = Root + "/Generated";

    // Poly Haven CC0 textures (https://polyhaven.com, public domain). Downloaded on the PC by the editor.
    static readonly string[] PhIds =
    {
        "monastery_stone_floor", // plaza / walkway floors
        "cliff_side",            // island cliffs
        "mossy_rock",            // natural island tops
        "medieval_blocks_02",    // arches, obelisks, pedestals
        "japanese_cedar_planks", // bridges, torii
        "japanese_stone_wall",   // lanterns, curbs
    };

    static Dictionary<string, Material> M = new Dictionary<string, Material>();

    static void EnsureDirs()
    {
        foreach (var d in new[] { Root, TexDir, PhDir, GenDir, MatDir, GeneratedDir })
            Directory.CreateDirectory(d);
        AssetDatabase.Refresh();
    }

    // ------------------------------------------------------------------ Poly Haven download
    static void DownloadPolyHaven(bool force)
    {
        int i = 0, total = PhIds.Length * 2, ok = 0, failed = 0;
        foreach (var id in PhIds)
        {
            foreach (var kind in new[] { "diff", "nor_gl" })
            {
                string file = id + "_" + kind + "_2k.jpg";
                string dst = PhDir + "/" + file;
                EditorUtility.DisplayProgressBar("Sky Realm", "Downloading CC0 texture " + file, (float)i++ / total);
                if (File.Exists(dst) && !force) { ok++; continue; }
                string url = "https://dl.polyhaven.org/file/ph-assets/Textures/jpg/2k/" + id + "/" + file;
                using (var req = UnityWebRequest.Get(url))
                {
                    req.timeout = 90;
                    var op = req.SendWebRequest();
                    while (!op.isDone) System.Threading.Thread.Sleep(20);
                    if (req.result == UnityWebRequest.Result.Success && req.downloadHandler.data != null && req.downloadHandler.data.Length > 1000)
                    {
                        File.WriteAllBytes(dst, req.downloadHandler.data);
                        ok++;
                    }
                    else
                    {
                        failed++;
                        Log("download failed " + url + " : " + req.error);
                    }
                }
            }
        }
        EditorUtility.ClearProgressBar();
        AssetDatabase.Refresh();
        foreach (var id in PhIds)
        {
            ConfigureTex(PhDir + "/" + id + "_diff_2k.jpg", false);
            ConfigureTex(PhDir + "/" + id + "_nor_gl_2k.jpg", true);
        }
        Log("Poly Haven textures ready: " + ok + " ok, " + failed + " failed");
    }

    static void ConfigureTex(string path, bool normal)
    {
        var ti = AssetImporter.GetAtPath(path) as TextureImporter;
        if (ti == null) return;
        bool dirty = false;
        var wantType = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
        if (ti.textureType != wantType) { ti.textureType = wantType; dirty = true; }
        if (ti.maxTextureSize != 2048) { ti.maxTextureSize = 2048; dirty = true; }
        if (ti.wrapMode != TextureWrapMode.Repeat) { ti.wrapMode = TextureWrapMode.Repeat; dirty = true; }
        if (dirty) ti.SaveAndReimport();
    }

    static Texture2D PH(string id, string kind)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(PhDir + "/" + id + "_" + kind + "_2k.jpg");
    }

    // ------------------------------------------------------------------ procedural textures
    static float SS(float a, float b, float x)
    {
        float t = Mathf.Clamp01((x - a) / (b - a));
        return t * t * (3f - 2f * t);
    }

    static float Fbm(float x, float y, int oct)
    {
        float sum = 0f, amp = 0.5f, f = 1f;
        for (int o = 0; o < oct; o++)
        {
            sum += amp * Mathf.PerlinNoise(x * f + 17.3f * o, y * f + 31.7f * o);
            f *= 2.03f; amp *= 0.5f;
        }
        return sum / (1f - Mathf.Pow(0.5f, oct));
    }

    static uint Hash(uint x)
    {
        x ^= x >> 16; x *= 0x7feb352d; x ^= x >> 15; x *= 0x846ca68b; x ^= x >> 16;
        return x;
    }

    static float Hash01(int a, int b) { return (Hash((uint)(a * 73856093) ^ (uint)(b * 19349663)) & 0xffffff) / 16777215f; }

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 pa = p - a, ba = b - a;
        float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Vector2.Dot(ba, ba));
        return (pa - ba * h).magnitude;
    }

    static Texture2D Gen(string name) { return AssetDatabase.LoadAssetAtPath<Texture2D>(GenDir + "/" + name + ".png"); }

    static Texture2D SaveTex(Texture2D t, string name, TextureWrapMode wrapU, TextureWrapMode wrapV, bool mips, int maxSize)
    {
        string p = GenDir + "/" + name + ".png";
        File.WriteAllBytes(p, t.EncodeToPNG());
        Object.DestroyImmediate(t);
        AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceUpdate);
        var ti = (TextureImporter)AssetImporter.GetAtPath(p);
        ti.textureType = TextureImporterType.Default;
        ti.alphaIsTransparency = true;
        ti.wrapModeU = wrapU;
        ti.wrapModeV = wrapV;
        ti.mipmapEnabled = mips;
        ti.maxTextureSize = maxSize;
        ti.textureCompression = TextureImporterCompression.CompressedHQ;
        ti.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(p);
    }

    static void GenerateTextures(bool force)
    {
        EditorUtility.DisplayProgressBar("Sky Realm", "Painting night sky", 0.1f);
        if (force || !Gen("SR_Sky")) GenSky();
        EditorUtility.DisplayProgressBar("Sky Realm", "Drawing rune circle", 0.4f);
        if (force || !Gen("SR_RuneRing")) GenRune();
        if (force || !Gen("SR_Paw")) GenPaw();
        EditorUtility.DisplayProgressBar("Sky Realm", "Banners, clouds, water", 0.7f);
        if (force || !Gen("SR_Banner")) GenBanner();
        if (force || !Gen("SR_Cloud")) GenCloud();
        if (force || !Gen("SR_Glow")) GenGlow();
        if (force || !Gen("SR_Petal")) GenPetal();
        if (force || !Gen("SR_Water")) GenWater();
        if (force || !Gen("SR_Moon")) GenMoon();
        if (force || !Gen("SR_Noise")) GenNoise();
        EditorUtility.ClearProgressBar();
    }

    // Direction for a pixel of a lat-long panorama, matching Unity's Skybox/Panoramic mapping.
    static Vector3 PanoDir(float u, float v)
    {
        float lon = (0.5f - u) * 2f * Mathf.PI;
        float lat = (1f - v) * Mathf.PI;
        return new Vector3(Mathf.Sin(lat) * Mathf.Cos(lon), Mathf.Cos(lat), Mathf.Sin(lat) * Mathf.Sin(lon));
    }

    static readonly Vector3 MoonDir = new Vector3(0.42f, 0.36f, 1f).normalized;

    static void GenSky()
    {
        int W = 4096, H = 2048;
        var t = new Texture2D(W, H, TextureFormat.RGB24, false);
        var px = new Color[W * H];
        Color horizon = new Color(0.66f, 0.58f, 0.90f);
        Color mid = new Color(0.30f, 0.22f, 0.58f);
        Color zenith = new Color(0.05f, 0.04f, 0.16f);
        Color below = new Color(0.40f, 0.35f, 0.70f);
        Color nebula = new Color(0.40f, 0.12f, 0.50f);
        Color cloudC = new Color(0.78f, 0.72f, 0.97f);
        for (int y = 0; y < H; y++)
        {
            float v = (y + 0.5f) / H;
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W;
                Vector3 d = PanoDir(u, v);
                float e = d.y; // -1..1
                Color c;
                if (e >= 0f)
                    c = e < 0.22f ? Color.Lerp(horizon, mid, SS(0f, 0.22f, e)) : Color.Lerp(mid, zenith, SS(0.22f, 0.95f, e));
                else
                    c = Color.Lerp(horizon, below, Mathf.Clamp01(-e * 3f));

                // nebula
                float n = Fbm(d.x * 2.2f + d.z * 0.7f + 10f, d.y * 2.6f + d.z * 1.9f + 5f, 3);
                float neb = Mathf.Max(0f, n - 0.5f) * 2.2f * (e > 0f ? (1f - e) : 0.2f);
                c += nebula * neb;

                // cloud banks near the horizon
                float band = Mathf.Exp(-Mathf.Pow((e - 0.04f) / 0.09f, 2f));
                float cn = Fbm(d.x * 5f + 3f, d.z * 5f + d.y * 9f, 4);
                c = Color.Lerp(c, cloudC, Mathf.Clamp01(band * SS(0.45f, 0.75f, cn)) * 0.75f);

                // moon glow
                float md = Mathf.Max(0f, Vector3.Dot(d, MoonDir));
                c += new Color(0.55f, 0.48f, 0.75f) * (Mathf.Pow(md, 90f) * 0.9f + Mathf.Pow(md, 12f) * 0.18f);

                // stars
                if (e > 0.06f)
                {
                    float h = Hash01(x, y);
                    if (h > 0.9985f)
                    {
                        float b = (h - 0.9985f) / 0.0015f;
                        c += Color.white * (0.35f + 0.65f * b) * SS(0.06f, 0.3f, e);
                    }
                }
                px[y * W + x] = c;
            }
        }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Sky", TextureWrapMode.Repeat, TextureWrapMode.Clamp, false, 4096);
    }

    static float Line(float dist, float w, float aa) { return 1f - SS(w, w + aa, dist); }

    static float PawMask(Vector2 p, float aa)
    {
        float a = 0f;
        a = Mathf.Max(a, Ellipse(p, new Vector2(0f, -0.14f), new Vector2(0.20f, 0.155f), aa));
        a = Mathf.Max(a, Ellipse(p, new Vector2(-0.235f, 0.10f), new Vector2(0.075f, 0.095f), aa));
        a = Mathf.Max(a, Ellipse(p, new Vector2(-0.085f, 0.22f), new Vector2(0.078f, 0.10f), aa));
        a = Mathf.Max(a, Ellipse(p, new Vector2(0.085f, 0.22f), new Vector2(0.078f, 0.10f), aa));
        a = Mathf.Max(a, Ellipse(p, new Vector2(0.235f, 0.10f), new Vector2(0.075f, 0.095f), aa));
        return a;
    }

    static float Ellipse(Vector2 p, Vector2 c, Vector2 r, float aa)
    {
        Vector2 q = new Vector2((p.x - c.x) / r.x, (p.y - c.y) / r.y);
        float d = (q.magnitude - 1f) * Mathf.Min(r.x, r.y);
        return 1f - SS(-aa * 0.5f, aa * 0.5f, d);
    }

    static void GenRune()
    {
        int N = 1024;
        float aa = 3f / N;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        // glyph strokes, 24 glyphs x 3 segments, in local (radial, tangential) space
        int G = 24;
        var glyph = new Vector2[G * 6];
        var rnd = new System.Random(7);
        for (int k = 0; k < G * 6; k++)
            glyph[k] = new Vector2((float)rnd.NextDouble() * 0.10f - 0.05f, (float)rnd.NextDouble() * 0.10f - 0.05f);
        var star = new Vector2[8];
        for (int j = 0; j < 8; j++)
        {
            float a = (j * 45f + 22.5f) * Mathf.Deg2Rad;
            star[j] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.64f;
        }
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f);
                float d = p.magnitude;
                float ang = Mathf.Atan2(p.y, p.x);
                float a = 0f;
                a = Mathf.Max(a, Line(Mathf.Abs(d - 0.955f), 0.012f, aa));
                a = Mathf.Max(a, Line(Mathf.Abs(d - 0.895f), 0.005f, aa));
                a = Mathf.Max(a, Line(Mathf.Abs(d - 0.705f), 0.010f, aa));
                a = Mathf.Max(a, Line(Mathf.Abs(d - 0.655f), 0.004f, aa));
                a = Mathf.Max(a, Line(Mathf.Abs(d - 0.32f), 0.006f, aa));
                // ticks
                if (d > 0.9f && d < 0.95f)
                {
                    float step = Mathf.PI * 2f / 96f;
                    float nearest = Mathf.Round(ang / step) * step;
                    a = Mathf.Max(a, Line(Mathf.Abs(ang - nearest) * d, 0.003f, aa));
                }
                // glyph band
                if (d > 0.71f && d < 0.89f)
                {
                    float sector = Mathf.PI * 2f / G;
                    int k = Mathf.FloorToInt((ang + Mathf.PI) / sector);
                    k = Mathf.Clamp(k, 0, G - 1);
                    float ca = -Mathf.PI + (k + 0.5f) * sector;
                    Vector2 local = new Vector2(d - 0.80f, (ang - ca) * d);
                    float gd = 10f;
                    for (int s = 0; s < 3; s++)
                        gd = Mathf.Min(gd, SegDist(local, glyph[k * 6 + s * 2], glyph[k * 6 + s * 2 + 1]));
                    a = Mathf.Max(a, Line(gd, 0.006f, aa));
                }
                // eight-point star
                if (d < 0.67f && d > 0.33f)
                {
                    float sd = 10f;
                    for (int j = 0; j < 8; j++) sd = Mathf.Min(sd, SegDist(p, star[j], star[(j + 3) % 8]));
                    a = Mathf.Max(a, Line(sd, 0.004f, aa));
                }
                // soft inner glow
                a = Mathf.Max(a, 0.18f * Mathf.Exp(-Mathf.Pow((d - 0.93f) / 0.05f, 2f)));
                a *= 1f - SS(0.97f, 0.995f, d);
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_RuneRing", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 1024);
    }

    static void GenPaw()
    {
        int N = 512;
        float aa = 3f / N;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f);
                float a = PawMask(p * 1.1f, aa);
                a = Mathf.Max(a, 0.25f * Mathf.Exp(-p.sqrMagnitude / 0.12f));
                px[y * N + x] = new Color(1f, 1f, 1f, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Paw", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 512);
    }

    static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float s1 = (b.x - a.x) * (p.y - a.y) - (b.y - a.y) * (p.x - a.x);
        float s2 = (c.x - b.x) * (p.y - b.y) - (c.y - b.y) * (p.x - b.x);
        float s3 = (a.x - c.x) * (p.y - c.y) - (a.y - c.y) * (p.x - c.x);
        bool neg = s1 < 0 || s2 < 0 || s3 < 0, pos = s1 > 0 || s2 > 0 || s3 > 0;
        return !(neg && pos);
    }

    static void GenBanner()
    {
        int W = 256, H = 512;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var px = new Color[W * H];
        Color cloth = new Color(0.40f, 0.06f, 0.16f);
        Color trim = new Color(0.78f, 0.58f, 0.32f);
        Color emblem = new Color(0.95f, 0.88f, 0.93f);
        Color dark = new Color(0.16f, 0.03f, 0.08f);
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                float alpha = v < 0.12f - Mathf.Abs(u - 0.5f) * 0.24f ? 0f : 1f;
                float weave = 0.92f + 0.08f * Mathf.PerlinNoise(u * 60f, v * 120f);
                Color c = cloth * weave;
                float border = Mathf.Min(Mathf.Min(u, 1f - u), 1f - v);
                if (border < 0.035f) c = dark;
                else if (border < 0.06f) c = trim;
                // emblem (cat face) in a round frame
                Vector2 q = new Vector2((u - 0.5f), (v - 0.63f) * 2f);
                float r = q.magnitude;
                if (r < 0.36f && r > 0.33f) c = trim;
                bool head = ((q.x / 0.24f) * (q.x / 0.24f) + ((q.y + 0.03f) / 0.2f) * ((q.y + 0.03f) / 0.2f)) < 1f;
                bool earL = InTri(q, new Vector2(-0.23f, 0.06f), new Vector2(-0.19f, 0.27f), new Vector2(-0.06f, 0.14f));
                bool earR = InTri(q, new Vector2(0.23f, 0.06f), new Vector2(0.19f, 0.27f), new Vector2(0.06f, 0.14f));
                if (head || earL || earR) c = emblem;
                bool eyeL = (((q.x + 0.095f) / 0.055f) * ((q.x + 0.095f) / 0.055f) + ((q.y + 0.0f) / 0.03f) * ((q.y + 0.0f) / 0.03f)) < 1f;
                bool eyeR = (((q.x - 0.095f) / 0.055f) * ((q.x - 0.095f) / 0.055f) + ((q.y + 0.0f) / 0.03f) * ((q.y + 0.0f) / 0.03f)) < 1f;
                bool nose = InTri(q, new Vector2(-0.025f, -0.06f), new Vector2(0.025f, -0.06f), new Vector2(0f, -0.09f));
                if (eyeL || eyeR || nose) c = cloth * 0.8f;
                c.a = alpha;
                px[y * W + x] = c;
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Banner", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 512);
    }

    static void GenCloud()
    {
        int N = 256;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v);
                float n = Fbm(u * 2.5f + 4f, v * 2.5f + 9f, 4);
                float a = Mathf.Clamp01((1f - d) * 1.6f) * SS(0.3f, 0.75f, n + (1f - d) * 0.35f);
                float shade = 0.82f + 0.18f * SS(-0.6f, 0.6f, v);
                px[y * N + x] = new Color(shade, shade, shade, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Cloud", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 256);
    }

    static void GenGlow()
    {
        int N = 128;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
                px[y * N + x] = new Color(1f, 1f, 1f, Mathf.Pow(1f - d, 2.2f));
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Glow", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 128);
    }

    static void GenPetal()
    {
        int N = 64;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                Vector2 p = new Vector2((x + 0.5f) / N * 2f - 1f, (y + 0.5f) / N * 2f - 1f);
                float a = Ellipse(p, Vector2.zero, new Vector2(0.45f, 0.85f), 0.08f);
                float notch = Ellipse(p, new Vector2(0f, 0.95f), new Vector2(0.18f, 0.2f), 0.08f);
                a = Mathf.Clamp01(a - notch);
                float shade = 0.85f + 0.15f * (1f - Mathf.Abs(p.x));
                px[y * N + x] = new Color(shade, shade, shade, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Petal", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 64);
    }

    static void GenWater()
    {
        int W = 128, H = 512;
        var t = new Texture2D(W, H, TextureFormat.RGBA32, true);
        var px = new Color[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = (x + 0.5f) / W, v = (y + 0.5f) / H;
                // seamless vertical tiling
                float a1 = Fbm(u * 18f, v * 4f, 3);
                float a2 = Fbm(u * 18f, (v - 1f) * 4f, 3);
                float n = Mathf.Lerp(a1, a2, v);
                float streak = SS(0.35f, 0.8f, n);
                float edge = SS(0f, 0.18f, u) * SS(1f, 0.82f, u);
                float a = (0.35f + 0.65f * streak) * edge;
                float b = 0.8f + 0.2f * streak;
                px[y * W + x] = new Color(b, b, 1f, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Water", TextureWrapMode.Clamp, TextureWrapMode.Repeat, true, 512);
    }

    static void GenMoon()
    {
        int N = 512;
        var t = new Texture2D(N, N, TextureFormat.RGBA32, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (x + 0.5f) / N * 2f - 1f, v = (y + 0.5f) / N * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v) / 0.92f;
                float a = 1f - SS(0.985f, 1.0f, d);
                float limb = 0.78f + 0.22f * Mathf.Sqrt(Mathf.Max(0f, 1f - d * d));
                float maria = SS(0.5f, 0.7f, Fbm(u * 2.2f + 3f, v * 2.2f + 7f, 4));
                float craters = SS(0.62f, 0.8f, Fbm(u * 9f, v * 9f, 3));
                float b = limb * (1f - 0.22f * maria - 0.1f * craters);
                px[y * N + x] = new Color(b * 0.97f, b * 0.95f, b, a);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Moon", TextureWrapMode.Clamp, TextureWrapMode.Clamp, true, 512);
    }

    static void GenNoise()
    {
        int N = 512;
        var t = new Texture2D(N, N, TextureFormat.RGB24, true);
        var px = new Color[N * N];
        for (int y = 0; y < N; y++)
            for (int x = 0; x < N; x++)
            {
                float u = (float)x / N, v = (float)y / N;
                // tileable
                float n = Mathf.Lerp(Mathf.Lerp(Fbm(u * 8f, v * 8f, 4), Fbm((u - 1f) * 8f, v * 8f, 4), u),
                                     Mathf.Lerp(Fbm(u * 8f, (v - 1f) * 8f, 4), Fbm((u - 1f) * 8f, (v - 1f) * 8f, 4), u), v);
                float g = 0.55f + 0.45f * n;
                px[y * N + x] = new Color(g, g, g);
            }
        t.SetPixels(px);
        t.Apply();
        SaveTex(t, "SR_Noise", TextureWrapMode.Repeat, TextureWrapMode.Repeat, true, 512);
    }

    // ------------------------------------------------------------------ materials
    static Material GetMat(string key, Shader shader)
    {
        string p = MatDir + "/SR_" + key + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(shader);
            AssetDatabase.CreateAsset(m, p);
        }
        else if (m.shader != shader) m.shader = shader;
        M[key] = m;
        return m;
    }

    static Material Lit(string key, Color tint, string phId, float smooth, float bump = 1f)
    {
        var m = GetMat(key, Shader.Find("Universal Render Pipeline/Lit"));
        m.SetColor("_BaseColor", tint);
        Texture2D alb = phId != null ? PH(phId, "diff") : null;
        Texture2D nor = phId != null ? PH(phId, "nor_gl") : null;
        if (phId != null && alb == null) alb = Gen("SR_Noise"); // fallback when the download failed
        m.SetTexture("_BaseMap", alb);
        if (nor != null)
        {
            m.SetTexture("_BumpMap", nor);
            m.SetFloat("_BumpScale", bump);
            m.EnableKeyword("_NORMALMAP");
        }
        else
        {
            m.SetTexture("_BumpMap", null);
            m.DisableKeyword("_NORMALMAP");
        }
        m.SetFloat("_Smoothness", smooth);
        m.SetFloat("_Metallic", 0f);
        m.DisableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", Color.black);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Emissive(string key, Color baseCol, Color emission, float smooth)
    {
        var m = Lit(key, baseCol, null, smooth);
        m.EnableKeyword("_EMISSION");
        m.SetColor("_EmissionColor", emission);
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material Glow(string key, Texture2D tex, Color col, bool additive, float fog, Vector2 scroll)
    {
        var sh = Shader.Find("SkyRealm/Glow");
        if (sh == null) { Log("ERROR: shader SkyRealm/Glow not found"); sh = Shader.Find("Universal Render Pipeline/Unlit"); }
        var m = GetMat(key, sh);
        m.SetTexture("_BaseMap", tex);
        m.SetColor("_BaseColor", col);
        m.SetVector("_Scroll", new Vector4(scroll.x, scroll.y, 0f, 0f));
        m.SetFloat("_FogAmount", fog);
        m.SetFloat("_Additive", additive ? 1f : 0f);
        m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        m.SetFloat("_DstBlend", additive ? (float)UnityEngine.Rendering.BlendMode.One : (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        m.SetFloat("_Cull", 0f);
        m.renderQueue = 3000;
        EditorUtility.SetDirty(m);
        return m;
    }

    static void CreateMaterials()
    {
        Lit("Floor", new Color(0.80f, 0.76f, 0.92f), "monastery_stone_floor", 0.18f);
        Lit("Cliff", new Color(0.50f, 0.43f, 0.68f), "cliff_side", 0.08f, 1.2f);
        Lit("Moss", new Color(0.62f, 0.66f, 0.72f), "mossy_rock", 0.1f);
        Lit("Blocks", new Color(0.66f, 0.61f, 0.82f), "medieval_blocks_02", 0.12f);
        Lit("StoneWall", new Color(0.64f, 0.60f, 0.78f), "japanese_stone_wall", 0.12f);
        Lit("Wood", new Color(0.56f, 0.47f, 0.44f), "japanese_cedar_planks", 0.15f);
        Lit("Lacquer", new Color(0.80f, 0.20f, 0.13f), "japanese_cedar_planks", 0.4f, 0.4f);
        Lit("DarkWood", new Color(0.20f, 0.14f, 0.16f), "japanese_cedar_planks", 0.3f, 0.5f);
        Lit("Roof", new Color(0.20f, 0.16f, 0.30f), null, 0.35f);
        Lit("Statue", new Color(0.40f, 0.37f, 0.52f), "japanese_stone_wall", 0.25f, 0.35f);
        Lit("Metal", new Color(0.14f, 0.12f, 0.17f), null, 0.55f);
        var metal = M["Metal"]; metal.SetFloat("_Metallic", 0.7f);
        Emissive("LanternGlow", new Color(0.2f, 0.1f, 0.05f), new Color(9f, 4.2f, 1.3f), 0.2f);
        Emissive("Crystal", new Color(0.62f, 0.28f, 0.95f), new Color(2.6f, 0.7f, 4.2f), 0.9f);
        Emissive("RuneStone", new Color(0.3f, 0.15f, 0.5f), new Color(1.4f, 0.4f, 3.4f), 0.4f);
        Emissive("Blossom", new Color(0.95f, 0.58f, 0.90f), new Color(0.16f, 0.04f, 0.18f), 0.05f);
        Emissive("Bush", new Color(0.72f, 0.36f, 0.86f), new Color(0.10f, 0.02f, 0.14f), 0.05f);

        var banner = Lit("Banner", Color.white, null, 0.1f);
        banner.SetTexture("_BaseMap", Gen("SR_Banner"));
        banner.SetFloat("_Surface", 0f);
        banner.SetFloat("_Cull", 0f);
        banner.SetFloat("_AlphaClip", 1f);
        banner.SetFloat("_Cutoff", 0.5f);
        banner.EnableKeyword("_ALPHATEST_ON");
        banner.SetOverrideTag("RenderType", "TransparentCutout");
        banner.renderQueue = 2450;
        banner.EnableKeyword("_EMISSION");
        banner.SetTexture("_EmissionMap", Gen("SR_Banner"));
        banner.SetColor("_EmissionColor", new Color(0.18f, 0.12f, 0.2f));
        EditorUtility.SetDirty(banner);

        var ropeU = GetMat("Rope", Shader.Find("Universal Render Pipeline/Unlit"));
        ropeU.SetColor("_BaseColor", new Color(0.20f, 0.14f, 0.12f));
        var under = GetMat("Underworld", Shader.Find("Universal Render Pipeline/Unlit"));
        under.SetColor("_BaseColor", new Color(0.40f, 0.35f, 0.70f));

        var sky = GetMat("Sky", Shader.Find("Skybox/Panoramic"));
        sky.SetTexture("_MainTex", Gen("SR_Sky"));
        sky.SetFloat("_Exposure", 1f);
        sky.SetFloat("_Rotation", 0f);
        EditorUtility.SetDirty(sky);

        Glow("RuneGlow", Gen("SR_RuneRing"), new Color(1.3f, 0.5f, 3.6f, 1f), true, 1f, Vector2.zero);
        Glow("PawGlow", Gen("SR_Paw"), new Color(1.6f, 0.6f, 4.2f, 1f), true, 1f, Vector2.zero);
        Glow("Portal", Gen("SR_Glow"), new Color(0.9f, 0.3f, 2.4f, 0.9f), true, 1f, Vector2.zero);
        Glow("FlameP", Gen("SR_Glow"), new Color(2.0f, 0.7f, 4.8f, 1f), true, 1f, Vector2.zero);
        Glow("FlameW", Gen("SR_Glow"), new Color(4.0f, 1.8f, 0.6f, 1f), true, 1f, Vector2.zero);
        Glow("Cloud", Gen("SR_Cloud"), new Color(0.72f, 0.64f, 0.96f, 0.55f), false, 0.5f, Vector2.zero);
        Glow("Petal", Gen("SR_Petal"), new Color(1f, 0.72f, 0.96f, 1f), false, 1f, Vector2.zero);
        Glow("Water", Gen("SR_Water"), new Color(0.80f, 0.84f, 1.25f, 0.85f), false, 1f, new Vector2(0f, -0.9f));
        Glow("Moon", Gen("SR_Moon"), new Color(1.7f, 1.6f, 1.9f, 1f), false, 0f, Vector2.zero);
        Glow("MoonHalo", Gen("SR_Glow"), new Color(0.55f, 0.45f, 0.95f, 0.8f), true, 0f, Vector2.zero);
        Glow("Mote", Gen("SR_Glow"), new Color(1.6f, 0.8f, 3.5f, 1f), true, 1f, Vector2.zero);
        AssetDatabase.SaveAssets();
    }
}
#endif

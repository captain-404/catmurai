#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

// Part 2 of the Sky Realm builder: procedural meshes (islands, boxes, cylinders, roofs, crystals, blobs).
// All meshes are stored as sub-assets of Assets/SkyRealm/Generated/SkyRealmMeshes.asset.
public static partial class SkyRealmBuilder
{
    const string MeshAssetPath = GeneratedDir + "/SkyRealmMeshes.asset";
    static Mesh meshRoot;
    static Dictionary<string, Mesh> meshCache = new Dictionary<string, Mesh>();
    static int meshCounter;

    static void BeginMeshes()
    {
        AssetDatabase.DeleteAsset(MeshAssetPath);
        meshRoot = new Mesh { name = "SkyRealmMeshes" };
        AssetDatabase.CreateAsset(meshRoot, MeshAssetPath);
        meshCache.Clear();
        meshCounter = 0;
    }

    static Mesh Reg(Mesh m, string name)
    {
        m.name = name + "_" + (meshCounter++);
        AssetDatabase.AddObjectToAsset(m, meshRoot);
        return m;
    }

    // Winding-safe triangle helper: flips the triangle so its normal points along 'outward'.
    static void Tri(List<int> t, List<Vector3> v, int a, int b, int c, Vector3 outward)
    {
        Vector3 n = Vector3.Cross(v[b] - v[a], v[c] - v[a]);
        if (Vector3.Dot(n, outward) >= 0f) { t.Add(a); t.Add(b); t.Add(c); }
        else { t.Add(a); t.Add(c); t.Add(b); }
    }

    static Mesh Finish(Mesh m, List<Vector3> v, List<Vector2> uv, List<List<int>> subs)
    {
        if (v.Count > 65000) m.indexFormat = IndexFormat.UInt32;
        m.SetVertices(v);
        m.SetUVs(0, uv);
        var cols = new Color[v.Count];
        for (int i = 0; i < cols.Length; i++) cols[i] = Color.white;
        m.colors = cols;
        m.subMeshCount = subs.Count;
        for (int s = 0; s < subs.Count; s++) m.SetTriangles(subs[s], s);
        m.RecalculateNormals();
        m.RecalculateTangents();
        m.RecalculateBounds();
        return m;
    }

    // ------------------------------------------------------------------ box (centered, world-scaled UVs)
    static Mesh BoxMesh(Vector3 s)
    {
        string key = "box_" + s.x.ToString("F2") + "_" + s.y.ToString("F2") + "_" + s.z.ToString("F2");
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        Vector3 h = s * 0.5f;
        const float k = 0.5f; // one texture repeat per 2 m
        void Face(Vector3 n, Vector3 u, Vector3 w)
        {
            Vector3 c = Vector3.Scale(n, h), du = Vector3.Scale(u, h), dw = Vector3.Scale(w, h);
            float su = Mathf.Abs(Vector3.Dot(u, s)), sw = Mathf.Abs(Vector3.Dot(w, s));
            int b = v.Count;
            v.Add(c - du - dw); v.Add(c + du - dw); v.Add(c + du + dw); v.Add(c - du + dw);
            uv.Add(new Vector2(0, 0)); uv.Add(new Vector2(su * k, 0)); uv.Add(new Vector2(su * k, sw * k)); uv.Add(new Vector2(0, sw * k));
            Tri(t, v, b, b + 1, b + 2, n); Tri(t, v, b, b + 2, b + 3, n);
        }
        Face(Vector3.right, Vector3.back, Vector3.up);
        Face(Vector3.left, Vector3.forward, Vector3.up);
        Face(Vector3.forward, Vector3.right, Vector3.up);
        Face(Vector3.back, Vector3.left, Vector3.up);
        Face(Vector3.up, Vector3.forward, Vector3.right);
        Face(Vector3.down, Vector3.right, Vector3.forward);
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        Reg(m, "Box");
        meshCache[key] = m;
        return m;
    }

    // ------------------------------------------------------------------ cylinder (pivot at bottom)
    static Mesh CylMesh(float r, float h, int seg)
    {
        string key = "cyl_" + r.ToString("F2") + "_" + h.ToString("F2") + "_" + seg;
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        float circ = 2f * Mathf.PI * r;
        for (int i = 0; i <= seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
            v.Add(p); uv.Add(new Vector2(i / (float)seg * circ * 0.5f, 0f));
            v.Add(p + Vector3.up * h); uv.Add(new Vector2(i / (float)seg * circ * 0.5f, h * 0.5f));
        }
        for (int i = 0; i < seg; i++)
        {
            int a0 = i * 2, a1 = a0 + 1, b0 = a0 + 2, b1 = a0 + 3;
            Vector3 mid = (v[a0] + v[b0]) * 0.5f; mid.y = 0f;
            Tri(t, v, a0, b0, b1, mid); Tri(t, v, a0, b1, a1, mid);
        }
        foreach (float y in new[] { 0f, h })
        {
            int c = v.Count;
            v.Add(new Vector3(0f, y, 0f)); uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                v.Add(new Vector3(Mathf.Cos(a) * r, y, Mathf.Sin(a) * r));
                uv.Add(new Vector2(Mathf.Cos(a) * r * 0.5f, Mathf.Sin(a) * r * 0.5f));
            }
            Vector3 n = y > 0f ? Vector3.up : Vector3.down;
            for (int i = 0; i < seg; i++) Tri(t, v, c, c + 1 + i, c + 2 + i, n);
        }
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        Reg(m, "Cyl");
        meshCache[key] = m;
        return m;
    }

    // ------------------------------------------------------------------ flat-shaded helpers
    struct FTri { public Vector3 a, b, c, outward; }

    static Mesh FlatMesh(List<FTri> tris, string name)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        foreach (var f in tris)
        {
            int b = v.Count;
            v.Add(f.a); v.Add(f.b); v.Add(f.c);
            Vector3 n = Vector3.Cross(f.b - f.a, f.c - f.a).normalized;
            Vector3 an = new Vector3(Mathf.Abs(n.x), Mathf.Abs(n.y), Mathf.Abs(n.z));
            foreach (var p in new[] { f.a, f.b, f.c })
            {
                if (an.y >= an.x && an.y >= an.z) uv.Add(new Vector2(p.x, p.z) * 0.5f);
                else if (an.x >= an.z) uv.Add(new Vector2(p.z, p.y) * 0.5f);
                else uv.Add(new Vector2(p.x, p.y) * 0.5f);
            }
            Tri(t, v, b, b + 1, b + 2, f.outward);
        }
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        return Reg(m, name);
    }

    // Pyramid / cone roof with a bottom cap. Pivot at bottom centre.
    static Mesh ConeMesh(float r, float h, int seg, float rotDeg)
    {
        string key = "cone_" + r.ToString("F2") + "_" + h.ToString("F2") + "_" + seg + "_" + rotDeg.ToString("F0");
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        var tris = new List<FTri>();
        Vector3 apex = new Vector3(0f, h, 0f);
        for (int i = 0; i < seg; i++)
        {
            float a0 = (i / (float)seg * 360f + rotDeg) * Mathf.Deg2Rad, a1 = ((i + 1) / (float)seg * 360f + rotDeg) * Mathf.Deg2Rad;
            var p0 = new Vector3(Mathf.Cos(a0) * r, 0f, Mathf.Sin(a0) * r);
            var p1 = new Vector3(Mathf.Cos(a1) * r, 0f, Mathf.Sin(a1) * r);
            var mid = (p0 + p1 + apex) / 3f;
            tris.Add(new FTri { a = p0, b = p1, c = apex, outward = new Vector3(mid.x, h * 0.3f, mid.z) });
            tris.Add(new FTri { a = p0, b = p1, c = Vector3.zero, outward = Vector3.down });
        }
        var m = FlatMesh(tris, "Cone");
        meshCache[key] = m;
        return m;
    }

    // Hexagonal crystal (bipyramid), unit size: height 1, radius 0.22, pivot near the bottom point.
    static Mesh CrystalMesh()
    {
        if (meshCache.TryGetValue("crystal", out var cached)) return cached;
        var tris = new List<FTri>();
        Vector3 top = new Vector3(0f, 1f, 0f), bot = new Vector3(0f, -0.12f, 0f);
        int seg = 6;
        for (int i = 0; i < seg; i++)
        {
            float a0 = i / (float)seg * Mathf.PI * 2f, a1 = (i + 1) / (float)seg * Mathf.PI * 2f;
            var p0 = new Vector3(Mathf.Cos(a0) * 0.22f, 0.22f, Mathf.Sin(a0) * 0.22f);
            var p1 = new Vector3(Mathf.Cos(a1) * 0.22f, 0.22f, Mathf.Sin(a1) * 0.22f);
            var q0 = new Vector3(Mathf.Cos(a0) * 0.2f, 0.68f, Mathf.Sin(a0) * 0.2f);
            var q1 = new Vector3(Mathf.Cos(a1) * 0.2f, 0.68f, Mathf.Sin(a1) * 0.2f);
            Vector3 o = new Vector3((p0.x + p1.x), 0f, (p0.z + p1.z));
            tris.Add(new FTri { a = p0, b = p1, c = q1, outward = o });
            tris.Add(new FTri { a = p0, b = q1, c = q0, outward = o });
            tris.Add(new FTri { a = q0, b = q1, c = top, outward = o + Vector3.up * 0.3f });
            tris.Add(new FTri { a = p0, b = p1, c = bot, outward = o - Vector3.up * 0.3f });
        }
        var m = FlatMesh(tris, "Crystal");
        meshCache["crystal"] = m;
        return m;
    }

    // Lumpy sphere (foliage / bushes), unit radius.
    static Mesh BlobMesh(int seed)
    {
        string key = "blob_" + seed;
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        float p = (1f + Mathf.Sqrt(5f)) / 2f;
        var verts = new List<Vector3>
        {
            new Vector3(-1, p, 0), new Vector3(1, p, 0), new Vector3(-1, -p, 0), new Vector3(1, -p, 0),
            new Vector3(0, -1, p), new Vector3(0, 1, p), new Vector3(0, -1, -p), new Vector3(0, 1, -p),
            new Vector3(p, 0, -1), new Vector3(p, 0, 1), new Vector3(-p, 0, -1), new Vector3(-p, 0, 1),
        };
        int[] f =
        {
            0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
            3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1
        };
        var faces = new List<int>(f);
        for (int i = 0; i < verts.Count; i++) verts[i] = verts[i].normalized;
        for (int iter = 0; iter < 2; iter++)
        {
            var nf = new List<int>();
            var midCache = new Dictionary<long, int>();
            int Mid(int a, int b)
            {
                long key2 = a < b ? ((long)a << 32) + b : ((long)b << 32) + a;
                if (midCache.TryGetValue(key2, out int idx)) return idx;
                verts.Add(((verts[a] + verts[b]) * 0.5f).normalized);
                midCache[key2] = verts.Count - 1;
                return verts.Count - 1;
            }
            for (int i = 0; i < faces.Count; i += 3)
            {
                int a = faces[i], b = faces[i + 1], c = faces[i + 2];
                int ab = Mid(a, b), bc = Mid(b, c), ca = Mid(c, a);
                nf.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
            }
            faces = nf;
        }
        var rnd = new System.Random(seed);
        float ph1 = (float)rnd.NextDouble() * 10f, ph2 = (float)rnd.NextDouble() * 10f;
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        foreach (var q in verts)
        {
            float n = Mathf.PerlinNoise(q.x * 1.7f + ph1, q.z * 1.7f + q.y * 0.9f + ph2);
            v.Add(q * (0.82f + 0.36f * n));
            uv.Add(new Vector2(q.x + q.z, q.y));
        }
        var t = new List<int>();
        for (int i = 0; i < faces.Count; i += 3)
        {
            var c = (v[faces[i]] + v[faces[i + 1]] + v[faces[i + 2]]) / 3f;
            Tri(t, v, faces[i], faces[i + 1], faces[i + 2], c);
        }
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        Reg(m, "Blob");
        meshCache[key] = m;
        return m;
    }

    // Vertical quad. pivotTop: spans y in [-h,0]; otherwise centred. Faces -Z (use a Cull Off material).
    static Mesh QuadMesh(float w, float h, bool pivotTop)
    {
        string key = "quad_" + w.ToString("F2") + "_" + h.ToString("F2") + "_" + pivotTop;
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        float y0 = pivotTop ? -h : -h * 0.5f, y1 = pivotTop ? 0f : h * 0.5f;
        var v = new List<Vector3> { new Vector3(-w / 2, y0, 0), new Vector3(w / 2, y0, 0), new Vector3(w / 2, y1, 0), new Vector3(-w / 2, y1, 0) };
        var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        var t = new List<int>();
        Tri(t, v, 0, 1, 2, Vector3.back); Tri(t, v, 0, 2, 3, Vector3.back);
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        Reg(m, "Quad");
        meshCache[key] = m;
        return m;
    }

    // Horizontal quad facing up, centred.
    static Mesh FlatQuadMesh(float size)
    {
        string key = "fquad_" + size.ToString("F2");
        if (meshCache.TryGetValue(key, out var cached)) return cached;
        float s = size / 2f;
        var v = new List<Vector3> { new Vector3(-s, 0, -s), new Vector3(s, 0, -s), new Vector3(s, 0, s), new Vector3(-s, 0, s) };
        var uv = new List<Vector2> { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        var t = new List<int>();
        Tri(t, v, 0, 1, 2, Vector3.up); Tri(t, v, 0, 2, 3, Vector3.up);
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { t });
        Reg(m, "FlatQuad");
        meshCache[key] = m;
        return m;
    }

    // Curved falling water sheet. Local +Z = outward from the cliff, top edge at y = 0.
    static Mesh WaterfallMesh(float w, float h)
    {
        var v = new List<Vector3>(); var uv = new List<Vector2>(); var t = new List<int>();
        int rows = 14;
        var cols = new List<Color>();
        for (int r = 0; r <= rows; r++)
        {
            float k = r / (float)rows;
            float y = -k * h;
            float z = 0.25f + 1.4f * Mathf.Sqrt(k);
            float ww = w * (1f + 0.35f * k);
            v.Add(new Vector3(-ww / 2, y, z)); uv.Add(new Vector2(0f, -y / 6f));
            v.Add(new Vector3(ww / 2, y, z)); uv.Add(new Vector2(1f, -y / 6f));
            float a = (1f - k * k) * SS(0f, 0.04f, k + 0.01f);
            cols.Add(new Color(1, 1, 1, a)); cols.Add(new Color(1, 1, 1, a));
        }
        for (int r = 0; r < rows; r++)
        {
            int a = r * 2;
            Tri(t, v, a, a + 1, a + 3, Vector3.forward); Tri(t, v, a, a + 3, a + 2, Vector3.forward);
        }
        var m = new Mesh();
        m.SetVertices(v); m.SetUVs(0, uv); m.SetColors(cols); m.SetTriangles(t, 0);
        m.RecalculateNormals(); m.RecalculateBounds();
        return Reg(m, "Waterfall");
    }

    // ------------------------------------------------------------------ floating island
    class Isle
    {
        public Vector3 c; public float R; public float[] rad; public Transform t;
        public float Edge(float ang)
        {
            float tw = Mathf.PI * 2f;
            ang %= tw; if (ang < 0f) ang += tw;
            float f = ang / tw * rad.Length;
            int i = Mathf.FloorToInt(f);
            return Mathf.Lerp(rad[i % rad.Length], rad[(i + 1) % rad.Length], f - i) * R;
        }
        public Vector3 EdgePoint(float ang, float inset)
        {
            float r = Edge(ang) - inset;
            return c + new Vector3(Mathf.Cos(ang) * r, 0f, Mathf.Sin(ang) * r);
        }
        public float AngleTo(Vector3 p) { return Mathf.Atan2(p.z - c.z, p.x - c.x); }
    }

    /// Floating island: flat (or gently domed) top at y=0 = submesh 0, jagged cliff tapering to a spike = submesh 1.
    static Mesh IslandMesh(int seed, float R, float depth, float bump, float wobble, out float[] rad)
    {
        var rnd = new System.Random(seed);
        float Rr() { return (float)rnd.NextDouble(); }
        int seg = 36, rings = 9;
        rad = new float[seg];
        float p1 = Rr() * 6.28f, p2 = Rr() * 6.28f, p3 = Rr() * 6.28f;
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            rad[i] = 1f + wobble * (0.10f * Mathf.Sin(2 * a + p1) + 0.06f * Mathf.Sin(3 * a + p2) + 0.04f * Mathf.Sin(5 * a + p3) + (Rr() - 0.5f) * 0.05f);
        }
        var v = new List<Vector3>(); var uv = new List<Vector2>();
        var top = new List<int>(); var side = new List<int>();

        // top: centre + inner ring + outer ring
        v.Add(new Vector3(0f, bump, 0f)); uv.Add(Vector2.zero);
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * R * rad[i] * 0.55f, bump * 0.65f, Mathf.Sin(a) * R * rad[i] * 0.55f);
            v.Add(p); uv.Add(new Vector2(p.x, p.z) / 5f);
        }
        for (int i = 0; i < seg; i++)
        {
            float a = i / (float)seg * Mathf.PI * 2f;
            var p = new Vector3(Mathf.Cos(a) * R * rad[i], 0f, Mathf.Sin(a) * R * rad[i]);
            v.Add(p); uv.Add(new Vector2(p.x, p.z) / 5f);
        }
        for (int i = 0; i < seg; i++)
        {
            int j = (i + 1) % seg;
            Tri(top, v, 0, 1 + i, 1 + j, Vector3.up);
            Tri(top, v, 1 + i, 1 + seg + i, 1 + seg + j, Vector3.up);
            Tri(top, v, 1 + i, 1 + seg + j, 1 + j, Vector3.up);
        }

        // cliffs
        int baseIdx = v.Count;
        var tipOff = new Vector2((Rr() - 0.5f) * R * 0.4f, (Rr() - 0.5f) * R * 0.4f);
        float circ = 2f * Mathf.PI * R;
        var jit = new float[(rings + 1) * seg];
        for (int k = 0; k < jit.Length; k++) jit[k] = Rr() - 0.5f;
        for (int k = 0; k <= rings; k++)
        {
            float tk = k / (float)rings;
            float y = k == 0 ? 0f : (k == 1 ? -0.45f : -depth * Mathf.Pow(tk, 1.15f));
            float f = k == 1 ? 1.03f : (1f - 0.22f * tk) * (1f - Mathf.Pow(tk, 1.9f));
            for (int i = 0; i <= seg; i++)
            {
                int ii = i % seg;
                float a = ii / (float)seg * Mathf.PI * 2f + (k > 1 ? jit[k * seg + ii] * 0.08f : 0f);
                float r = R * rad[ii] * f * (1f + (k > 1 ? jit[k * seg + ii] * 0.28f * wobble + jit[((k * 7) % (rings + 1)) * seg + ii] * 0.12f : 0f));
                if (k == rings) r = 0f;
                float ox = tipOff.x * tk * tk, oz = tipOff.y * tk * tk;
                v.Add(new Vector3(Mathf.Cos(a) * r + ox, y + (k > 1 ? jit[k * seg + ii] * 0.6f : 0f), Mathf.Sin(a) * r + oz));
                uv.Add(new Vector2(i / (float)seg * circ / 6f, y / 6f));
            }
        }
        int cols = seg + 1;
        for (int k = 0; k < rings; k++)
            for (int i = 0; i < seg; i++)
            {
                int a = baseIdx + k * cols + i, b = a + 1, c = a + cols + 1, d = a + cols;
                Vector3 mid = (v[a] + v[b] + v[c] + v[d]) * 0.25f;
                Vector3 o = new Vector3(mid.x - tipOff.x * 0.5f, 0f, mid.z - tipOff.y * 0.5f);
                if (o.sqrMagnitude < 1e-4f) o = new Vector3(v[a].x, 0f, v[a].z);
                o = o.normalized + Vector3.down * 0.25f;
                Tri(side, v, a, b, c, o);
                Tri(side, v, a, c, d, o);
            }
        var m = Finish(new Mesh(), v, uv, new List<List<int>> { top, side });
        return Reg(m, "Island");
    }

    // ------------------------------------------------------------------ Catmurai statue (baked pose)
    static Mesh catStatueMesh;
    static float catStatueBottom;

    static void BakeCatStatue()
    {
        catStatueMesh = null;
        var model = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Catmurai/Models/Catmurai_Drawn_ue.fbx");
        if (model == null) { Log("Catmurai model not found, statues skipped"); return; }
        var inst = (GameObject)Object.Instantiate(model);
        inst.transform.position = Vector3.zero;
        inst.transform.rotation = Quaternion.identity;
        try
        {
            AnimationClip clip = null;
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath("Assets/Catmurai/Anims/Anim_Idle_ue.fbx"))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) { clip = c; break; }
            if (clip != null) clip.SampleAnimation(inst, 0.2f);
            var combines = new List<CombineInstance>();
            Matrix4x4 rootInv = inst.transform.worldToLocalMatrix;
            foreach (var smr in inst.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                var baked = new Mesh { indexFormat = IndexFormat.UInt32 };
                smr.BakeMesh(baked, true);
                var tr = rootInv * Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                for (int s = 0; s < baked.subMeshCount; s++)
                    combines.Add(new CombineInstance { mesh = baked, subMeshIndex = s, transform = tr });
            }
            foreach (var mf in inst.GetComponentsInChildren<MeshFilter>())
            {
                if (mf.sharedMesh == null) continue;
                for (int s = 0; s < mf.sharedMesh.subMeshCount; s++)
                    combines.Add(new CombineInstance { mesh = mf.sharedMesh, subMeshIndex = s, transform = rootInv * mf.transform.localToWorldMatrix });
            }
            if (combines.Count == 0) { Log("Catmurai model has no meshes"); return; }
            var result = new Mesh { indexFormat = IndexFormat.UInt32 };
            result.CombineMeshes(combines.ToArray(), true, true);
            result.RecalculateBounds();
            catStatueBottom = result.bounds.min.y;
            catStatueMesh = Reg(result, "CatmuraiStatue");
            Log("Statue mesh baked: " + result.vertexCount + " verts, height " + result.bounds.size.y.ToString("F2"));
        }
        catch (System.Exception e) { Log("Statue bake failed: " + e.Message); }
        finally { Object.DestroyImmediate(inst); }
    }
}
#endif

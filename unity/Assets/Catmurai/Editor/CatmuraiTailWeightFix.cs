using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Collections;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Fixes the "glitchy spikes at the tail end" on the Catmurai body meshes (LOD0-3) by making fixed COPIES of the
// meshes (originals untouched) and assigning them to the renderers in the open scene:
//  1. tail_06 weights are merged into tail_05 (the tip moves as one piece with the bone before it).
//  2. vertices that are mostly tail, or on the lower tail (tail_03+), lose their leg/cape/pelvis weights;
//     leg/cape vertices that picked up a bit of tail weight lose the tail weight.
//  3. the few triangles bridging tail vertices to cape/leg vertices are removed (they stretched into spikes).
//  4. cape vertices lose leg weights and leg vertices lose cape weights; triangles bridging cape to legs are removed
//     (the centre cape panel over the tail end was stitched to the thighs and stretched into spikes when running).
// Revert: menu Catmurai > Tail Fix > Revert to Original Meshes.
public static class CatmuraiTailWeightFix
{
    const string OutDir = "Assets/Catmurai/Models/TailFix";
    static readonly string[] LegCape = { "thigh", "calf", "foot", "ball", "cape" };

    static bool IsTail(string n) => n.StartsWith("tail_");
    static int TailIdx(string n) => int.Parse(n.Substring(5, 2));
    static bool IsLegCape(string n) => LegCape.Any(p => n.StartsWith(p));
    static readonly string[] Legs = { "thigh", "calf", "foot", "ball" };
    static bool IsLeg(string n) => Legs.Any(p => n.StartsWith(p));
    static bool IsCape(string n) => n.StartsWith("cape");

    static Mesh Original(Mesh m)
    {
        if (!AssetDatabase.GetAssetPath(m).StartsWith(OutDir)) return m;
        string baseName = m.name.Replace("_tailfix", "");
        string file = baseName == "Catmurai_LOD0" ? "Assets/Catmurai/Models/Catmurai_Drawn_ue.fbx" : "Assets/Catmurai/Models/Catmurai_Base_LODs_ue.fbx";
        return AssetDatabase.LoadAllAssetsAtPath(file).OfType<Mesh>().FirstOrDefault(x => x.name == baseName);
    }

    [MenuItem("Catmurai/Tail Fix/Apply to Player Meshes")]
    static void Apply()
    {
        if (!AssetDatabase.IsValidFolder(OutDir)) AssetDatabase.CreateFolder("Assets/Catmurai/Models", "TailFix");
        int done = 0;
        foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (smr.sharedMesh == null || !smr.name.StartsWith("Catmurai_LOD")) continue;
            var src = Original(smr.sharedMesh);   // always rebuild from the untouched FBX mesh
            if (src == null) { Debug.LogWarning($"[TailFix] {smr.name}: original mesh not found"); continue; }
            var names = smr.bones.Select(b => b ? b.name : "").ToArray();
            int t5 = System.Array.IndexOf(names, "tail_05"), t6 = System.Array.IndexOf(names, "tail_06");
            if (t5 < 0) { Debug.LogWarning($"[TailFix] {smr.name}: no tail bones"); continue; }

            var mesh = Object.Instantiate(src); mesh.name = src.name + "_tailfix";
            var bpv = mesh.GetBonesPerVertex().ToArray();
            var all = mesh.GetAllBoneWeights().ToArray();
            int nv = mesh.vertexCount, off = 0, changed = 0;
            var newBpv = new byte[nv]; var newW = new List<BoneWeight1>(all.Length);
            var dom = new string[nv];
            for (int v = 0; v < nv; v++)
            {
                var ws = new Dictionary<int, float>();
                for (int k = 0; k < bpv[v]; k++) { var w = all[off + k]; if (w.weight > 0f) ws[w.boneIndex] = (ws.TryGetValue(w.boneIndex, out var o) ? o : 0f) + w.weight; }
                off += bpv[v];
                bool mod = false;
                // rule 2 (evaluated on the original weights)
                float ts = ws.Where(p => IsTail(names[p.Key])).Sum(p => p.Value);
                var other = ws.Where(p => !IsTail(names[p.Key])).ToList();
                if (ts > 0.05f && other.Count > 0)
                {
                    int domT = TailIdx(names[ws.Where(p => IsTail(names[p.Key])).OrderByDescending(p => p.Value).First().Key]);
                    string domO = names[other.OrderByDescending(p => p.Value).First().Key];
                    List<int> drop = null;
                    if (ts >= 0.5f || domT >= 3) drop = other.Select(p => p.Key).ToList();
                    else if (IsLegCape(domO)) drop = ws.Keys.Where(k => IsTail(names[k])).ToList();
                    if (drop != null) { foreach (var k in drop) ws.Remove(k); mod = true; }
                }
                // rule 1
                if (t6 >= 0 && ws.TryGetValue(t6, out var w6)) { ws.Remove(t6); ws[t5] = (ws.TryGetValue(t5, out var w5) ? w5 : 0f) + w6; mod = true; }
                // rule 4: cape vertices do not follow the legs, leg vertices do not follow the cape
                if (ws.Count > 1)
                {
                    string d0 = names[ws.OrderByDescending(p => p.Value).First().Key];
                    List<int> drop2 = null;
                    if (IsCape(d0)) drop2 = ws.Keys.Where(k => IsLeg(names[k])).ToList();
                    else if (IsLeg(d0)) drop2 = ws.Keys.Where(k => IsCape(names[k])).ToList();
                    if (drop2 != null && drop2.Count > 0) { foreach (var k in drop2) ws.Remove(k); mod = true; }
                }
                float sum = ws.Values.Sum();
                var list = ws.OrderByDescending(p => p.Value).Select(p => new BoneWeight1 { boneIndex = p.Key, weight = p.Value / sum }).ToList();
                if (list.Count == 0) list.Add(new BoneWeight1 { boneIndex = t5, weight = 1f });
                newBpv[v] = (byte)list.Count; newW.AddRange(list);
                dom[v] = names[list[0].boneIndex];
                if (mod) changed++;
            }
            using (var nb = new NativeArray<byte>(newBpv, Allocator.Temp))
            using (var nw = new NativeArray<BoneWeight1>(newW.ToArray(), Allocator.Temp))
                mesh.SetBoneWeights(nb, nw);
            // rule 3
            int removed = 0;
            for (int s = 0; s < mesh.subMeshCount; s++)
            {
                var tri = mesh.GetTriangles(s); var keep = new List<int>(tri.Length);
                for (int i = 0; i < tri.Length; i += 3)
                {
                    bool anyT = false, anyX = false, anyC = false, anyL = false;
                    for (int j = 0; j < 3; j++) { var d = dom[tri[i + j]]; anyT |= IsTail(d); anyX |= IsLegCape(d); anyC |= IsCape(d); anyL |= IsLeg(d); }
                    if ((anyT && anyX) || (anyC && anyL)) { removed++; continue; }
                    keep.Add(tri[i]); keep.Add(tri[i + 1]); keep.Add(tri[i + 2]);
                }
                mesh.SetTriangles(keep, s);
            }
            string path = $"{OutDir}/{mesh.name}.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(mesh, path);
            Undo.RecordObject(smr, "Tail fix");
            smr.sharedMesh = mesh;
            PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
            EditorSceneManager.MarkSceneDirty(smr.gameObject.scene);
            Debug.Log($"[TailFix] {smr.name}: {changed} vertices re-weighted, {removed} bridge triangles removed -> {path} (original: {AssetDatabase.GetAssetPath(src)})");
            done++;
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[TailFix] done ({done} meshes). Save the scene (Ctrl+S).");
    }

    [MenuItem("Catmurai/Tail Fix/Revert to Original Meshes")]
    static void Revert()
    {
        var originals = new Dictionary<string, Mesh>();
        foreach (var p in new[] { "Assets/Catmurai/Models/Catmurai_Drawn_ue.fbx", "Assets/Catmurai/Models/Catmurai_Base_LODs_ue.fbx" })
            foreach (var m in AssetDatabase.LoadAllAssetsAtPath(p).OfType<Mesh>())
                if (!originals.ContainsKey(p + m.name)) originals[p + m.name] = m;
        foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (smr.sharedMesh == null || !AssetDatabase.GetAssetPath(smr.sharedMesh).StartsWith(OutDir)) continue;
            string baseName = smr.sharedMesh.name.Replace("_tailfix", "");
            // LOD0 came from the Drawn model, LOD1-3 from the LOD file
            string file = baseName == "Catmurai_LOD0" ? "Assets/Catmurai/Models/Catmurai_Drawn_ue.fbx" : "Assets/Catmurai/Models/Catmurai_Base_LODs_ue.fbx";
            if (!originals.TryGetValue(file + baseName, out var orig)) { Debug.LogWarning($"[TailFix] original {baseName} not found"); continue; }
            Undo.RecordObject(smr, "Revert tail fix"); smr.sharedMesh = orig;
            PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
            EditorSceneManager.MarkSceneDirty(smr.gameObject.scene);
            Debug.Log($"[TailFix] {smr.name} reverted to {file}");
        }
    }
}

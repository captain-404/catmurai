using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Re-binds the LOD1-3 skinned meshes of Player > Model to the animated rig by BONE NAME.
// The LOD meshes come from Catmurai_Base_LODs_ue.fbx; if their bones[] array was copied from another
// renderer (different bone order), skinning is scrambled at runtime while the rest pose still looks fine.
public static class CatmuraiLodBoneFix
{
    const string LodFbx = "Assets/Catmurai/Models/Catmurai_Base_LODs_ue.fbx";

    [MenuItem("Catmurai/Check LOD Bones")]
    static void Check() { Run(false); }

    [MenuItem("Catmurai/Fix LOD Bones")]
    static void Fix() { Run(true); }

    static void Run(bool apply)
    {
        var src = AssetDatabase.LoadAllAssetsAtPath(LodFbx).OfType<GameObject>()
            .SelectMany(g => g.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            .GroupBy(r => r.sharedMesh).ToDictionary(g => g.Key, g => g.First());
        int fixedCount = 0;
        foreach (var smr in Object.FindObjectsByType<SkinnedMeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (smr.sharedMesh == null || !src.TryGetValue(smr.sharedMesh, out var reference)) continue;
            // All transforms of the rig this renderer should follow (the animated one under the same Model).
            Transform model = smr.transform.parent;
            var rig = model.GetComponentsInChildren<Transform>(true).GroupBy(t => t.name).ToDictionary(g => g.Key, g => g.First());
            var want = reference.bones.Select(b => b != null && rig.ContainsKey(b.name) ? rig[b.name] : null).ToArray();
            var have = smr.bones;
            int bad = 0;
            for (int i = 0; i < want.Length; i++)
                if (i >= have.Length || have[i] != want[i]) bad++;
            bool rootBad = reference.rootBone != null && (smr.rootBone == null || smr.rootBone.name != reference.rootBone.name || smr.rootBone != rig[reference.rootBone.name]);
            Debug.Log($"[Catmurai LOD] {smr.name}: bones {have.Length} vs {want.Length} expected, {bad} mismatched, rootBone {(smr.rootBone ? smr.rootBone.name : "none")}{(rootBad ? " (WRONG)" : "")}, missing names {want.Count(t => t == null)}");
            if (apply && (bad > 0 || rootBad) && want.All(t => t != null))
            {
                Undo.RecordObject(smr, "Fix LOD bones");
                smr.bones = want;
                if (reference.rootBone != null) smr.rootBone = rig[reference.rootBone.name];
                PrefabUtility.RecordPrefabInstancePropertyModifications(smr);
                EditorSceneManager.MarkSceneDirty(smr.gameObject.scene);
                fixedCount++;
            }
        }
        Debug.Log(apply ? $"[Catmurai LOD] fixed {fixedCount} renderer(s). Save the scene (Ctrl+S)." : "[Catmurai LOD] check done");
    }
}

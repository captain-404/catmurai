// Catmurai > Assign Project Materials To LODs
// Makes every Catmurai_LOD* renderer (and the katana meshes) use the project materials in
// Assets/Catmurai/Materials instead of the materials embedded in the FBX files.
// Added 2026-09-24 (v10 model): the LOD FBX had no material remap, so LOD1-3 used FBX-internal
// materials that picked up the wrong texture after the model was replaced.
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class CatmuraiMaterialFix
{
    const string Dir = "Assets/Catmurai/Materials/";

    [MenuItem("Catmurai/Assign Project Materials To LODs")]
    public static void Assign()
    {
        var game = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Catmurai_Game.mat");
        var m1 = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Material_1.mat");
        var m2 = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Material_2.mat");
        var kat = AssetDatabase.LoadAssetAtPath<Material>(Dir + "Catmurai_Katana.mat");
        if (!game || !m1 || !m2) { Debug.LogError("[Catmurai] project materials not found in " + Dir); return; }
        int n = 0;
        foreach (var r in Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.name.StartsWith("Catmurai_LOD"))
            {
                Undo.RecordObject(r, "Assign Catmurai materials");
                r.sharedMaterials = new[] { game, m1, m2 };
                EditorUtility.SetDirty(r); n++;
            }
            else if (r.name == "Katana_Saya")
            {
                // 2026-09-24: Lisvi Senpai chose to hide the sheath (the sword is always drawn)
                Undo.RecordObject(r.gameObject, "Hide sheath");
                r.gameObject.SetActive(false); n++;
            }
            else if (kat && r.name.StartsWith("Katana_"))
            {
                Undo.RecordObject(r, "Assign Catmurai materials");
                r.sharedMaterials = Enumerable.Repeat(kat, r.sharedMaterials.Length).ToArray();
                EditorUtility.SetDirty(r); n++;
            }
        }
        EditorSceneManager.MarkAllScenesDirty();
        EditorSceneManager.SaveOpenScenes();
        Debug.Log($"[Catmurai] Assigned project materials to {n} renderer(s) and saved the scene.");
    }
}

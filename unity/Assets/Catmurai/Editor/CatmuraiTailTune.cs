using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Applies calmer tail spring settings to every CatmuraiSpringBones in the open scene (the component is serialized
// in the scene, so changing the script defaults alone does not change it). Undo-able; save the scene afterwards.
public static class CatmuraiTailTune
{
    [MenuItem("Catmurai/Calm Tail Tip")]
    static void Apply()
    {
        int n = 0;
        foreach (var sb in Object.FindObjectsByType<CatmuraiSpringBones>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Undo.RecordObject(sb, "Calm tail tip");
            foreach (var g in sb.groups)
            {
                if (g.name != "Tail") continue;
                Debug.Log($"[Catmurai] {sb.name} tail before: stiff {g.stiffness} drag {g.drag} grav {g.gravity} max {g.maxAngle} root {g.rootInfluence} rigidTip {g.rigidTipBones} tipScale {g.tipAngleScale}");
                g.stiffness = 0.25f; g.drag = 0.20f; g.gravity = -1f; g.maxAngle = 20f;
                g.rigidTipBones = 2; g.tipAngleScale = 0.4f;
                n++;
            }
            PrefabUtility.RecordPrefabInstancePropertyModifications(sb);
            EditorSceneManager.MarkSceneDirty(sb.gameObject.scene);
        }
        Debug.Log($"[Catmurai] tail settings applied to {n} group(s). Save the scene (Ctrl+S).");
    }
}

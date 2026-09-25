#if UNITY_EDITOR
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-click setup: configures the Catmurai FBX imports (Generic rig, animation clips, URP materials),
/// builds the Animator controller (Idle / Run / Slash), a Player prefab and a test scene.
/// Menu: Catmurai / Build Playable Player.  Runs automatically once after the files are imported.
/// </summary>
public static class CatmuraiSetup
{
    const string Root = "Assets/Catmurai";
    const string ModelPath = Root + "/Models/Catmurai_Drawn_ue.fbx";
    const string ControllerPath = Root + "/Catmurai.controller";
    const string PrefabPath = Root + "/Player.prefab";
    static readonly string[] AnimNames = { "Idle", "Run", "Slash" };

    static string AnimPath(string n) { return Root + "/Anims/Anim_" + n + "_ue.fbx"; }

    [InitializeOnLoadMethod]
    static void AutoRun()
    {
        if (SessionState.GetBool("CatmuraiSetupTried", false)) return;
        if (!File.Exists(ModelPath) || File.Exists(PrefabPath)) return;
        SessionState.SetBool("CatmuraiSetupTried", true);
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Catmurai/Build Playable Player")]
    public static void Run()
    {
        try
        {
            AssetDatabase.Refresh();
            ConfigureModel();
            ConfigureAnimations();
            ExtractAndFixMaterials();
            var ctrl = BuildController();
            BuildScene(ctrl);
            Debug.Log("[Catmurai] Setup finished. Press Play: WASD move, Space / mouse click = slash.");
        }
        catch (System.Exception e)
        {
            Debug.LogError("[Catmurai] Setup failed: " + e);
        }
    }

    // ---------------------------------------------------------------- model
    static void ConfigureModel()
    {
        var mi = (ModelImporter)AssetImporter.GetAtPath(ModelPath);
        mi.animationType = ModelImporterAnimationType.Generic;
        mi.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
        mi.importAnimation = false;
        mi.bakeAxisConversion = true;
        mi.optimizeGameObjects = false;
        mi.importCameras = false;
        mi.importLights = false;
        mi.SaveAndReimport();
    }

    static Avatar ModelAvatar()
    {
        return AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Avatar>().FirstOrDefault();
    }

    static void ConfigureAnimations()
    {
        var avatar = ModelAvatar();
        foreach (var n in AnimNames)
        {
            string p = AnimPath(n);
            var ai = AssetImporter.GetAtPath(p) as ModelImporter;
            if (ai == null) { Debug.LogWarning("[Catmurai] missing " + p); continue; }
            ai.animationType = ModelImporterAnimationType.Generic;
            if (avatar != null)
            {
                ai.avatarSetup = ModelImporterAvatarSetup.CopyFromOther;
                ai.sourceAvatar = avatar;
            }
            ai.importAnimation = true;
            ai.bakeAxisConversion = true;
            ai.importBlendShapes = false;
            ai.importCameras = false;
            ai.importLights = false;
            ai.SaveAndReimport();

            ai = (ModelImporter)AssetImporter.GetAtPath(p);
            var clips = ai.defaultClipAnimations;
            if (clips.Length > 0)
            {
                var c = clips[0];
                c.name = n;
                bool loop = n != "Slash";
                c.loopTime = loop;
                c.loopPose = loop;
                ai.clipAnimations = new[] { c };
                ai.SaveAndReimport();
            }
        }
    }

    // ---------------------------------------------------------------- materials
    static void ExtractAndFixMaterials()
    {
        string dir = Root + "/Materials";
        Directory.CreateDirectory(dir);
        var mats = AssetDatabase.LoadAllAssetsAtPath(ModelPath).OfType<Material>().ToArray();
        foreach (var m in mats)
        {
            string np = dir + "/" + m.name + ".mat";
            if (!File.Exists(np))
            {
                string err = AssetDatabase.ExtractAsset(m, np);
                if (!string.IsNullOrEmpty(err)) Debug.LogWarning("[Catmurai] extract " + m.name + ": " + err);
            }
        }
        AssetDatabase.WriteImportSettingsIfDirty(ModelPath);
        AssetDatabase.ImportAsset(ModelPath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();

        var urp = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { dir }))
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
            if (mat == null || urp == null) continue;
            if (mat.shader != urp)
            {
                Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color col = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                Texture emis = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
                mat.shader = urp;
                if (tex) mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", col);
                if (emis) { mat.SetTexture("_EmissionMap", emis); mat.SetColor("_EmissionColor", Color.white); mat.EnableKeyword("_EMISSION"); }
            }
            mat.SetColor("_BaseColor", Color.white);
            string texName = null;
            switch (mat.name)
            {
                case "Catmurai_Game": texName = "T_Catmurai_Body_BaseColor_2048.jpg"; break;
                case "Material_1": texName = "Cat_Image_1.png"; break;
                case "Material_2": texName = "Cat_Image_2.png"; break;
                case "Catmurai_Katana": texName = "catmurai_katana_basecolor.png"; break;
            }
            if (mat.name == "Catmurai_Game")   // prefer the newest body bake that exists (v7 4K > 4K > 2K)
            {
                foreach (var cand in new[] { "T_Catmurai_Body_BaseColor_4096_v10.jpg", "T_Catmurai_Body_BaseColor_4096_v7.jpg", "T_Catmurai_Body_BaseColor_4096.jpg", "T_Catmurai_Body_BaseColor_2048.jpg" })
                    if (AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + cand) != null) { texName = cand; break; }
            }
            if (texName != null)
            {
                var bt = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/" + texName);
                if (bt) mat.SetTexture("_BaseMap", bt); else Debug.LogWarning("[Catmurai] texture missing " + texName);
            }
            if (mat.name == "Catmurai_Katana")
            {
                var et = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/catmurai_katana_emissive.png");
                if (et)
                {
                    mat.SetTexture("_EmissionMap", et);
                    mat.SetColor("_EmissionColor", Color.white);
                    mat.EnableKeyword("_EMISSION");
                    mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
                }
            }
            if (mat.name == "Material_1")   // cape: double-sided alpha cutout
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetFloat("_Cull", 0f);
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.5f);
                mat.EnableKeyword("_ALPHATEST_ON");
                mat.SetOverrideTag("RenderType", "TransparentCutout");
                mat.renderQueue = 2450;
            }
            mat.SetFloat("_Smoothness", 0.2f);
            mat.SetFloat("_Metallic", 0f);
            // katana: metallic/smoothness map
            if (mat.name.Contains("Katana"))
            {
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(Root + "/Textures/katana_mask_unity.png");
                if (mask)
                {
                    mat.SetTexture("_MetallicGlossMap", mask);
                    mat.EnableKeyword("_METALLICSPECGLOSSMAP");
                    mat.SetFloat("_Metallic", 1f);
                    mat.SetFloat("_Smoothness", 1f);
                }
            }
            EditorUtility.SetDirty(mat);
        }
        AssetDatabase.SaveAssets();
    }

    // ---------------------------------------------------------------- animator
    static AnimationClip FindClip(string n)
    {
        return AssetDatabase.LoadAllAssetsAtPath(AnimPath(n)).OfType<AnimationClip>()
            .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
    }

    static AnimatorController BuildController()
    {
        var ac = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (ac == null)
        {
            ac = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
        }
        else
        {
            while (ac.parameters.Length > 0) ac.RemoveParameter(0);
        }
        ac.AddParameter("Speed", AnimatorControllerParameterType.Float);
        ac.AddParameter("Slash", AnimatorControllerParameterType.Trigger);
        var sm = ac.layers[0].stateMachine;
        foreach (var cs in sm.states.ToArray()) sm.RemoveState(cs.state);
        foreach (var at in sm.anyStateTransitions.ToArray()) sm.RemoveAnyStateTransition(at);

        var idle = sm.AddState("Idle"); idle.motion = FindClip("Idle");
        var run = sm.AddState("Run"); run.motion = FindClip("Run");
        var slash = sm.AddState("Slash"); slash.motion = FindClip("Slash");
        sm.defaultState = idle;

        var t = idle.AddTransition(run);
        t.hasExitTime = false; t.duration = 0.12f;
        t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");

        t = run.AddTransition(idle);
        t.hasExitTime = false; t.duration = 0.12f;
        t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");

        var any = sm.AddAnyStateTransition(slash);
        any.hasExitTime = false; any.duration = 0.06f; any.canTransitionToSelf = false;
        any.AddCondition(AnimatorConditionMode.If, 0f, "Slash");

        t = slash.AddTransition(idle);
        t.hasExitTime = true; t.exitTime = 0.92f; t.duration = 0.12f;

        AssetDatabase.SaveAssets();
        return ac;
    }

    // ---------------------------------------------------------------- scene
    static void BuildScene(AnimatorController ctrl)
    {
        var scene = SceneManager.GetActiveScene();
        // remove previous build
        foreach (var n in new[] { "Player", "Ground", "TrainingPost" })
            foreach (var go in scene.GetRootGameObjects().Where(g => g.name.StartsWith(n)).ToArray())
                Object.DestroyImmediate(go);

        // ground
        var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(4f, 1f, 4f);
        var gm = MakeCheckerMaterial();
        ground.GetComponent<Renderer>().sharedMaterial = gm;

        // player
        var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath);
        var player = new GameObject("Player");
        var cc = player.AddComponent<CharacterController>();
        cc.height = 0.98f; cc.radius = 0.22f; cc.center = new Vector3(0f, 0.49f, 0f); cc.stepOffset = 0.15f; cc.skinWidth = 0.02f;

        var vis = (GameObject)PrefabUtility.InstantiatePrefab(model);
        vis.name = "Model";
        vis.transform.SetParent(player.transform, false);
        var anim = vis.GetComponent<Animator>();
        if (anim == null) anim = vis.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        foreach (var smr in vis.GetComponentsInChildren<SkinnedMeshRenderer>()) smr.updateWhenOffscreen = true;
        PrefabUtility.RecordPrefabInstancePropertyModifications(anim);
        anim.avatar = ModelAvatar();
        anim.applyRootMotion = false;
        anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        var ctl = player.AddComponent<CatmuraiPlayerController>();
        ctl.animator = anim;
        ctl.model = vis.transform;
        ctl.modelYawOffset = 180f; // imported model faces -Z; controller forward is +Z

        // save prefab (keeps the scene instance linked)
        var prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(player, PrefabPath, InteractionMode.AutomatedAction);

        // camera
        var cam = Camera.main;
        if (cam != null)
        {
            var fc = cam.GetComponent<CatmuraiFollowCamera>();
            if (fc == null) fc = cam.gameObject.AddComponent<CatmuraiFollowCamera>();
            fc.target = player.transform;
            cam.transform.position = new Vector3(0f, 2.4f, -3.4f);
            cam.transform.rotation = Quaternion.Euler(35f, 0f, 0f);
        }

        // a few posts to swing at
        for (int i = 0; i < 4; i++)
        {
            var post = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            post.name = "TrainingPost" + i;
            post.transform.position = new Vector3(-1.5f + i * 1.0f, 0.5f, 2.2f + (i % 2) * 0.6f);
            post.transform.localScale = new Vector3(0.25f, 0.5f, 0.25f);
            post.GetComponent<Renderer>().sharedMaterial = MakeFlatMaterial("Post", new Color(0.55f, 0.35f, 0.2f));
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = player;
    }

    static Material MakeFlatMaterial(string name, Color c)
    {
        string p = Root + "/Materials/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.SetColor("_BaseColor", c);
        m.SetFloat("_Smoothness", 0.1f);
        EditorUtility.SetDirty(m);
        return m;
    }

    static Material MakeCheckerMaterial()
    {
        string tp = Root + "/Textures/Checker.png";
        if (!File.Exists(tp))
        {
            var t = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            for (int y = 0; y < 64; y++)
                for (int x = 0; x < 64; x++)
                {
                    bool a = ((x / 32) + (y / 32)) % 2 == 0;
                    t.SetPixel(x, y, a ? new Color(0.30f, 0.36f, 0.30f) : new Color(0.24f, 0.30f, 0.24f));
                }
            File.WriteAllBytes(tp, t.EncodeToPNG());
            AssetDatabase.ImportAsset(tp);
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(tp);
        string p = Root + "/Materials/Ground.mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(m, p);
        }
        m.SetTexture("_BaseMap", tex);
        m.SetTextureScale("_BaseMap", new Vector2(20f, 20f));
        m.SetFloat("_Smoothness", 0.05f);
        EditorUtility.SetDirty(m);
        return m;
    }
}
#endif

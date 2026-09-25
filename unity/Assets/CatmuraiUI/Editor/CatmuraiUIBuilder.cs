#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Builds the Catmurai game UI (main menu, HUD, pause, options, how-to-play) in the style of the
/// "Catmurai - Path of Shadows" reference sheet: brush fonts, cream text with violet glow, dark violet panels.
/// Menu: Catmurai / UI / Build Game UI. Output: Assets/CatmuraiUI/CatmuraiUI.prefab, placed in the Sky Realm scene.
/// </summary>
public static class CatmuraiUIBuilder
{
    const string Root = "Assets/CatmuraiUI";
    const string ArtDir = Root + "/Art";
    const string FontDir = Root + "/Fonts";
    const string FontOut = Root + "/FontAssets";
    const string PrefabPath = Root + "/CatmuraiUI.prefab";
    const string SkyRealmScene = "Assets/SkyRealm/SkyRealm.unity";

    static readonly Color Cream = new Color(0.95f, 0.90f, 0.82f);
    static readonly Color CreamTop = new Color(1f, 0.96f, 0.88f);
    static readonly Color LavBottom = new Color(0.84f, 0.70f, 0.96f);
    static readonly Color Lav = new Color(0.72f, 0.62f, 0.90f);
    static readonly Color Violet = new Color(0.75f, 0.48f, 1f);
    static readonly Color InkText = new Color(0.17f, 0.08f, 0.16f);

    static TMP_FontAsset fTitle, fBrush, fCaps, fBody;
    static Material mTitle, mBrush, mBrushInk, mCaps, mBody;

    static void Log(string m) { Debug.Log("[CatmuraiUI] " + m); }

    // ------------------------------------------------------------------ entry points
    [MenuItem("Catmurai/UI/Build Game UI")]
    public static void BuildMenu()
    {
        if (!EnsureTmpEssentials()) return;
        try
        {
            AssetDatabase.Refresh();
            ConfigureSprites();
            if (!MakeFonts()) return;
            if (!OpenSkyRealm()) return;
            var ui = BuildHierarchy();
            Directory.CreateDirectory(Root);
            PrefabUtility.SaveAsPrefabAssetAndConnect(ui, PrefabPath, InteractionMode.AutomatedAction);
            EnsureEventSystem();
            var scene = EditorSceneManager.GetActiveScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Selection.activeGameObject = ui;
            Log("Game UI built: " + PrefabPath + " (placed in " + scene.path + "). Press Play.");
        }
        catch (Exception e) { Debug.LogError("[CatmuraiUI] Build failed: " + e); }
    }

    /// Instantiates the saved UI prefab (+ EventSystem) into the open scene. Used by the Sky Realm world builder.
    public static void AddToOpenScene()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab) return;
        var old = GameObject.Find("CatmuraiUI");
        if (old) Object.DestroyImmediate(old);
        PrefabUtility.InstantiatePrefab(prefab);
        EnsureEventSystem();
    }

    // ------------------------------------------------------------------ TMP essentials
    static bool tmpImportHooked;

    static bool EnsureTmpEssentials()
    {
        if (AssetDatabase.FindAssets("t:TMP_Settings").Length > 0) return true;
        Type t = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("TMPro.TMP_PackageResourceImporter")).FirstOrDefault(x => x != null);
        var m = t?.GetMethod("ImportResources", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(bool), typeof(bool), typeof(bool) }, null);
        if (m == null)
        {
            Debug.LogError("[CatmuraiUI] TextMesh Pro essentials are missing. Use Window > TextMeshPro > Import TMP Essential Resources, then run Catmurai > UI > Build Game UI again.");
            return false;
        }
        if (!tmpImportHooked)
        {
            tmpImportHooked = true;
            AssetDatabase.importPackageCompleted += name =>
            {
                Log("Imported package '" + name + "', continuing the UI build...");
                EditorApplication.delayCall += BuildMenu;
            };
        }
        Log("Importing TextMesh Pro essentials first (the build continues automatically when the import finishes)...");
        m.Invoke(null, new object[] { true, false, false });
        return false;
    }

    // ------------------------------------------------------------------ sprites
    static Vector4 BorderFor(string n)
    {
        switch (n)
        {
            case "ui_panel": return new Vector4(56, 56, 56, 56);
            case "ui_button":
            case "ui_button_glow": return new Vector4(26, 26, 26, 26);
            case "ui_toast_violet":
            case "ui_toast_parchment": return new Vector4(64, 40, 64, 40);
            case "ui_divider": return new Vector4(40, 0, 40, 0);
            case "ui_bar_bg":
            case "ui_bar_fill": return new Vector4(12, 12, 12, 12);
            case "ui_track":
            case "ui_track_fill": return new Vector4(8, 6, 8, 6);
            case "ui_toggle_box": return new Vector4(16, 16, 16, 16);
            default: return Vector4.zero;
        }
    }

    static void ConfigureSprites()
    {
        foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { ArtDir }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var ti = AssetImporter.GetAtPath(path) as TextureImporter;
            if (ti == null) continue;
            string n = Path.GetFileNameWithoutExtension(path);
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 2048;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.spriteBorder = BorderFor(n);
            ti.SaveAndReimport();
        }
    }

    static Sprite Spr(string n)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(ArtDir + "/" + n + ".png");
        if (!s) Debug.LogWarning("[CatmuraiUI] sprite missing: " + n);
        return s;
    }

    // ------------------------------------------------------------------ fonts
    const string Glyphs = " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~·•…—–’‘“”×÷";

    static TMP_FontAsset FontAsset(string ttf, string name, int sampling, int padding, int atlas)
    {
        Directory.CreateDirectory(FontOut);
        string outPath = FontOut + "/" + name + ".asset";
        var fa = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outPath);
        if (fa) return fa;
        var font = AssetDatabase.LoadAssetAtPath<Font>(FontDir + "/" + ttf);
        if (!font) { Debug.LogError("[CatmuraiUI] font missing: " + ttf); return null; }
        fa = TMP_FontAsset.CreateFontAsset(font, sampling, padding, GlyphRenderMode.SDFAA, atlas, atlas, AtlasPopulationMode.Dynamic, false);
        if (!fa) { Debug.LogError("[CatmuraiUI] could not create font asset for " + ttf); return null; }
        fa.name = name;
        AssetDatabase.CreateAsset(fa, outPath);
        fa.atlasTextures[0].name = name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        fa.TryAddCharacters(Glyphs);
        EditorUtility.SetDirty(fa);
        AssetDatabase.SaveAssets();
        Log("Font asset created: " + outPath);
        return fa;
    }

    static Material Preset(TMP_FontAsset fa, string name, Color outline, float outlineW, Color underlay, float dilate, float soft, float offY)
    {
        string p = FontOut + "/" + name + ".mat";
        var m = AssetDatabase.LoadAssetAtPath<Material>(p);
        if (m == null)
        {
            m = new Material(fa.material);
            AssetDatabase.CreateAsset(m, p);
        }
        else
        {
            m.shader = fa.material.shader;
            m.CopyPropertiesFromMaterial(fa.material);
        }
        m.name = name;
        m.SetFloat("_OutlineWidth", outlineW);
        m.SetColor("_OutlineColor", outline);
        if (underlay.a > 0f)
        {
            m.EnableKeyword("UNDERLAY_ON");
            m.SetColor("_UnderlayColor", underlay);
            m.SetFloat("_UnderlayDilate", dilate);
            m.SetFloat("_UnderlaySoftness", soft);
            m.SetFloat("_UnderlayOffsetX", 0f);
            m.SetFloat("_UnderlayOffsetY", offY);
        }
        else m.DisableKeyword("UNDERLAY_ON");
        EditorUtility.SetDirty(m);
        return m;
    }

    static bool MakeFonts()
    {
        fTitle = FontAsset("Knewave-Regular.ttf", "Catmurai_Title SDF", 90, 14, 1024);
        fBrush = FontAsset("KaushanScript-Regular.ttf", "Catmurai_Brush SDF", 90, 14, 1024);
        fCaps = FontAsset("Cinzel-Variable.ttf", "Catmurai_Caps SDF", 72, 10, 1024);
        fBody = FontAsset("CormorantGaramond-Variable.ttf", "Catmurai_Body SDF", 72, 9, 1024);
        if (!fTitle || !fBrush || !fCaps || !fBody) return false;
        var deep = new Color(0.12f, 0.03f, 0.22f, 1f);
        mTitle = Preset(fTitle, "Catmurai_Title Glow", deep, 0.12f, new Color(0.62f, 0.25f, 1f, 0.85f), 0.45f, 0.75f, -0.2f);
        mBrush = Preset(fBrush, "Catmurai_Brush Glow", deep, 0.06f, new Color(0.58f, 0.22f, 1f, 0.7f), 0.3f, 0.7f, -0.1f);
        mBrushInk = Preset(fBrush, "Catmurai_Brush Ink", new Color(0, 0, 0, 0), 0f, new Color(0.35f, 0.15f, 0.1f, 0.35f), 0.1f, 0.4f, -0.4f);
        mCaps = Preset(fCaps, "Catmurai_Caps Glow", deep, 0.05f, new Color(0.55f, 0.25f, 0.95f, 0.55f), 0.2f, 0.6f, 0f);
        mBody = Preset(fBody, "Catmurai_Body Soft", deep, 0.03f, new Color(0f, 0f, 0f, 0.6f), 0.1f, 0.4f, -0.3f);
        AssetDatabase.SaveAssets();
        return true;
    }

    // ------------------------------------------------------------------ scene
    static bool OpenSkyRealm()
    {
        var active = EditorSceneManager.GetActiveScene();
        if (active.path == SkyRealmScene) return true;
        if (!File.Exists(SkyRealmScene)) { Log("Sky Realm scene not found; building into the open scene " + active.path); return true; }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
        EditorSceneManager.OpenScene(SkyRealmScene, OpenSceneMode.Single);
        return true;
    }

    static void EnsureEventSystem()
    {
        var es = Object.FindAnyObjectByType<EventSystem>();
        if (es)
        {
            if (!es.GetComponent<InputSystemUIInputModule>())
            {
                var old = es.GetComponent<StandaloneInputModule>();
                if (old) Object.DestroyImmediate(old);
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
            return;
        }
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    // ------------------------------------------------------------------ UI helpers
    static RectTransform NewRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = 5; // UI
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    static RectTransform Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = pivot;
        rt.sizeDelta = size; rt.anchoredPosition = pos;
        return rt;
    }

    static readonly Vector2 TL = new Vector2(0f, 1f), TC = new Vector2(0.5f, 1f), TR = new Vector2(1f, 1f);
    static readonly Vector2 ML = new Vector2(0f, 0.5f), MC = new Vector2(0.5f, 0.5f), MR = new Vector2(1f, 0.5f);
    static readonly Vector2 BL = new Vector2(0f, 0f), BC = new Vector2(0.5f, 0f), BR = new Vector2(1f, 0f);

    static RectTransform Stretch(RectTransform rt, float l = 0, float b = 0, float r = 0, float t = 0)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = MC;
        rt.offsetMin = new Vector2(l, b); rt.offsetMax = new Vector2(-r, -t);
        return rt;
    }

    static UnityEngine.UI.Image Img(string name, Transform parent, string sprite, Color col, bool raycast = false)
    {
        var rt = NewRect(name, parent);
        var im = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        im.sprite = sprite != null ? Spr(sprite) : null;
        im.color = col;
        im.raycastTarget = raycast;
        if (im.sprite && im.sprite.border.sqrMagnitude > 0f) im.type = UnityEngine.UI.Image.Type.Sliced;
        return im;
    }

    static TextMeshProUGUI Txt(string name, Transform parent, string text, TMP_FontAsset font, Material mat, float size, Color col, TextAlignmentOptions align, float spacing = 0f)
    {
        var rt = NewRect(name, parent);
        var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
        t.font = font;
        if (mat) t.fontSharedMaterial = mat;
        t.text = text;
        t.fontSize = size;
        t.color = col;
        t.alignment = align;
        t.characterSpacing = spacing;
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    static void ApplyGradient(TMP_Text t, Color top, Color bottom)
    {
        t.color = Color.white;
        t.enableVertexGradient = true;
        t.colorGradient = new VertexGradient(top, top, bottom, bottom);
    }

    static CanvasGroup MakeScreen(string name, Transform parent)
    {
        var rt = Stretch(NewRect(name, parent));
        return rt.gameObject.AddComponent<CanvasGroup>();
    }

    static void Dim(Transform parent, float alpha)
    {
        var d = Img("Dim", parent, null, new Color(0.03f, 0.02f, 0.07f, alpha), true);
        Stretch(d.rectTransform);
        var v = Img("Vignette", parent, "ui_vignette", Color.white);
        Stretch(v.rectTransform);
    }

    static UnityEngine.UI.Button MenuButton(string name, Transform parent, string label, Vector2 size, float fontSize)
    {
        var bg = Img(name, parent, "ui_button", Color.white, true);
        bg.rectTransform.sizeDelta = size;
        var le = bg.gameObject.AddComponent<LayoutElement>();
        le.preferredWidth = size.x; le.preferredHeight = size.y;
        var glow = Img("Glow", bg.transform, "ui_button_glow", new Color(1f, 1f, 1f, 0f));
        Stretch(glow.rectTransform, -6, -6, -6, -6);
        var icon = Img("Paw", bg.transform, "icon_paw_violet", new Color(1f, 1f, 1f, 0f));
        Place(icon.rectTransform, ML, ML, new Vector2(18f, 0f), new Vector2(38f, 38f));
        var t = Txt("Label", bg.transform, label, fBrush, mBrush, fontSize, Cream, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 46, 2, 20, 2);
        var b = bg.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = bg;
        b.transition = Selectable.Transition.None;
        var fx = bg.gameObject.AddComponent<UIButtonFX>();
        fx.glow = glow; fx.icon = icon; fx.label = t;
        return b;
    }

    static RectTransform Column(string name, Transform parent, Vector2 pos, Vector2 size, float spacing)
    {
        var rt = Place(NewRect(name, parent), TL, TL, pos, size);
        var vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true; vlg.childControlHeight = true;
        vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        return rt;
    }

    static RectTransform Row(Transform parent, string label, float height = 54f)
    {
        var rt = NewRect("Row_" + label.Replace(" ", ""), parent);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = height; le.preferredHeight = height;
        if (!string.IsNullOrEmpty(label))
        {
            var t = Txt("Label", rt, label, fBrush, mBrush, 28f, Cream, TextAlignmentOptions.MidlineLeft);
            Place(t.rectTransform, ML, ML, Vector2.zero, new Vector2(210f, height));
        }
        return rt;
    }

    static void Section(Transform parent, string title)
    {
        var rt = NewRect("Section_" + title, parent);
        var le = rt.gameObject.AddComponent<LayoutElement>();
        le.minHeight = 56f; le.preferredHeight = 56f;
        var t = Txt("Title", rt, title, fCaps, mCaps, 22f, Lav, TextAlignmentOptions.BottomLeft, 12f);
        Place(t.rectTransform, TL, TL, new Vector2(0f, -4f), new Vector2(520f, 30f));
        var d = Img("Divider", rt, "ui_divider", new Color(1f, 1f, 1f, 0.9f));
        Place(d.rectTransform, BL, BL, new Vector2(-8f, 2f), new Vector2(530f, 20f));
    }

    static UnityEngine.UI.Slider SliderRow(Transform parent, string label, string name, float min, float max)
    {
        var row = Row(parent, label);
        var root = Place(NewRect(name, row), ML, ML, new Vector2(215f, 0f), new Vector2(210f, 18f));
        var bg = Img("Background", root, "ui_track", Color.white);
        Stretch(bg.rectTransform);
        var fillArea = Stretch(NewRect("Fill Area", root), 4, 3, 4, 3);
        var fill = Img("Fill", fillArea, "ui_track_fill", Violet);
        fill.rectTransform.anchorMin = Vector2.zero; fill.rectTransform.anchorMax = new Vector2(0f, 1f);
        fill.rectTransform.offsetMin = Vector2.zero; fill.rectTransform.offsetMax = Vector2.zero;
        var handleArea = Stretch(NewRect("Handle Slide Area", root), 10, 0, 10, 0);
        var handle = Img("Handle", handleArea, "ui_handle", Color.white, true);
        handle.rectTransform.sizeDelta = new Vector2(34f, 16f);
        var s = root.gameObject.AddComponent<UnityEngine.UI.Slider>();
        s.fillRect = fill.rectTransform;
        s.handleRect = handle.rectTransform;
        s.targetGraphic = handle;
        s.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        s.minValue = min; s.maxValue = max; s.value = min;
        var cb = s.colors; cb.highlightedColor = new Color(0.9f, 0.8f, 1f); cb.selectedColor = new Color(0.9f, 0.8f, 1f); cb.pressedColor = new Color(0.75f, 0.6f, 1f); s.colors = cb;
        var val = Txt(name + "_Value", row, "", fCaps, mCaps, 19f, Lav, TextAlignmentOptions.MidlineLeft);
        Place(val.rectTransform, ML, ML, new Vector2(440f, 0f), new Vector2(100f, 40f));
        return s;
    }

    static UnityEngine.UI.Toggle ToggleRow(Transform parent, string label, string name)
    {
        var row = Row(parent, label);
        var root = Place(NewRect(name, row), ML, ML, new Vector2(215f, 0f), new Vector2(40f, 40f));
        var box = Img("Box", root, "ui_toggle_box", Color.white, true);
        Stretch(box.rectTransform);
        var check = Img("Check", box.transform, "ui_toggle_check", Color.white);
        Stretch(check.rectTransform, 5, 5, 5, 5);
        var tg = root.gameObject.AddComponent<UnityEngine.UI.Toggle>();
        tg.targetGraphic = box; tg.graphic = check; tg.isOn = true;
        var cb = tg.colors; cb.highlightedColor = new Color(0.9f, 0.8f, 1f); cb.selectedColor = new Color(0.9f, 0.8f, 1f); tg.colors = cb;
        return tg;
    }

    static UnityEngine.UI.Button Arrow(string name, Transform parent, string sprite, Vector2 anchor)
    {
        var im = Img(name, parent, sprite, Color.white, true);
        Place(im.rectTransform, anchor, anchor, Vector2.zero, new Vector2(36f, 36f));
        var b = im.gameObject.AddComponent<UnityEngine.UI.Button>();
        b.targetGraphic = im;
        var cb = b.colors;
        cb.normalColor = Cream; cb.highlightedColor = Violet; cb.selectedColor = Violet; cb.pressedColor = new Color(0.55f, 0.35f, 0.85f);
        b.colors = cb;
        return b;
    }

    static UISelector SelectorRow(Transform parent, string label, string name)
    {
        var row = Row(parent, label);
        var root = Place(NewRect(name, row), ML, ML, new Vector2(210f, 0f), new Vector2(320f, 44f));
        var l = Arrow("Left", root, "ui_arrow_left", ML);
        var r = Arrow("Right", root, "ui_arrow_right", MR);
        var t = Txt("Value", root, "-", fBrush, mBrush, 26f, Cream, TextAlignmentOptions.Center);
        Stretch(t.rectTransform, 40, 0, 40, 0);
        var sel = root.gameObject.AddComponent<UISelector>();
        sel.left = l; sel.right = r; sel.label = t;
        return sel;
    }

    static RectTransform Panel(Transform parent, Vector2 size)
    {
        var p = Img("Panel", parent, "ui_panel", Color.white, true);
        Place(p.rectTransform, MC, MC, Vector2.zero, size);
        return p.rectTransform;
    }

    static void PanelTitle(RectTransform panel, string title, float size, float width)
    {
        var t = Txt("Title", panel, title, fTitle, mTitle, size, Color.white, TextAlignmentOptions.Center, 4f);
        ApplyGradient(t, CreamTop, LavBottom);
        Place(t.rectTransform, TC, TC, new Vector2(0f, -30f), new Vector2(width, size * 1.25f));
        foreach (float sx in new[] { -1f, 1f })
        {
            var paw = Img("TitlePaw", panel, "icon_paw_violet", Color.white);
            Place(paw.rectTransform, TC, MC, new Vector2(sx * (width * 0.5f + 10f), -30f - size * 0.62f), new Vector2(40f, 40f));
        }
        var d = Img("TitleDivider", panel, "ui_divider", Color.white);
        Place(d.rectTransform, TC, TC, new Vector2(0f, -40f - size * 1.25f), new Vector2(Mathf.Min(900f, panel.sizeDelta.x - 160f), 24f));
    }

    // ------------------------------------------------------------------ hierarchy
    static GameObject BuildHierarchy()
    {
        var old = GameObject.Find("CatmuraiUI");
        if (old) Object.DestroyImmediate(old);

        var root = new GameObject("CatmuraiUI", typeof(RectTransform));
        root.layer = 5;
        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        root.AddComponent<GraphicRaycaster>();
        root.AddComponent<CatmuraiUIController>();

        BuildHud(root.transform);
        BuildMain(root.transform);
        BuildPause(root.transform);
        BuildSettings(root.transform);
        BuildHowTo(root.transform);
        return root;
    }

    static void BuildMain(Transform root)
    {
        var s = MakeScreen("Screen_Main", root).transform;
        var fadeL = Img("FadeLeft", s, "ui_fade_left", Color.white);
        fadeL.rectTransform.anchorMin = Vector2.zero; fadeL.rectTransform.anchorMax = new Vector2(0.66f, 1f);
        fadeL.rectTransform.offsetMin = Vector2.zero; fadeL.rectTransform.offsetMax = Vector2.zero;
        Stretch(Img("Vignette", s, "ui_vignette", new Color(1f, 1f, 1f, 0.85f)).rectTransform);
        var fadeB = Img("FadeBottom", s, "ui_fade_bottom", Color.white);
        fadeB.rectTransform.anchorMin = Vector2.zero; fadeB.rectTransform.anchorMax = new Vector2(1f, 0f);
        fadeB.rectTransform.pivot = BC; fadeB.rectTransform.sizeDelta = new Vector2(0f, 300f); fadeB.rectTransform.anchoredPosition = Vector2.zero;

        Place(Img("Kanji", s, "ui_kanji", Color.white).rectTransform, TL, TL, new Vector2(30f, -30f), new Vector2(66f, 128f));
        var motto = Txt("Motto", s, "CATS\nCODE\nCOURAGE\nBEYOND", fCaps, mCaps, 15f, Lav, TextAlignmentOptions.TopLeft, 8f);
        Place(motto.rectTransform, TL, TL, new Vector2(34f, -178f), new Vector2(170f, 110f));

        Place(Img("Logo", s, "ui_logo", Color.white).rectTransform, TL, TL, new Vector2(110f, -20f), new Vector2(980f, 448f));

        // START (art cut from the reference sheet)
        var start = Img("Btn_Start", s, "ui_start_button", Color.white, true);
        start.preserveAspect = true;
        Place(start.rectTransform, TL, MC, new Vector2(420f, -640f), new Vector2(560f, 314f));
        var sb = start.gameObject.AddComponent<UnityEngine.UI.Button>();
        sb.targetGraphic = start;
        sb.transition = Selectable.Transition.None;
        var sfx = start.gameObject.AddComponent<UIButtonFX>();
        sfx.hoverScale = 1.05f;
        var cap = Txt("StartCaption", s, "A NEW JOURNEY AWAITS", fCaps, mCaps, 19f, Lav, TextAlignmentOptions.Center, 14f);
        Place(cap.rectTransform, TL, MC, new Vector2(420f, -815f), new Vector2(600f, 40f));

        var col = Column("MenuButtons", s, new Vector2(740f, -540f), new Vector2(320f, 250f), 18f);
        MenuButton("Btn_HowTo", col, "How to Play", new Vector2(300f, 68f), 32f);
        MenuButton("Btn_Options", col, "Options", new Vector2(300f, 68f), 32f);
        MenuButton("Btn_Quit", col, "Quit", new Vector2(300f, 68f), 32f);

        var quote = Txt("Quote", s, "A QUIET\nPAW\nSHAPES\nA BRIGHTER\nTOMORROW.", fCaps, mCaps, 16f, Lav, TextAlignmentOptions.TopRight, 6f);
        Place(quote.rectTransform, TR, TR, new Vector2(-40f, -40f), new Vector2(260f, 150f));
        Place(Img("Seal", s, "ui_seal", Color.white).rectTransform, TR, TR, new Vector2(-40f, -200f), new Vector2(54f, 54f));

        var fl = Txt("FooterLeft", s, "CATMURAI   ·   PATH OF SHADOWS", fCaps, mCaps, 17f, Lav, TextAlignmentOptions.BottomLeft, 10f);
        Place(fl.rectTransform, BL, BL, new Vector2(40f, 28f), new Vector2(760f, 36f));
        Place(Img("FooterOrnament", s, "ui_footer_ornament", Color.white).rectTransform, BC, BC, new Vector2(0f, 22f), new Vector2(200f, 50f));
        var fr = Txt("FooterRight", s, "CATS  ·  CODE  ·  COURAGE  ·  BEYOND", fCaps, mCaps, 17f, Lav, TextAlignmentOptions.BottomRight, 10f);
        Place(fr.rectTransform, BR, BR, new Vector2(-40f, 28f), new Vector2(760f, 36f));
    }

    static void BuildHud(Transform root)
    {
        var s = MakeScreen("Screen_HUD", root).transform;

        var pp = Place(NewRect("PlayerPanel", s), TL, TL, new Vector2(28f, -24f), new Vector2(620f, 130f));
        Place(Img("Emblem", pp, "icon_emblem", Color.white).rectTransform, TL, TL, Vector2.zero, new Vector2(116f, 116f));
        var name = Txt("Name", pp, "CATMURAI", fCaps, mCaps, 26f, Cream, TextAlignmentOptions.TopLeft, 10f);
        Place(name.rectTransform, TL, TL, new Vector2(128f, -8f), new Vector2(400f, 36f));
        var barBg = Img("HP_Bar", pp, "ui_bar_bg", Color.white);
        Place(barBg.rectTransform, TL, TL, new Vector2(128f, -50f), new Vector2(340f, 30f));
        var fill = Img("HP_Fill", barBg.transform, "ui_bar_fill", Violet);
        fill.type = UnityEngine.UI.Image.Type.Filled;
        fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
        fill.fillOrigin = 0;
        fill.fillAmount = 1f;
        Stretch(fill.rectTransform, 3, 3, 3, 3);
        var hp = Txt("HP_Text", pp, "30 / 30", fBrush, mBrush, 26f, Cream, TextAlignmentOptions.MidlineLeft);
        Place(hp.rectTransform, TL, TL, new Vector2(482f, -44f), new Vector2(130f, 40f));
        var sub = Txt("Subtitle", pp, "PATH OF SHADOWS", fCaps, mCaps, 14f, Lav, TextAlignmentOptions.TopLeft, 8f);
        Place(sub.rectTransform, TL, TL, new Vector2(130f, -88f), new Vector2(340f, 24f));

        var kp = Place(NewRect("KillPanel", s), TR, TR, new Vector2(-28f, -24f), new Vector2(360f, 118f));
        var plate = Img("Plate", kp, "ui_panel", new Color(1f, 1f, 1f, 0.9f));
        Stretch(plate.rectTransform);
        Place(Img("CatIcon", kp, "icon_cat", Color.white).rectTransform, TL, TL, new Vector2(22f, -20f), new Vector2(76f, 76f));
        var kl = Txt("Kills_Label", kp, "BLEEPS DEFEATED", fCaps, mCaps, 16f, Lav, TextAlignmentOptions.TopLeft, 6f);
        Place(kl.rectTransform, TL, TL, new Vector2(112f, -22f), new Vector2(240f, 26f));
        var kc = Txt("Kills_Count", kp, "0", fTitle, mTitle, 54f, Color.white, TextAlignmentOptions.TopLeft);
        ApplyGradient(kc, CreamTop, LavBottom);
        Place(kc.rectTransform, TL, TL, new Vector2(112f, -42f), new Vector2(230f, 66f));

        var hints = Place(NewRect("Hints", s), BC, BC, new Vector2(0f, 22f), new Vector2(1500f, 70f));
        Place(Img("HintsDivider", hints, "ui_divider", new Color(1f, 1f, 1f, 0.8f)).rectTransform, TC, TC, Vector2.zero, new Vector2(1000f, 20f));
        var ht = Txt("HintsText", hints, "WASD  MOVE     ·     SPACE  JUMP     ·     J / CLICK  SLASH     ·     HOLD WHEEL  CAMERA     ·     ESC / P  PAUSE", fCaps, mCaps, 17f, Lav, TextAlignmentOptions.Center, 4f);
        Place(ht.rectTransform, BC, BC, Vector2.zero, new Vector2(1500f, 40f));

        var fpsRoot = Place(NewRect("FPS_Root", s), TC, TC, new Vector2(0f, -16f), new Vector2(200f, 30f));
        var fps = Txt("FPS_Text", fpsRoot, "", fCaps, mCaps, 16f, Lav, TextAlignmentOptions.Center, 4f);
        Stretch(fps.rectTransform);

        var toasts = Place(NewRect("Toasts", s), TC, TC, new Vector2(0f, -150f), new Vector2(640f, 130f));
        BuildToast(toasts, "Toast_Parchment", "ui_toast_parchment", "icon_paw", true);
        BuildToast(toasts, "Toast_Violet", "ui_toast_violet", "icon_flame", false);
    }

    static void BuildToast(Transform parent, string name, string sprite, string icon, bool parchment)
    {
        var bg = Img(name, parent, sprite, Color.white);
        Place(bg.rectTransform, TC, TC, Vector2.zero, new Vector2(640f, 128f));
        bg.gameObject.AddComponent<CanvasGroup>();
        var ic = Img(name + "_Icon", bg.transform, icon, parchment ? new Color(0.22f, 0.1f, 0.2f, 1f) : Color.white);
        Place(ic.rectTransform, ML, ML, new Vector2(44f, 0f), new Vector2(66f, 66f));
        var title = Txt(name + "_Title", bg.transform, parchment ? "Quest Complete!" : "New Skill Unlocked!", fBrush, parchment ? mBrushInk : mBrush, 42f, parchment ? InkText : Color.white, TextAlignmentOptions.MidlineLeft);
        if (!parchment) ApplyGradient(title, CreamTop, LavBottom);
        Place(title.rectTransform, TL, TL, new Vector2(128f, -16f), new Vector2(480f, 56f));
        var sub = Txt(name + "_Sub", bg.transform, "", fBody, parchment ? null : mBody, 23f, parchment ? new Color(0.3f, 0.16f, 0.2f) : Lav, TextAlignmentOptions.TopLeft);
        Place(sub.rectTransform, TL, TL, new Vector2(130f, -72f), new Vector2(480f, 40f));
    }

    static void BuildPause(Transform root)
    {
        var s = MakeScreen("Screen_Pause", root).transform;
        Dim(s, 0.6f);
        var panel = Panel(s, new Vector2(580f, 660f));
        PanelTitle(panel, "PAUSED", 84f, 360f);
        var col = Place(NewRect("PauseButtons", panel), TC, TC, new Vector2(0f, -190f), new Vector2(360f, 330f));
        var vlg = col.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 16f; vlg.childAlignment = TextAnchor.UpperCenter;
        vlg.childControlWidth = true; vlg.childControlHeight = true; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;
        MenuButton("Btn_Resume", col, "Continue", new Vector2(340f, 66f), 32f);
        MenuButton("Btn_PauseOptions", col, "Options", new Vector2(340f, 66f), 32f);
        MenuButton("Btn_MainMenu", col, "Main Menu", new Vector2(340f, 66f), 32f);
        MenuButton("Btn_PauseQuit", col, "Quit", new Vector2(340f, 66f), 32f);
        var tag = Txt("Tagline", panel, "Shadows guide. Cats endure.", fBrush, mBrush, 24f, Violet, TextAlignmentOptions.Center);
        Place(tag.rectTransform, BC, BC, new Vector2(0f, 70f), new Vector2(500f, 36f));
        Place(Img("Ornament", panel, "ui_footer_ornament", Color.white).rectTransform, BC, BC, new Vector2(0f, 24f), new Vector2(180f, 45f));
    }

    static void BuildSettings(Transform root)
    {
        var s = MakeScreen("Screen_Settings", root).transform;
        Dim(s, 0.7f);
        var panel = Panel(s, new Vector2(1240f, 900f));
        PanelTitle(panel, "OPTIONS", 84f, 420f);

        var left = Column("LeftColumn", panel, new Vector2(80f, -175f), new Vector2(540f, 560f), 4f);
        Section(left, "AUDIO");
        SliderRow(left, "Master", "Slider_Master", 0f, 1f);
        SliderRow(left, "Music", "Slider_Music", 0f, 1f);
        SliderRow(left, "Effects", "Slider_Sfx", 0f, 1f);
        Section(left, "CONTROLS");
        SliderRow(left, "Mouse Speed", "Slider_Sensitivity", 0.05f, 0.6f);
        SliderRow(left, "Camera Distance", "Slider_CamDistance", 2.5f, 8f);
        ToggleRow(left, "Invert Camera Y", "Toggle_InvertY");

        var right = Column("RightColumn", panel, new Vector2(660f, -175f), new Vector2(540f, 560f), 4f);
        Section(right, "GRAPHICS");
        SelectorRow(right, "Quality", "Sel_Quality");
        SelectorRow(right, "Resolution", "Sel_Resolution");
        ToggleRow(right, "Fullscreen", "Toggle_Fullscreen");
        ToggleRow(right, "V-Sync", "Toggle_VSync");
        ToggleRow(right, "Bloom Glow", "Toggle_Bloom");
        Section(right, "INTERFACE");
        ToggleRow(right, "Control Hints", "Toggle_Hints");
        ToggleRow(right, "Show FPS", "Toggle_Fps");

        var tag = Txt("Tagline", panel, "Shadows Guide. Cats Endure. Destiny Follows.", fBrush, mBrush, 24f, Violet, TextAlignmentOptions.Center);
        Place(tag.rectTransform, BC, BC, new Vector2(0f, 128f), new Vector2(800f, 36f));
        var reset = MenuButton("Btn_SettingsReset", panel, "Reset Defaults", new Vector2(320f, 66f), 30f);
        Place((RectTransform)reset.transform, BC, BC, new Vector2(-190f, 44f), new Vector2(320f, 66f));
        var back = MenuButton("Btn_SettingsBack", panel, "Back", new Vector2(320f, 66f), 30f);
        Place((RectTransform)back.transform, BC, BC, new Vector2(190f, 44f), new Vector2(320f, 66f));
    }

    static void BuildHowTo(Transform root)
    {
        var s = MakeScreen("Screen_HowTo", root).transform;
        Dim(s, 0.7f);
        var panel = Panel(s, new Vector2(1000f, 760f));
        PanelTitle(panel, "HOW TO PLAY", 72f, 560f);
        string[,] rows =
        {
            { "WASD / Left Stick", "Move" },
            { "Space / (A)", "Jump" },
            { "J / Left Click / (X)", "Sword slash" },
            { "Hold Mouse Wheel", "Rotate the camera" },
            { "Q / E", "Turn the camera" },
            { "V", "Cinematic camera" },
            { "Esc / P / Start", "Pause" },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            float y = -176f - i * 52f;
            var k = Txt("Key" + i, panel, rows[i, 0], fBrush, mBrush, 28f, Violet, TextAlignmentOptions.MidlineRight);
            Place(k.rectTransform, TC, new Vector2(1f, 1f), new Vector2(-30f, y), new Vector2(400f, 48f));
            var d = Img("Dot" + i, panel, "icon_paw_violet", Color.white);
            Place(d.rectTransform, TC, TC, new Vector2(0f, y - 12f), new Vector2(24f, 24f));
            var a = Txt("Action" + i, panel, rows[i, 1], fBody, mBody, 30f, Cream, TextAlignmentOptions.MidlineLeft);
            Place(a.rectTransform, TC, new Vector2(0f, 1f), new Vector2(30f, y), new Vector2(380f, 48f));
        }
        var lore = Txt("Lore", panel, "Bleeps pour out of the summoning circles. Defeat them, survive the night,\nand follow the Path of Shadows.", fBody, mBody, 26f, Lav, TextAlignmentOptions.Center);
        Place(lore.rectTransform, BC, BC, new Vector2(0f, 120f), new Vector2(880f, 80f));
        var back = MenuButton("Btn_HowToBack", panel, "Back", new Vector2(300f, 66f), 30f);
        Place((RectTransform)back.transform, BC, BC, new Vector2(0f, 40f), new Vector2(300f, 66f));
    }
}
#endif

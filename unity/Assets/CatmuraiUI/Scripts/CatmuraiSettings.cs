using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Player settings for Catmurai (saved in PlayerPrefs) and how they are applied to the game.
/// </summary>
public static class CatmuraiSettings
{
    public static float Master = 1f, Music = 0.8f, Sfx = 0.8f;
    public static float Sensitivity = 0.2f, CamDistance = 4.6f;
    public static bool InvertY = false;
    public static int Quality = -1;       // -1 = keep the project's current level
    public static int ResolutionIndex = -1;
    public static bool Fullscreen = true, VSync = true, Bloom = true;
    public static bool ShowHints = true, ShowFps = false;

    const string P = "catmurai.";

    public static void Load()
    {
        Master = PlayerPrefs.GetFloat(P + "master", 1f);
        Music = PlayerPrefs.GetFloat(P + "music", 0.8f);
        Sfx = PlayerPrefs.GetFloat(P + "sfx", 0.8f);
        Sensitivity = PlayerPrefs.GetFloat(P + "sens", 0.2f);
        CamDistance = PlayerPrefs.GetFloat(P + "camdist", 4.6f);
        InvertY = PlayerPrefs.GetInt(P + "invertY", 0) == 1;
        Quality = PlayerPrefs.GetInt(P + "quality", QualitySettings.GetQualityLevel());
        ResolutionIndex = PlayerPrefs.GetInt(P + "res", -1);
        Fullscreen = PlayerPrefs.GetInt(P + "fullscreen", Screen.fullScreen ? 1 : 0) == 1;
        VSync = PlayerPrefs.GetInt(P + "vsync", 1) == 1;
        Bloom = PlayerPrefs.GetInt(P + "bloom", 1) == 1;
        ShowHints = PlayerPrefs.GetInt(P + "hints", 1) == 1;
        ShowFps = PlayerPrefs.GetInt(P + "fps", 0) == 1;
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(P + "master", Master);
        PlayerPrefs.SetFloat(P + "music", Music);
        PlayerPrefs.SetFloat(P + "sfx", Sfx);
        PlayerPrefs.SetFloat(P + "sens", Sensitivity);
        PlayerPrefs.SetFloat(P + "camdist", CamDistance);
        PlayerPrefs.SetInt(P + "invertY", InvertY ? 1 : 0);
        PlayerPrefs.SetInt(P + "quality", Quality);
        PlayerPrefs.SetInt(P + "res", ResolutionIndex);
        PlayerPrefs.SetInt(P + "fullscreen", Fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(P + "vsync", VSync ? 1 : 0);
        PlayerPrefs.SetInt(P + "bloom", Bloom ? 1 : 0);
        PlayerPrefs.SetInt(P + "hints", ShowHints ? 1 : 0);
        PlayerPrefs.SetInt(P + "fps", ShowFps ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetDefaults()
    {
        Master = 1f; Music = 0.8f; Sfx = 0.8f;
        Sensitivity = 0.2f; CamDistance = 4.6f; InvertY = false;
        Quality = QualitySettings.names.Length - 1;
        ResolutionIndex = -1;
        Fullscreen = true; VSync = true; Bloom = true;
        ShowHints = true; ShowFps = false;
    }

    /// Distinct screen resolutions (largest last).
    public static List<Vector2Int> Resolutions()
    {
        var list = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().OrderBy(v => v.x * v.y).ToList();
        if (list.Count == 0) list.Add(new Vector2Int(Screen.width, Screen.height));
        return list;
    }

    public static int CurrentResolutionIndex()
    {
        var list = Resolutions();
        if (ResolutionIndex >= 0 && ResolutionIndex < list.Count) return ResolutionIndex;
        int best = list.Count - 1;
        for (int i = 0; i < list.Count; i++)
            if (list[i].x == Screen.currentResolution.width && list[i].y == Screen.currentResolution.height) best = i;
        return best;
    }

    public static void Apply()
    {
        AudioListener.volume = Master;

        if (Quality >= 0 && Quality < QualitySettings.names.Length && QualitySettings.GetQualityLevel() != Quality)
            QualitySettings.SetQualityLevel(Quality, true);
        QualitySettings.vSyncCount = VSync ? 1 : 0;

        if (!Application.isEditor)
        {
            var mode = Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed;
            var res = Resolutions()[CurrentResolutionIndex()];
            if (Screen.width != res.x || Screen.height != res.y || Screen.fullScreenMode != mode)
                Screen.SetResolution(res.x, res.y, mode);
        }

        foreach (var cam in Object.FindObjectsByType<CatmuraiFollowCamera>(FindObjectsSortMode.None))
        {
            cam.mouseSensitivity = Sensitivity;
            cam.invertY = InvertY;
            cam.distance = CamDistance;
        }

        foreach (var v in Object.FindObjectsByType<Volume>(FindObjectsSortMode.None))
        {
            if (!v.sharedProfile) continue;
            if (v.profile.TryGet(out UnityEngine.Rendering.Universal.Bloom b)) b.active = Bloom;
        }
    }
}

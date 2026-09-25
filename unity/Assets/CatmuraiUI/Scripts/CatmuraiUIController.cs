using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Catmurai game UI: main menu (over the cinematic Sky Realm camera), HUD (health, Bleeps defeated,
/// control hints, toasts), pause menu, options menu and a how-to-play panel.
/// UI objects are found by name, so the hierarchy can be rebuilt by Catmurai > UI > Build Game UI.
/// Esc / gamepad Start = pause, back.
/// </summary>
public class CatmuraiUIController : MonoBehaviour
{
    enum UIState { Main, Game, Pause, Settings, HowTo }

    [Header("Timing")]
    public float fadeTime = 0.35f;
    public float toastHold = 2.6f;
    public int questEvery = 10;

    Dictionary<string, Transform> map;
    readonly Dictionary<UIState, CanvasGroup> screens = new Dictionary<UIState, CanvasGroup>();
    UIState state = UIState.Main;
    UIState settingsReturn = UIState.Main;
    bool inGame;

    // world
    CatmuraiPlayerController player;
    Health playerHealth;
    BleepSpawner[] spawners = new BleepSpawner[0];
    SkyRealmCameraRig rig;

    // HUD
    UnityEngine.UI.Image hpFill;
    TMP_Text hpText, killsText, fpsText;
    GameObject hints, fpsRoot;
    float shownHp = 1f;
    bool wasDead;
    int lastKills;
    float fpsTimer; int fpsFrames;

    // toasts
    CanvasGroup toastParchment, toastViolet;
    TMP_Text parchTitle, parchSub, violetTitle, violetSub;
    readonly Queue<(bool parchment, string title, string sub)> toastQueue = new Queue<(bool, string, string)>();
    bool toastRunning;

    bool suppress;

    // ---------------------------------------------------------------- lookup helpers
    Transform Find(string n) { return map != null && map.TryGetValue(n, out var t) ? t : null; }
    T Get<T>(string n) where T : Component { var t = Find(n); return t ? t.GetComponent<T>() : null; }

    void Button(string n, UnityEngine.Events.UnityAction a)
    {
        var b = Get<UnityEngine.UI.Button>(n);
        if (b) b.onClick.AddListener(a); else Debug.LogWarning("[CatmuraiUI] missing button " + n);
    }

    void Awake()
    {
        map = new Dictionary<string, Transform>();
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (!map.ContainsKey(t.name)) map[t.name] = t;

        screens[UIState.Main] = Get<CanvasGroup>("Screen_Main");
        screens[UIState.Game] = Get<CanvasGroup>("Screen_HUD");
        screens[UIState.Pause] = Get<CanvasGroup>("Screen_Pause");
        screens[UIState.Settings] = Get<CanvasGroup>("Screen_Settings");
        screens[UIState.HowTo] = Get<CanvasGroup>("Screen_HowTo");

        hpFill = Get<UnityEngine.UI.Image>("HP_Fill");
        hpText = Get<TMP_Text>("HP_Text");
        killsText = Get<TMP_Text>("Kills_Count");
        fpsText = Get<TMP_Text>("FPS_Text");
        var h = Find("Hints"); hints = h ? h.gameObject : null;
        var f = Find("FPS_Root"); fpsRoot = f ? f.gameObject : null;

        toastParchment = Get<CanvasGroup>("Toast_Parchment");
        toastViolet = Get<CanvasGroup>("Toast_Violet");
        parchTitle = Get<TMP_Text>("Toast_Parchment_Title");
        parchSub = Get<TMP_Text>("Toast_Parchment_Sub");
        violetTitle = Get<TMP_Text>("Toast_Violet_Title");
        violetSub = Get<TMP_Text>("Toast_Violet_Sub");
        foreach (var cg in new[] { toastParchment, toastViolet }) if (cg) { cg.alpha = 0f; cg.gameObject.SetActive(false); }

        Button("Btn_Start", StartGame);
        Button("Btn_HowTo", () => Show(UIState.HowTo));
        Button("Btn_Options", () => OpenSettings(UIState.Main));
        Button("Btn_Quit", Quit);
        Button("Btn_Resume", Resume);
        Button("Btn_PauseOptions", () => OpenSettings(UIState.Pause));
        Button("Btn_MainMenu", BackToMainMenu);
        Button("Btn_PauseQuit", Quit);
        Button("Btn_SettingsBack", CloseSettings);
        Button("Btn_SettingsReset", () => { CatmuraiSettings.ResetDefaults(); CatmuraiSettings.Apply(); CatmuraiSettings.Save(); SettingsToUI(); });
        Button("Btn_HowToBack", () => Show(inGame ? UIState.Pause : UIState.Main));

        BindSettings();
    }

    void Start()
    {
        player = FindAnyObjectByType<CatmuraiPlayerController>();
        if (player)
        {
            player.showHud = false;
            playerHealth = player.GetComponent<Health>();
        }
        spawners = FindObjectsByType<BleepSpawner>(FindObjectsSortMode.None);
        rig = FindAnyObjectByType<SkyRealmCameraRig>();

        CatmuraiSettings.Load();
        CatmuraiSettings.Apply();
        SettingsToUI();

        SetGameplay(false);
        Time.timeScale = 1f;
        ShowImmediate(UIState.Main);
    }

    // ---------------------------------------------------------------- flow
    void SetGameplay(bool on)
    {
        if (player) { player.enabled = on; player.showHud = false; }
        foreach (var s in spawners) if (s) s.enabled = on;
        if (rig) rig.SetCinematic(!on && !inGame);
    }

    public void StartGame()
    {
        inGame = true;
        BleepSpawner.TotalKilled = 0;
        lastKills = 0;
        Time.timeScale = 1f;
        SetGameplay(true);
        Show(UIState.Game);
        QueueToast(false, "A New Journey Awaits", "Small cat. Big blade. Bigger destiny.");
    }

    public void Pause()
    {
        if (state != UIState.Game) return;
        Time.timeScale = 0f;
        if (player) player.enabled = false;
        Show(UIState.Pause);
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        if (player) player.enabled = true;
        Show(UIState.Game);
    }

    void OpenSettings(UIState from)
    {
        settingsReturn = from;
        SettingsToUI();
        Show(UIState.Settings);
    }

    void CloseSettings()
    {
        CatmuraiSettings.Save();
        Show(settingsReturn);
    }

    public void BackToMainMenu()
    {
        Time.timeScale = 1f;
        var scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0) SceneManager.LoadScene(scene.buildIndex);
        else SceneManager.LoadScene(scene.name);
    }

    public void Quit()
    {
        CatmuraiSettings.Save();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ---------------------------------------------------------------- screens
    void ShowImmediate(UIState s)
    {
        state = s;
        foreach (var kv in screens)
        {
            if (!kv.Value) continue;
            bool on = kv.Key == s || (kv.Key == UIState.Game && inGame && s != UIState.Main);
            kv.Value.gameObject.SetActive(on);
            kv.Value.alpha = on ? 1f : 0f;
            kv.Value.interactable = kv.Key == s;
            kv.Value.blocksRaycasts = kv.Key == s;
        }
        SelectFirst(s);
    }

    void Show(UIState s)
    {
        state = s;
        foreach (var c in fades) if (c != null) StopCoroutine(c);
        fades.Clear();
        foreach (var kv in screens)
        {
            var cg = kv.Value;
            if (!cg) continue;
            // the HUD stays visible (dimmed underneath) while paused / in options during a run
            bool on = kv.Key == s || (kv.Key == UIState.Game && inGame && s != UIState.Main);
            cg.interactable = kv.Key == s;
            cg.blocksRaycasts = kv.Key == s;
            if (on)
            {
                if (!cg.gameObject.activeSelf) { cg.gameObject.SetActive(true); cg.alpha = 0f; }
                fades.Add(StartCoroutine(Fade(cg, 1f, false)));
            }
            else if (cg.gameObject.activeSelf) fades.Add(StartCoroutine(Fade(cg, 0f, true)));
        }
        SelectFirst(s);
    }

    readonly List<Coroutine> fades = new List<Coroutine>();

    IEnumerator Fade(CanvasGroup cg, float to, bool deactivate)
    {
        float from = cg.alpha;
        float t = 0f;
        while (t < fadeTime)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(from, to, t / fadeTime);
            yield return null;
        }
        cg.alpha = to;
        if (deactivate) cg.gameObject.SetActive(false);
    }

    void SelectFirst(UIState s)
    {
        if (!EventSystem.current) return;
        string first = null;
        if (s == UIState.Pause) first = "Btn_Resume";
        else if (s == UIState.Settings) first = "Btn_SettingsBack";
        else if (s == UIState.HowTo) first = "Btn_HowToBack";
        var t = first != null ? Find(first) : null;
        EventSystem.current.SetSelectedGameObject(t ? t.gameObject : null);
    }

    // ---------------------------------------------------------------- per frame
    void Update()
    {
        var kb = Keyboard.current;
        var gp = Gamepad.current;
        // Esc (builds), P (also works inside the Unity editor, which keeps Esc for itself), gamepad Start
        bool back = (kb != null && (kb.escapeKey.wasPressedThisFrame || kb.pKey.wasPressedThisFrame)) || (gp != null && gp.startButton.wasPressedThisFrame);
        if (back)
        {
            switch (state)
            {
                case UIState.Game: Pause(); break;
                case UIState.Pause: Resume(); break;
                case UIState.Settings: CloseSettings(); break;
                case UIState.HowTo: Show(inGame ? UIState.Pause : UIState.Main); break;
            }
        }

        UpdateHud();
    }

    void UpdateHud()
    {
        if (playerHealth && hpFill)
        {
            float frac = playerHealth.maxHealth > 0f ? playerHealth.current / playerHealth.maxHealth : 0f;
            shownHp = Mathf.MoveTowards(shownHp, frac, Time.unscaledDeltaTime * 1.5f);
            hpFill.fillAmount = shownHp;
            Color hi = new Color(0.72f, 0.40f, 1f), lo = new Color(0.78f, 0.12f, 0.25f);
            Color c = Color.Lerp(lo, hi, Mathf.Clamp01((frac - 0.15f) / 0.5f));
            if (frac < 0.3f && frac > 0f) c *= 0.75f + 0.25f * Mathf.Sin(Time.unscaledTime * 8f);
            c.a = 1f;
            hpFill.color = c;
            if (hpText) hpText.text = Mathf.CeilToInt(playerHealth.current) + " / " + Mathf.CeilToInt(playerHealth.maxHealth);

            if (inGame && playerHealth.IsDead && !wasDead)
                QueueToast(false, "Defeated...", "Rise again, Catmurai. The shadows are waiting.");
            wasDead = playerHealth.IsDead;
        }

        int kills = BleepSpawner.TotalKilled;
        if (killsText) killsText.text = kills.ToString();
        if (inGame && kills != lastKills)
        {
            if (lastKills == 0 && kills > 0) QueueToast(true, "First Bleep Defeated!", "A little darkness goes a long way.");
            else if (questEvery > 0 && kills / questEvery > lastKills / questEvery)
                QueueToast(true, "Quest Complete!", "Defeated " + (kills / questEvery * questEvery) + " Bleeps");
            lastKills = kills;
        }

        if (hints) hints.SetActive(CatmuraiSettings.ShowHints);
        if (fpsRoot) fpsRoot.SetActive(CatmuraiSettings.ShowFps);
        if (CatmuraiSettings.ShowFps && fpsText)
        {
            fpsFrames++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                fpsText.text = Mathf.RoundToInt(fpsFrames / fpsTimer) + " FPS";
                fpsFrames = 0; fpsTimer = 0f;
            }
        }
    }

    // ---------------------------------------------------------------- toasts
    public void QueueToast(bool parchment, string title, string sub)
    {
        toastQueue.Enqueue((parchment, title, sub));
        if (!toastRunning) StartCoroutine(RunToasts());
    }

    IEnumerator RunToasts()
    {
        toastRunning = true;
        foreach (var cg in new[] { toastParchment, toastViolet }) if (cg) { cg.alpha = 0f; cg.gameObject.SetActive(false); }
        while (toastQueue.Count > 0)
        {
            var (parchment, title, sub) = toastQueue.Dequeue();
            var cg = parchment ? toastParchment : toastViolet;
            if (!cg) continue;
            (parchment ? parchTitle : violetTitle).text = title;
            var st = parchment ? parchSub : violetSub;
            if (st) st.text = sub;
            cg.gameObject.SetActive(true);
            var rt = (RectTransform)cg.transform;
            Vector2 basePos = rt.anchoredPosition;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.3f);
                cg.alpha = k;
                rt.anchoredPosition = basePos + Vector2.up * (1f - k) * 30f;
                rt.localScale = Vector3.one * Mathf.Lerp(0.9f, 1f, k);
                yield return null;
            }
            rt.anchoredPosition = basePos;
            float hold = 0f;
            while (hold < toastHold) { hold += Time.unscaledDeltaTime; yield return null; }
            t = 0f;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                cg.alpha = 1f - t / 0.4f;
                yield return null;
            }
            cg.alpha = 0f;
            cg.gameObject.SetActive(false);
        }
        toastRunning = false;
    }

    // ---------------------------------------------------------------- settings <-> UI
    UnityEngine.UI.Slider sMaster, sMusic, sSfx, sSens, sDist;
    UnityEngine.UI.Toggle tFull, tVsync, tBloom, tInvert, tHints, tFps;
    UISelector selQuality, selRes;

    void BindSlider(ref UnityEngine.UI.Slider s, string name, System.Action<float> set, System.Func<float, string> fmt)
    {
        s = Get<UnityEngine.UI.Slider>(name);
        var valueText = Get<TMP_Text>(name + "_Value");
        if (!s) return;
        s.onValueChanged.AddListener(v =>
        {
            if (valueText) valueText.text = fmt(v);
            if (suppress) return;
            set(v);
            CatmuraiSettings.Apply();
            CatmuraiSettings.Save();
        });
    }

    void BindToggle(ref UnityEngine.UI.Toggle t, string name, System.Action<bool> set)
    {
        t = Get<UnityEngine.UI.Toggle>(name);
        if (!t) return;
        t.onValueChanged.AddListener(v =>
        {
            if (suppress) return;
            set(v);
            CatmuraiSettings.Apply();
            CatmuraiSettings.Save();
        });
    }

    static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }

    void BindSettings()
    {
        BindSlider(ref sMaster, "Slider_Master", v => CatmuraiSettings.Master = v, Pct);
        BindSlider(ref sMusic, "Slider_Music", v => CatmuraiSettings.Music = v, Pct);
        BindSlider(ref sSfx, "Slider_Sfx", v => CatmuraiSettings.Sfx = v, Pct);
        BindSlider(ref sSens, "Slider_Sensitivity", v => CatmuraiSettings.Sensitivity = v, v => v.ToString("0.00"));
        BindSlider(ref sDist, "Slider_CamDistance", v => CatmuraiSettings.CamDistance = v, v => v.ToString("0.0") + " m");
        BindToggle(ref tFull, "Toggle_Fullscreen", v => CatmuraiSettings.Fullscreen = v);
        BindToggle(ref tVsync, "Toggle_VSync", v => CatmuraiSettings.VSync = v);
        BindToggle(ref tBloom, "Toggle_Bloom", v => CatmuraiSettings.Bloom = v);
        BindToggle(ref tInvert, "Toggle_InvertY", v => CatmuraiSettings.InvertY = v);
        BindToggle(ref tHints, "Toggle_Hints", v => CatmuraiSettings.ShowHints = v);
        BindToggle(ref tFps, "Toggle_Fps", v => CatmuraiSettings.ShowFps = v);

        selQuality = Get<UISelector>("Sel_Quality");
        if (selQuality) selQuality.onChanged = i => { CatmuraiSettings.Quality = i; CatmuraiSettings.Apply(); CatmuraiSettings.Save(); };
        selRes = Get<UISelector>("Sel_Resolution");
        if (selRes) selRes.onChanged = i => { CatmuraiSettings.ResolutionIndex = i; CatmuraiSettings.Apply(); CatmuraiSettings.Save(); };
    }

    void SettingsToUI()
    {
        suppress = true;
        if (sMaster) { sMaster.value = CatmuraiSettings.Master; sMaster.onValueChanged.Invoke(sMaster.value); }
        if (sMusic) { sMusic.value = CatmuraiSettings.Music; sMusic.onValueChanged.Invoke(sMusic.value); }
        if (sSfx) { sSfx.value = CatmuraiSettings.Sfx; sSfx.onValueChanged.Invoke(sSfx.value); }
        if (sSens) { sSens.value = CatmuraiSettings.Sensitivity; sSens.onValueChanged.Invoke(sSens.value); }
        if (sDist) { sDist.value = CatmuraiSettings.CamDistance; sDist.onValueChanged.Invoke(sDist.value); }
        if (tFull) tFull.isOn = CatmuraiSettings.Fullscreen;
        if (tVsync) tVsync.isOn = CatmuraiSettings.VSync;
        if (tBloom) tBloom.isOn = CatmuraiSettings.Bloom;
        if (tInvert) tInvert.isOn = CatmuraiSettings.InvertY;
        if (tHints) tHints.isOn = CatmuraiSettings.ShowHints;
        if (tFps) tFps.isOn = CatmuraiSettings.ShowFps;
        if (selQuality) selQuality.SetOptions(QualitySettings.names, CatmuraiSettings.Quality >= 0 ? CatmuraiSettings.Quality : QualitySettings.GetQualityLevel());
        if (selRes)
        {
            var list = CatmuraiSettings.Resolutions();
            selRes.SetOptions(list.Select(r => r.x + " x " + r.y).ToArray(), CatmuraiSettings.CurrentResolutionIndex());
        }
        suppress = false;
    }
}

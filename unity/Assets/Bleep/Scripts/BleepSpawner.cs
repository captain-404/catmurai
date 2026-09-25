using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps up to maxAlive Bleeps around this point. When one dies, a new one is summoned after respawnDelay.
/// Spawned Bleeps idle/wander around the spawn point and chase the player when they come within detectRange.
/// </summary>
public class BleepSpawner : MonoBehaviour
{
    public GameObject bleepPrefab;
    public int maxAlive = 2;
    public float respawnDelay = 8f;
    public float firstSpawnDelay = 1f;
    public float spawnRadius = 1.5f;

    [Header("Behaviour of spawned Bleeps")]
    public float detectRange = 9f;
    public float leashRange = 16f;
    public float wanderRadius = 2.5f;

    [Tooltip("Only summon while the player is within this distance (0 = always).")]
    public float activationRange = 45f;

    [Header("Summon effect")]
    public float growDuration = 0.6f;
    public Light glow;

    readonly List<GameObject> alive = new List<GameObject>();
    float timer;
    bool filledOnce;
    Transform player;
    float glowBase;
    float pulse;

    public static int TotalKilled;

    void Start()
    {
        timer = firstSpawnDelay;
        var p = GameObject.Find("Player");
        if (p) player = p.transform;
        if (glow) glowBase = glow.intensity;
    }

    void Update()
    {
        int before = alive.Count;
        alive.RemoveAll(g => g == null);
        TotalKilled += before - alive.Count;

        if (glow)
        {
            pulse = Mathf.Max(0f, pulse - Time.deltaTime * 1.5f);
            glow.intensity = glowBase * (1f + pulse * 3f);
        }

        if (alive.Count >= maxAlive || !bleepPrefab) return;
        if (activationRange > 0f && player && Vector3.Distance(player.position, transform.position) > activationRange) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;
        Spawn();
        if (alive.Count >= maxAlive) filledOnce = true;
        timer = filledOnce ? respawnDelay : 0.7f;
    }

    void Spawn()
    {
        Vector2 r = Random.insideUnitCircle * spawnRadius;
        Vector3 pos = transform.position + new Vector3(r.x, 0.4f, r.y);
        var go = Instantiate(bleepPrefab, pos, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
        go.name = "Bleep";
        var be = go.GetComponent<BleepEnemy>();
        if (be)
        {
            be.detectRange = detectRange;
            be.leashRange = leashRange;
            be.wanderRadius = wanderRadius;
            be.home = pos;
            be.hasHome = true;
            be.followGround = true;
        }
        alive.Add(go);
        pulse = 1f;
        StartCoroutine(Grow(go));
    }

    IEnumerator Grow(GameObject go)
    {
        var h = go.GetComponent<Health>();
        Vector3 full = go.transform.localScale;
        float t = 0f;
        while (t < growDuration)
        {
            if (!go || (h && h.IsDead)) yield break;
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / growDuration);
            float e = 1f + 2.2f * Mathf.Pow(k - 1f, 3f) + 1.2f * Mathf.Pow(k - 1f, 2f); // ease-out with a little overshoot
            go.transform.localScale = full * Mathf.Max(0.01f, e);
            yield return null;
        }
        if (go && !(h && h.IsDead)) go.transform.localScale = full;
    }
}

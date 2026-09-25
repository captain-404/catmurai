using UnityEngine;

/// <summary>
/// Puts the player back on the spawn point after falling off an island, or after being defeated
/// (health restored to full).
/// </summary>
public class SkyRealmRespawn : MonoBehaviour
{
    public Vector3 spawnPoint;
    public float spawnYaw = 0f;
    public float killY = -25f;
    [Tooltip("Seconds to wait after the player is defeated before respawning.")]
    public float deathDelay = 1.2f;

    Health health;
    float deathTimer = -1f;

    void Start()
    {
        health = GetComponent<Health>();
        if (health) health.OnDeath += () => deathTimer = deathDelay;
    }

    void Update()
    {
        if (deathTimer >= 0f)
        {
            deathTimer -= Time.deltaTime;
            if (deathTimer < 0f) Respawn(true);
            return;
        }
        if (transform.position.y < killY) Respawn(false);
    }

    void Respawn(bool revive)
    {
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        transform.SetPositionAndRotation(spawnPoint, Quaternion.Euler(0f, spawnYaw, 0f));
        if (cc) cc.enabled = true;
        if (revive && health) health.Revive();
        Debug.Log(revive ? "[SkyRealm] Catmurai was defeated - respawned at the landing." : "[SkyRealm] Fell off - respawned.");
    }
}

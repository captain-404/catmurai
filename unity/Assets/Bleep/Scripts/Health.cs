using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Minimal shared health component for player and enemies. Keeps a static registry so
/// attackers (e.g. the Catmurai slash) can find nearby targets without needing colliders/triggers set up.
/// </summary>
public class Health : MonoBehaviour
{
    public float maxHealth = 30f;
    [HideInInspector] public float current;
    public float invulnSeconds = 0.15f;

    public event Action<float, float> OnChanged; // current, max
    public event Action<float> OnDamaged;        // amount
    public event Action OnDeath;

    float invulnTimer;
    bool dead;
    public bool IsDead => dead;

    public static readonly List<Health> All = new List<Health>();

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    void Awake() { current = maxHealth; }

    void Update()
    {
        if (invulnTimer > 0f) invulnTimer -= Time.deltaTime;
    }

    public bool TakeDamage(float amount)
    {
        if (dead || invulnTimer > 0f || amount <= 0f) return false;
        current = Mathf.Max(0f, current - amount);
        invulnTimer = invulnSeconds;
        OnDamaged?.Invoke(amount);
        OnChanged?.Invoke(current, maxHealth);
        if (current <= 0f && !dead)
        {
            dead = true;
            OnDeath?.Invoke();
        }
        return true;
    }

    /// <summary>Bring back to full health (used when the player respawns).</summary>
    public void Revive()
    {
        dead = false;
        current = maxHealth;
        invulnTimer = 1f;
        OnChanged?.Invoke(current, maxHealth);
    }

    public void Heal(float amount)
    {
        if (dead || amount <= 0f) return;
        current = Mathf.Min(maxHealth, current + amount);
        OnChanged?.Invoke(current, maxHealth);
    }
}

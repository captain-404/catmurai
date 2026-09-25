using UnityEngine;

/// <summary>Gentle flame flicker for lantern / brazier lights.</summary>
[RequireComponent(typeof(Light))]
public class SkyRealmFlicker : MonoBehaviour
{
    public float baseIntensity = 2f;
    public float amount = 0.18f;
    public float speed = 3f;
    public float seed = 0f;
    Light l;

    void Awake() { l = GetComponent<Light>(); }

    void Update()
    {
        float n = Mathf.PerlinNoise(Time.time * speed, seed) - 0.5f;
        l.intensity = baseIntensity * (1f + n * 2f * amount);
    }
}

using UnityEngine;

/// <summary>Slow bobbing (and optional spin) for floating rocks and crystals.</summary>
public class SkyRealmFloat : MonoBehaviour
{
    public float amplitude = 0.4f;
    public float speed = 0.3f;
    public float phase = 0f;
    public float spin = 0f;
    Vector3 start;

    void Start() { start = transform.localPosition; }

    void Update()
    {
        transform.localPosition = start + Vector3.up * (Mathf.Sin(Time.time * speed + phase) * amplitude);
        if (spin != 0f) transform.Rotate(0f, spin * Time.deltaTime, 0f, Space.Self);
    }
}

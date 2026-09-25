using UnityEngine;

/// <summary>Cloth-like sway for hanging banners.</summary>
public class SkyRealmSway : MonoBehaviour
{
    public float degrees = 4f;
    public float speed = 0.9f;
    public float phase = 0f;
    Quaternion baseRot;

    void Start() { baseRot = transform.localRotation; }

    void Update()
    {
        float a = Mathf.Sin(Time.time * speed + phase) * degrees;
        float b = Mathf.Sin(Time.time * speed * 1.7f + phase * 2f) * degrees * 0.35f;
        transform.localRotation = baseRot * Quaternion.Euler(a, 0f, b);
    }
}

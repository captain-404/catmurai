using UnityEngine;

/// <summary>Constant rotation around a local axis (rune rings, floating crystals).</summary>
public class SkyRealmSpin : MonoBehaviour
{
    public Vector3 axis = Vector3.up;
    public float degreesPerSecond = 4f;

    void Update() { transform.Rotate(axis, degreesPerSecond * Time.deltaTime, Space.Self); }
}

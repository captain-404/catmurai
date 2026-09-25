using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Press V to toggle between the normal follow camera and a slow cinematic orbit of the Sky Realm
/// (handy for YouTube b-roll). Also refreshes environment lighting on start.
/// </summary>
public class SkyRealmCameraRig : MonoBehaviour
{
    public CatmuraiFollowCamera follow;
    public Vector3 orbitCenter = new Vector3(0f, 3f, 4f);
    public float orbitRadius = 40f;
    public float orbitHeight = 16f;
    public float orbitSpeed = 4f;
    public bool cinematic = false;
    [Tooltip("V toggles the cinematic camera (b-roll).")]
    public bool allowToggleKey = true;
    float angle = -90f;

    void Start()
    {
        DynamicGI.UpdateEnvironment();
        if (follow) follow.enabled = !cinematic;
    }

    /// Switch between the cinematic orbit (menus) and the follow camera (gameplay).
    public void SetCinematic(bool on)
    {
        cinematic = on;
        if (follow) follow.enabled = !on;
    }

    void LateUpdate()
    {
        var kb = Keyboard.current;
        if (allowToggleKey && kb != null && kb.vKey.wasPressedThisFrame)
        {
            cinematic = !cinematic;
            if (follow) follow.enabled = !cinematic;
        }
        if (!cinematic) return;
        angle += orbitSpeed * Time.deltaTime;
        float a = angle * Mathf.Deg2Rad;
        transform.position = orbitCenter + new Vector3(Mathf.Cos(a) * orbitRadius, orbitHeight, Mathf.Sin(a) * orbitRadius);
        transform.LookAt(orbitCenter + Vector3.up * 2f);
    }
}

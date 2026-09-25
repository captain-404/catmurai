using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Smooth third-person follow camera.
/// Hold the mouse wheel button (middle click) and move the mouse to orbit around the player
/// (left/right = yaw, up/down = pitch). Q / E also orbit the camera.
/// Mouse wheel zooms in/out (close zoom frames the face).
/// </summary>
public class CatmuraiFollowCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 3.6f;
    public float height = 0.9f;
    public float pitch = 30f;
    public float yaw = 0f;
    public float smooth = 8f;
    public float orbitSpeed = 90f;

    [Header("Zoom (mouse wheel)")]
    public float minDistance = 0.9f;
    public float maxDistance = 6f;
    [Tooltip("Distance change per wheel notch, as a fraction of the current distance.")]
    public float zoomStep = 0.12f;
    [Tooltip("Look-at height when fully zoomed in (the face).")]
    public float closeHeight = 0.72f;
    float targetDistance = -1f;

    [Header("Mouse orbit (hold wheel button)")]
    [Tooltip("Degrees of rotation per pixel of mouse movement.")]
    public float mouseSensitivity = 0.2f;
    public bool invertY = false;
    public float minPitch = -10f;
    public float maxPitch = 75f;
    [Tooltip("Hide and lock the cursor while the wheel button is held.")]
    public bool lockCursorWhileRotating = true;

    Vector3 focus;
    bool hasFocus;
    bool rotating;

    void LateUpdate()
    {
        if (!target) return;

        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.qKey.isPressed) yaw -= orbitSpeed * Time.deltaTime;
            if (kb.eKey.isPressed) yaw += orbitSpeed * Time.deltaTime;
        }

        var ms = Mouse.current;
        bool held = ms != null && ms.middleButton.isPressed;
        if (held)
        {
            Vector2 d = ms.delta.ReadValue();
            yaw += d.x * mouseSensitivity;
            pitch += (invertY ? d.y : -d.y) * mouseSensitivity;
        }
        if (held != rotating)
        {
            rotating = held;
            if (lockCursorWhileRotating)
            {
                Cursor.lockState = held ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = !held;
            }
        }
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (targetDistance < 0f) targetDistance = distance;
        if (ms != null)
        {
            float wheel = ms.scroll.ReadValue().y;
            if (Mathf.Abs(wheel) > 0.01f)
                targetDistance = Mathf.Clamp(targetDistance * (1f - Mathf.Sign(wheel) * zoomStep), minDistance, maxDistance);
        }
        distance = Mathf.Lerp(distance, targetDistance, 1f - Mathf.Exp(-12f * Time.deltaTime));
        float zoom01 = Mathf.InverseLerp(minDistance, Mathf.Max(minDistance + 0.01f, 3.6f), distance);
        float lookHeight = Mathf.Lerp(closeHeight, height, zoom01);

        // Smooth the point we look at, then place the camera exactly on the orbit,
        // so the player stays centred even while orbiting quickly.
        Vector3 desiredFocus = target.position + Vector3.up * lookHeight;
        if (!hasFocus) { focus = desiredFocus; hasFocus = true; }
        focus = Vector3.Lerp(focus, desiredFocus, 1f - Mathf.Exp(-smooth * Time.deltaTime));

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        transform.position = focus - rot * Vector3.forward * distance;
        transform.rotation = rot;
    }

    void OnDisable()
    {
        if (rotating && lockCursorWhileRotating)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        rotating = false;
        hasFocus = false;
    }
}

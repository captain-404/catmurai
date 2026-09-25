using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Hover / select / press feedback for Catmurai menu buttons: glow frame fades in, paw icon appears,
/// label brightens and the button grows slightly. Uses unscaled time so it works while paused.
/// </summary>
public class UIButtonFX : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler, IPointerDownHandler, IPointerUpHandler
{
    public UnityEngine.UI.Graphic glow;
    public UnityEngine.UI.Graphic icon;
    public TMP_Text label;
    public Color labelNormal = new Color(0.93f, 0.88f, 0.80f, 1f);
    public Color labelHover = new Color(1f, 0.97f, 0.92f, 1f);
    public float hoverScale = 1.05f;
    public float speed = 10f;

    bool hover, selected, pressed;
    Vector3 baseScale = Vector3.one;
    float t;

    void Awake()
    {
        baseScale = transform.localScale;
        ApplyVisual(0f);
    }

    void OnDisable()
    {
        hover = selected = pressed = false;
        t = 0f;
        ApplyVisual(0f);
    }

    public void OnPointerEnter(PointerEventData e)
    {
        hover = true;
        if (EventSystem.current) EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hover = false;
        pressed = false;
        if (EventSystem.current && EventSystem.current.currentSelectedGameObject == gameObject)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnSelect(BaseEventData e) { selected = true; }
    public void OnDeselect(BaseEventData e) { selected = false; }
    public void OnPointerDown(PointerEventData e) { pressed = true; }
    public void OnPointerUp(PointerEventData e) { pressed = false; }

    void Update()
    {
        float target = (hover || selected) ? 1f : 0f;
        t = Mathf.MoveTowards(t, target, Time.unscaledDeltaTime * speed);
        ApplyVisual(t);
    }

    void ApplyVisual(float k)
    {
        float e = k * k * (3f - 2f * k);
        if (glow) { var c = glow.color; c.a = e; glow.color = c; }
        if (icon) { var c = icon.color; c.a = e; icon.color = c; }
        if (label) label.color = Color.Lerp(labelNormal, labelHover, e);
        transform.localScale = baseScale * Mathf.Lerp(1f, hoverScale, e) * (pressed ? 0.96f : 1f);
    }
}

using TMPro;
using UnityEngine;

/// <summary>"‹ value ›" option picker used in the Options menu (quality, resolution).</summary>
public class UISelector : MonoBehaviour
{
    public UnityEngine.UI.Button left;
    public UnityEngine.UI.Button right;
    public TMP_Text label;
    public string[] options = new string[0];
    public int index;
    public System.Action<int> onChanged;

    void Awake()
    {
        if (left) left.onClick.AddListener(() => Step(-1));
        if (right) right.onClick.AddListener(() => Step(1));
    }

    public void SetOptions(string[] opts, int i)
    {
        options = opts ?? new string[0];
        index = Mathf.Clamp(i, 0, Mathf.Max(0, options.Length - 1));
        Refresh();
    }

    public void Step(int d)
    {
        if (options.Length == 0) return;
        index = (index + d + options.Length) % options.Length;
        Refresh();
        onChanged?.Invoke(index);
    }

    void Refresh()
    {
        if (label) label.text = options.Length > 0 ? options[index] : "-";
    }
}

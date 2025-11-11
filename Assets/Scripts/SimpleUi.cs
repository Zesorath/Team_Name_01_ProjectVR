using UnityEngine;
using TMPro;
using System.Text;

public class SimpleUi : MonoBehaviour
{
    [Header("Links")]
    public CircuitManager manager;         // assign in Inspector
    public TextMeshProUGUI label;          // assign a TMP text
    public Transform labelRoot;            // optional: parent to auto-find TMP

    [Header("Options")]
    public bool listAllNodes = true;       // show all node voltages
    public string[] nodesToShow;           // optional subset (e.g., "0","N1","N2")

    [Header("Visibility (optional)")]
    public CanvasGroup canvasGroup;        // set blocksRaycasts=false in Inspector

    void Awake()
    {
        // keep HUD out of XR raycasts
        if (label != null) label.raycastTarget = false;
        if (canvasGroup != null) canvasGroup.blocksRaycasts = false;
    }

    void OnEnable()
    {
        if (!manager) manager = Object.FindFirstObjectByType<CircuitManager>();
        if (manager != null) manager.OnSolved += HandleSolved;
    }

    void OnDisable()
    {
        if (manager != null) manager.OnSolved -= HandleSolved;
        label = null; // drop ref to avoid touching a destroyed label later
    }

    void TryFindLabel()
    {
        if (label != null) return;
        var root = labelRoot ? labelRoot : transform;
        label = root.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null) label.raycastTarget = false;
    }

    bool IsGone(Object o) => o == null || ReferenceEquals(o, null);

    void HandleSolved()
    {
        if (!isActiveAndEnabled || manager == null) return;

        if (IsGone(label)) TryFindLabel();
        if (IsGone(label)) return;

        if (canvasGroup != null && canvasGroup.alpha == 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        var sb = new StringBuilder();

        if (listAllNodes)
        {
            foreach (var kv in manager.nodeVoltages)
                sb.Append(kv.Key).Append(": ").Append(kv.Value.ToString("F3")).Append(" V\n");
        }
        else if (nodesToShow != null && nodesToShow.Length > 0)
        {
            foreach (var n in nodesToShow)
            {
                double v;
                if (manager.nodeVoltages.TryGetValue(n, out v))
                    sb.Append(n).Append(": ").Append(v.ToString("F3")).Append(" V\n");
                else
                    sb.Append(n).Append(": (n/a)\n");
            }
        }
        else
        {
            sb.Append("No nodes configured.");
        }

        // optionally include computed resistor currents if you kept deviceCurrents
        // foreach (var kv in manager.deviceCurrents) sb.Append(kv.Key).Append(": ").Append((kv.Value*1000).ToString("F3")).Append(" mA\n");

        label.text = sb.ToString().TrimEnd();
    }
}

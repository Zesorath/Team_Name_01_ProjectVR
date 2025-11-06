using UnityEngine;
using TMPro;

public class MeasurementDisplayTMP : MonoBehaviour
{
    [Header("Links")]
    public CableLink link;            // assign in Inspector
    public TextMeshProUGUI label;     // assign the TMP text here

    [Header("Optional")]
    public CanvasGroup canvasGroup;   // assign if you have one

    void Awake()
    {
        // Make the label non-interactable for UI raycasts
        if (label != null) label.raycastTarget = false;

        // If you want this HUD to persist across scene reloads:
        // DontDestroyOnLoad(gameObject);
    }

    void OnEnable()
    {
        if (link != null) link.OnTelemetry += HandleTelemetry;
    }

    void OnDisable()
    {
        if (link != null) link.OnTelemetry -= HandleTelemetry;
        // Break the reference so XR UI won't stumble over a destroyed label
        // (especially if this object is about to be disabled/destroyed)
        // We do NOT Destroy it; we simply stop using it.
        // If your workflow sometimes rebinds a new label, leave this as-is.
    }

    private void HandleTelemetry(float v, float i, float r)
    {
        // The safest possible guards:
        if (label == null) return;
        if (ReferenceEquals(label, null)) return;        // covers destroyed objects
        if (label.gameObject == null) return;            // extra guard

        // If you use a CanvasGroup to hide/show, ensure we’re visible when values arrive
        if (canvasGroup != null && canvasGroup.alpha == 0f)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        label.text =
            "Voltage: " + v.ToString("F3") + " V\n" +
            "Current: " + (i * 1000f).ToString("F3") + " mA\n" +
            "Resistance: " + r.ToString("F0") + " Ohms";
    }
}

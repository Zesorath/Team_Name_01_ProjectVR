using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;


public class LED_Component : CircuitComponentBase
{
    [Header("Bulb Visual Settings")]
    public Renderer ledRenderer;
    public Color offColor     = Color.black;
    public Color dimColor_filament     = new Color(1f, 0.55f, 0.05f);   // dim warm orange
    public Color brightColor_filament  = new Color(1f, 0.98f, 0.85f);   // bright warm white
    public Light lightSource;

    [Header("Electrical Characteristics")]
    [Tooltip("Bulb resistance in ohms. Controls how bright it glows and how fast the cap discharges.\n" +
             "Lower R = brighter + faster discharge. Typical range: 100–2000 Ω.\n")]

    public float resistance = 1000f;

    [Tooltip("Voltage across the bulb that maps to 100 % brightness.")]
    public float maxVoltage = 5.0f;

    // Used by save system (ObjectState) — keep name/accessor stable
    public float currentVoltage = 0f;
    public float CurrentVoltage
    {
        get => currentVoltage;
        set => currentVoltage = value;
    }

    [Header("UI Display")]
    public TMPro.TextMeshProUGUI resistanceLabel;
    public TMPro.TextMeshProUGUI maxVoltageLabel;

    [Header("Runtime Adjustment")]
    public float resistanceStep  = 100f;
    public float minResistance   = 10f;
    public float maxResistance   = 10000f;
    public float voltageStep     = 0.5f;
    public float minMaxVoltage   = 0.5f;
    public float maxMaxVoltage   = 12f;

    protected override void Awake()
    {
        base.Awake();
        UpdateLEDState(0);
    }

    // ─────────────────────────────────────────────────────────────
    // SpiceSharp — bulb is a pure resistor; no diode model needed
    // ─────────────────────────────────────────────────────────────
    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string rName = $"R_{componentId}_BULB";
        var r = new SpiceSharp.Components.Resistor(rName, nodeA, nodeB, resistance);
        ckt.Add(r);
        Debug.Log($"[Bulb] {componentId}: {resistance:F0} Ω  ({nodeA} → {nodeB})");
    }

    // ─────────────────────────────────────────────────────────────
    // Called every simulation step by CircuitManager.
    // voltageDrop = |V(nodeA) - V(nodeB)|
    // ─────────────────────────────────────────────────────────────
    public void UpdateLEDState(float voltageDrop)
    {
        currentVoltage = voltageDrop;

        if (!ledRenderer)
            return;

        Material mat = ledRenderer.material;

        // Use power (V²) curve so the bulb dims realistically — quickly at first,
        // holding a warm glow longer before going dark.
        float linear = Mathf.Clamp01(voltageDrop / maxVoltage);
        float t = linear * linear; // power curve

        if (t < 0.004f) // ~< 10 % of maxVoltage
        {
            mat.color = offColor;
            mat.SetColor("_EmissionColor", offColor);
            lightSource.color = offColor;
            Debug.Log($"[Bulb] {componentId}: {voltageDrop:F3} V → OFF");
        }
        else
        {
            float c = Mathf.Lerp(0, 0.05f, t);
            lightSource.intensity = c;
            Color c2 = Color.Lerp(dimColor_filament, brightColor_filament, t);
            mat.color = c2;
            // HDR emission so the bulb actually glows in the scene
            mat.SetColor("_EmissionColor", c2 * Mathf.Pow(2f, t * 4f));
            Debug.Log($"[Bulb] {componentId}: {voltageDrop:F3} V → {t * 100f:F0}%");
        }
    }

    // ─────────────────────────────────────────────────────────────
    // VR runtime controls
    // ─────────────────────────────────────────────────────────────

    public void IncrementResistance()
    {
        resistance = Mathf.Clamp(resistance + resistanceStep, minResistance, maxResistance);
        if (resistanceLabel != null)
            resistanceLabel.text = $"R: {resistance:F0} Ω";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void DecrementResistance()
    {
        resistance = Mathf.Clamp(resistance - resistanceStep, minResistance, maxResistance);
        if (resistanceLabel != null)
            resistanceLabel.text = $"R: {resistance:F0} Ω";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void IncrementMaxVoltage()
    {
        maxVoltage = Mathf.Clamp(maxVoltage + voltageStep, minMaxVoltage, maxMaxVoltage);
        if (maxVoltageLabel != null)
            maxVoltageLabel.text = $"Vmax: {maxVoltage:F1} V";
        UpdateLEDState(currentVoltage);
    }

    public void DecrementMaxVoltage()
    {
        maxVoltage = Mathf.Clamp(maxVoltage - voltageStep, minMaxVoltage, maxMaxVoltage);
        if (maxVoltageLabel != null)
            maxVoltageLabel.text = $"Vmax: {maxVoltage:F1} V";
        UpdateLEDState(currentVoltage);
    }
}

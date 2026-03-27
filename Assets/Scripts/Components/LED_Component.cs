using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class LED_Component : CircuitComponentBase
{
    [Header("LED Visual Settings")]
    public Renderer ledRenderer;
    public Color offColor = Color.black;
    public Color dimColor = new Color(1f, 0.2f, 0.2f);
    public Color brightColor = Color.white;

    [Header("Electrical Characteristics")]
    [Tooltip("Forward voltage used for Shockley Is calculation. Controls real diode behaviour in simulation.")]
    public float forwardVoltage = 2.0f;

    [Tooltip("Minimum measured voltage across LED before it visually turns ON. Keep below forwardVoltage for visible glow.")]
    public float onThreshold = 1.0f;      // visual ON threshold — separate from Shockley model Vf

    [Tooltip("Voltage at which the LED reaches full brightness.")]
    public float maxVoltage = 5.0f;

    public float currentVoltage = 0f;

    /// <summary>Public accessor used by the save system (ObjectState).</summary>
    public float CurrentVoltage
    {
        get => currentVoltage;
        set => currentVoltage = value;
    }

    [Header("UI Display")]
    public TMPro.TextMeshProUGUI forwardVoltageLabel;
    public TMPro.TextMeshProUGUI maxVoltageLabel;

    [Header("Runtime Adjustment")]
    public float voltageStep = 0.1f;
    public float minForwardVoltage = 0.1f;
    public float maxForwardVoltage = 5f;
    public float minMaxVoltage = 0.1f;
    public float maxMaxVoltage = 10f;

    protected override void Awake()
    {
        base.Awake();
        if (!ledRenderer)
            ledRenderer = GetComponentInChildren<Renderer>();
    }

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string modelName = $"D_{componentId}_model";
        string diodeName = $"D_{componentId}";

        // Derive saturation current from forwardVoltage via Shockley equation:
        // Is = If / exp(Vf / (N * Vt))
        const double N = 2.0;       // emission coefficient (2 = typical for LEDs)
        const double Vt = 0.02585;  // thermal voltage at room temperature (kT/q)
        const double If = 0.02;     // reference forward current (20 mA)
        double Is = If / System.Math.Exp((double)forwardVoltage / (N * Vt));
        // Clamp Is so the diode stays numerically stable in SpiceSharp.
        // 1e-30 causes near-zero Jacobian entries → convergence failure.
        // 1e-20 keeps Is large enough for Newton-Raphson while still giving
        // accurate Vf up to ~2.6 V; above that the model saturates gracefully.
        Is = System.Math.Max(Is, 1e-20);

        var model = new SpiceSharp.Components.DiodeModel(modelName);
        model.SetParameter("is", Is);
        model.SetParameter("n", N);
        model.SetParameter("rs", 10.0); // ~10 Ω bulk series resistance
        ckt.Add(model);

        // nodeA = anode (cap+ / supply side), nodeB = cathode — correct SPICE order
        var diode = new SpiceSharp.Components.Diode(diodeName, nodeA, nodeB, modelName);
        ckt.Add(diode);

        Debug.Log($"[LED] {componentId}: Vf={forwardVoltage:F2}V  Is={Is:E3}A  onThreshold={onThreshold:F2}V");
    }

    // ─────────────────────────────────────────────────────────────
    // Runtime VR controls
    // ─────────────────────────────────────────────────────────────

    public void IncrementForwardVoltage()
    {
        forwardVoltage = Mathf.Clamp(forwardVoltage + voltageStep, minForwardVoltage, maxForwardVoltage);
        if (forwardVoltageLabel != null)
            forwardVoltageLabel.text = $"Vf: {forwardVoltage:F1}V";
        UpdateLEDState(currentVoltage);
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void DecrementForwardVoltage()
    {
        forwardVoltage = Mathf.Clamp(forwardVoltage - voltageStep, minForwardVoltage, maxForwardVoltage);
        if (forwardVoltageLabel != null)
            forwardVoltageLabel.text = $"Vf: {forwardVoltage:F1}V";
        UpdateLEDState(currentVoltage);
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void IncrementMaxVoltage()
    {
        maxVoltage = Mathf.Clamp(maxVoltage + voltageStep, minMaxVoltage, maxMaxVoltage);
        if (maxVoltageLabel != null)
            maxVoltageLabel.text = $"Vmax: {maxVoltage:F1}V";
        UpdateLEDState(currentVoltage);
    }

    public void DecrementMaxVoltage()
    {
        maxVoltage = Mathf.Clamp(maxVoltage - voltageStep, minMaxVoltage, maxMaxVoltage);
        if (maxVoltageLabel != null)
            maxVoltageLabel.text = $"Vmax: {maxVoltage:F1}V";
        UpdateLEDState(currentVoltage);
    }

    // ─────────────────────────────────────────────────────────────
    // Called every simulation step by CircuitManager
    // voltageDrop = |V(nodeA) - V(nodeB)| across the LED terminals
    // ─────────────────────────────────────────────────────────────
    public void UpdateLEDState(float voltageDrop)
    {
        currentVoltage = voltageDrop;

        if (!ledRenderer)
            return;

        Material mat = ledRenderer.material;

        if (voltageDrop < onThreshold)
        {
            // LED is OFF
            mat.color = offColor;
            mat.SetColor("_EmissionColor", offColor);
            Debug.Log($"[LED] {componentId}: {voltageDrop:F3} V → OFF  (onThreshold={onThreshold:F2}V)");
        }
        else
        {
            // Brightness: 0% at onThreshold, 100% at maxVoltage
            float t = Mathf.Clamp01(Mathf.InverseLerp(onThreshold, maxVoltage, voltageDrop));
            Color c = Color.Lerp(dimColor, brightColor, t);
            mat.color = c;
            // HDR emission: doubles in stops as brightness increases
            mat.SetColor("_EmissionColor", c * Mathf.Pow(2f, t * 3f));
            Debug.Log($"[LED] {componentId}: {voltageDrop:F3} V → ON  {t * 100f:F0}%");
        }
    }
}

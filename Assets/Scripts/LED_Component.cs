using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class LED_Component : CircuitComponentBase
{
    [Header("Unity LED Visuals")]
    public Renderer glowRenderer;
    public Color offColor = Color.black;
    public Color onColor = Color.red;
    public float forwardVoltage = 2.0f; // cutoff for lighting in Unity

    private string modelName;

    protected override void Awake()
    {
        base.Awake();

        modelName = componentId + "_model";

        if (glowRenderer == null)
            glowRenderer = GetComponentInChildren<Renderer>();

        if (glowRenderer != null)
        {
            var mat = glowRenderer.material;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", offColor);
        }
    }

    public override void AddToSpice(Circuit ckt, string nodePos, string nodeNeg)
    {
        Debug.Log($"[LED] Adding diode {componentId} between {nodePos} and {nodeNeg}");

        // ⚠ IMPORTANT: Must define a model BEFORE creating diode entity
        var model = new DiodeModel(modelName);
        ckt.Add(model);

        var diode = new Diode(componentId, nodePos, nodeNeg, modelName);
        ckt.Add(diode);
    }

    public void UpdateLEDState(float voltageAcross)
    {
        bool isOn = voltageAcross >= forwardVoltage;

        if (glowRenderer != null)
        {
            var mat = glowRenderer.material;
            mat.SetColor("_EmissionColor", isOn ? onColor : offColor);
        }

        Debug.Log($"[LED] {componentId}: {voltageAcross:F3} V → {(isOn ? "ON" : "OFF")}");
    }
}

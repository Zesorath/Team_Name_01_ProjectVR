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
    public float forwardVoltage = 1.6f; // Threshold where LED begins lighting
    public float maxVoltage = 2.5f;     // Max brightness voltage

    private float currentVoltage = 0f;

    protected override void Awake()
    {
        base.Awake();
        if (!ledRenderer)
            ledRenderer = GetComponentInChildren<Renderer>();
    }

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        float fakeResistance = 1000f;
        // Example: treat LED as a test resistor for now
        string rName = $"R_{componentId}_LED";
        ckt.Add(new SpiceSharp.Components.Resistor(rName, nodeA, nodeB, fakeResistance));

        Debug.Log($"[LED] Adding TEST resistor for LED between {nodeA} and {nodeB}");
    }

   
    public void UpdateLEDState(float voltageDrop)
    {
        currentVoltage = voltageDrop;

        if (!ledRenderer)
            return;

        Material mat = ledRenderer.material;

        // OFF behavior
        if (voltageDrop < forwardVoltage)
        {
            mat.SetColor("_EmissionColor", offColor);
            ledRenderer.material.color = offColor;
            Debug.Log($"[LED] {componentId}: {voltageDrop:F3} V → OFF");
            return;
        }

        // ON behavior — scale brightness
        float t = Mathf.InverseLerp(forwardVoltage, maxVoltage, voltageDrop);
        Color finalColor = Color.Lerp(dimColor, brightColor, t);

        ledRenderer.material.color = finalColor;
        mat.SetColor("_EmissionColor", finalColor * 2f); // glow

        Debug.Log($"[LED] {componentId}: {voltageDrop:F3} V → ON ({t:P0})");
    }
    public float CurrentVoltage
    {
        get { return currentVoltage; }
        set
        {
            currentVoltage = value;
            UpdateLEDState(currentVoltage);
        }
    }

}

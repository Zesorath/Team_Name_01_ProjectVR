using TMPro;
using SpiceSharp;
using SpiceSharp.Components;
using UnityEngine;

public class MultimeterComponent : CircuitComponentBase
{
    [Header("Display")]
    public TMP_Text displayText;

    [Header("Electrical")]
    // Near-infinite resistance = near-ideal voltmeter (draws negligible current)
    public double internalResistance = 1e10;

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        ckt.Add(new Resistor(componentId + "_VM", nodeA, nodeB, internalResistance));
        Debug.Log($"[Multimeter] {componentId}: probing {nodeA} (COM) → {nodeB} (V+)");
    }

    // Called each simulation step with (V+ node voltage) - (COM node voltage)
    public void UpdateReading(float voltage)
    {
        if (displayText == null) return;
        string sign = voltage >= 0f ? "+" : "";
        displayText.text = $"{sign}{voltage:F2} V";
    }

    public void ClearReading()
    {
        if (displayText == null) return;
        displayText.text = "---";
    }
}

using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Ohms : CircuitComponentBase
{
    [Header("Resistor")]
    public float resistance = 2000f;
    [Header("UI Display")]
    public TMPro.TextMeshProUGUI resistanceLabel;

    [Header("Runtime Adjustment")]
    public float resistanceStep = 100f;
    public float minResistance = 1f;
    public float maxResistance = 100000f;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string rName = $"R_{componentId}";
        ckt.Add(new SpiceSharp.Components.Resistor(rName, nodeA, nodeB, resistance));
        Debug.Log($"[Resistor] {rName} => {nodeA} to {nodeB}, {resistance}Ω");
    }
    public void IncrementResistance()
    {
        resistance = Mathf.Clamp(resistance + resistanceStep, minResistance, maxResistance);
        if (resistanceLabel != null)
            resistanceLabel.text = $"{resistance:F0}Ω";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void DecrementResistance()
    {
        resistance = Mathf.Clamp(resistance - resistanceStep, minResistance, maxResistance);
        if (resistanceLabel != null)
            resistanceLabel.text = $"{resistance:F0}Ω";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

}

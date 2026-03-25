using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class CapacitorComponent : CircuitComponentBase
{
    [Header("Capacitor Settings")]
    public double capacitanceFarads = 0.001; // 1 mF

    [Header("UI Display")]
    public TMPro.TextMeshProUGUI capacitanceLabel;

    [Header("Runtime Adjustment")]
    public double capacitanceStep = 0.0001; // 0.1 mF per step
    public double minCapacitance = 0.000001; // 1 uF
    public double maxCapacitance = 1.0; // 1 F
    // seeded by CircuitManager across rebuilds
    [HideInInspector] public double initialVoltage = 0.0;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string cName = $"C_{componentId}";
        ckt.Add(new SpiceSharp.Components.Capacitor(cName, nodeA, nodeB, capacitanceFarads));
    }
    public void IncrementCapacitance()
    {
        capacitanceFarads = System.Math.Clamp(capacitanceFarads + capacitanceStep, minCapacitance, maxCapacitance);
        if (capacitanceLabel != null)
            capacitanceLabel.text = $"{capacitanceFarads * 1000000:F0}μF";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void DecrementCapacitance()
    {
        capacitanceFarads = System.Math.Clamp(capacitanceFarads - capacitanceStep, minCapacitance, maxCapacitance);
        if (capacitanceLabel != null)
            capacitanceLabel.text = $"{capacitanceFarads * 1000000:F0}μF";
        CircuitManager.Instance.NotifyConnectionChanged();
    }
}

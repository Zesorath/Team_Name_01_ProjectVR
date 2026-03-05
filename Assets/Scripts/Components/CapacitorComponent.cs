using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class CapacitorComponent : CircuitComponentBase
{
    [Header("Capacitor Settings")]
    public double capacitanceFarads = 0.001; // 1 mF

    // seeded by CircuitManager across rebuilds
    [HideInInspector] public double initialVoltage = 0.0;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string cName = $"C_{componentId}";
        ckt.Add(new SpiceSharp.Components.Capacitor(cName, nodeA, nodeB, capacitanceFarads));
    }
}

using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class CapacitorComponent : CircuitComponentBase
{
    [Header("Capacitor Settings")]
    public double capacitanceFarads = 0.001; // 1 mF

    // seeded by CircuitManager across rebuilds
    [HideInInspector] public double initialVoltage = 0.0;

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        var cap = new Capacitor($"{componentId}_C", nodeA, nodeB, capacitanceFarads);

        // Works in SpiceSharp via parameter system:
        cap.SetParameter("ic", initialVoltage);

        ckt.Add(cap);
    }
}

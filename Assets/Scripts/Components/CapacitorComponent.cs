using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class CapacitorComponent : CircuitComponentBase
{
    [Header("Capacitor Settings")]
    public double capacitanceFarads = 0.001; // 1 mF (big so you can see it)

    // We'll seed this when rebuilding to preserve charge across rebuilds
    [HideInInspector] public double initialVoltage = 0.0;

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        var cap = new Capacitor($"{componentId}_C", nodeA, nodeB, capacitanceFarads);

        // If SpiceSharp supports IC parameter on Capacitor in your version:
        // (If this line errors, tell me and I’ll give the alternate IC method.)
        cap.SetParameter("ic", initialVoltage);

        ckt.Add(cap);
    }
}

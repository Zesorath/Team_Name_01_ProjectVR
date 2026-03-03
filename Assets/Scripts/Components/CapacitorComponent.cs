using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class CapacitorComponent : CircuitComponentBase
{
    [Header("Capacitor Settings")]
    public double capacitanceFarads = 0.001; // 1 mF

    [HideInInspector]
    public double initialVoltage = 0.0;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string cName = $"C_{componentId}";

        var cap = new Capacitor(cName, nodeA, nodeB, capacitanceFarads);

        // Apply initial condition
        if (Mathf.Abs((float)initialVoltage) > 1e-9f)
        {
            cap.SetParameter("ic", initialVoltage);
            Debug.Log($"[Capacitor] {cName} IC = {initialVoltage}V");
        }

        ckt.Add(cap);
        var bleed = new Resistor(
    $"RBLEED_{componentId}",
    nodeA,
    nodeB,
    1e6   // 1 megaohm
);

        ckt.Add(bleed);
    }
}
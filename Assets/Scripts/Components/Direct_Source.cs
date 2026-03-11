using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class DCSource : CircuitComponentBase
{
    [Header("DC Source")]
    public float voltage = 5f;

    // nodeA = negative (0), nodeB = positive (N1)
    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        // Use the GameObject name as the Spice source name
        string sourceName = gameObject.name;

        var v = new VoltageSource(sourceName, nodeB, nodeA, voltage);
        ckt.Add(v);

        Debug.Log($"[DCSource] Adding Vsource: {sourceName} => {nodeA} to {nodeB}, {voltage}V");
    }
}

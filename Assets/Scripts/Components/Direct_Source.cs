using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class DCSource : CircuitComponentBase
{
    [Header("DC Source")]
    public float voltage = 5f;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string vName = $"V_{componentId}";
        ckt.Add(new SpiceSharp.Components.VoltageSource(vName, nodeA, nodeB, voltage));
        Debug.Log($"[DCSource] Adding Vsource: {vName} => {nodeA} to {nodeB}, {voltage}V");
    }
}

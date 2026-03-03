using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class DCSource : CircuitComponentBase
{
    [Header("DC Source")]
    public float voltage = 5f;

    [Tooltip("Turn source on/off without rebuilding topology.")]
    public bool isOn = true;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string vName = $"V_{componentId}";
        double output = isOn ? voltage : 0.0;

        ckt.Add(new VoltageSource(vName, nodeA, nodeB, output));

        Debug.Log($"[DCSource] {vName}: {nodeA}->{nodeB} = {output}V");
    }
}
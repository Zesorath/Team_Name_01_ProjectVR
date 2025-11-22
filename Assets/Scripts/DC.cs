using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class DCSource : CircuitComponentBase
{
    public float voltage = 5f;

    public override void AddToSpice(Circuit ckt, string posNode, string negNode)
    {
        Debug.Log($"[DCSource] Adding Vsource: {componentId} => {posNode} to {negNode}, {voltage}V");
        ckt.Add(new VoltageSource(componentId, posNode, negNode, voltage));
    }
}

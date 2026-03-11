using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Ohms : CircuitComponentBase
{
    [Header("Resistor")]
    public float resistance = 2000f;

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string rName = $"R_{componentId}";
        ckt.Add(new SpiceSharp.Components.Resistor(rName, nodeA, nodeB, resistance));
        Debug.Log($"[Resistor] {rName} => {nodeA} to {nodeB}, {resistance}Ω");
    }
}

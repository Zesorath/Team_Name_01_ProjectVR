using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Ohms : CircuitComponentBase
{
    public float resistance = 1000f;

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        Debug.Log($"[Resistor] {componentId} => {nodeA} to {nodeB}, {resistance}Ω");
        ckt.Add(new Resistor(componentId, nodeA, nodeB, resistance));
    }
}
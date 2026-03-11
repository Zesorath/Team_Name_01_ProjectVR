using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Ohms : CircuitComponentBase
{
    [Header("Resistor")]
    public float resistance = 2000f;

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        // Use GameObject name as resistor name
        string rName = gameObject.name;

        var r = new Resistor(rName, nodeA, nodeB, resistance);
        ckt.Add(r);

        Debug.Log($"[Resistor] {rName} => {nodeA} to {nodeB}, {resistance}Ω");
    }
}

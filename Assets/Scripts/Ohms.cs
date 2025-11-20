using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Ohms : CircuitComponent
{
    [Tooltip("Name of this resistor in the netlist")]
    public string resistorName = "R1";

    [Tooltip("Resistance in ohms")]
    public float resistance = 1000f;

    // Expect 2 terminals: [0], [1]
    public override void AddToSpice(Circuit circuit, string[] nodeNames)
    {
        string n1 = nodeNames.Length > 0 && !string.IsNullOrEmpty(nodeNames[0]) ? nodeNames[0] : "0";
        string n2 = nodeNames.Length > 1 && !string.IsNullOrEmpty(nodeNames[1]) ? nodeNames[1] : "0";

        Debug.Log($"[Ohms] Adding Resistor {resistorName} {n1} {n2} {resistance}");

        circuit.Add(new Resistor(resistorName, n1, n2, resistance));
    }
}

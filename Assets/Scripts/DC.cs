using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class Direct_Current : CircuitComponent
{
    [Tooltip("Name of this voltage source in the netlist")]
    public string sourceName = "V1";

    [Tooltip("DC voltage in volts")]
    public float voltage = 5.0f;

    // Expect 2 terminals: [0] = positive, [1] = negative (or ground)
    public override void AddToSpice(Circuit circuit, string[] nodeNames)
    {
        string pos = nodeNames.Length > 0 && !string.IsNullOrEmpty(nodeNames[0]) ? nodeNames[0] : "0";
        string neg = nodeNames.Length > 1 && !string.IsNullOrEmpty(nodeNames[1]) ? nodeNames[1] : "0";

        Debug.Log($"[Direct_Current] Adding VoltageSource {sourceName} {pos} {neg} {voltage}V");

        circuit.Add(new VoltageSource(sourceName, pos, neg, voltage));
    }
}

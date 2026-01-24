using UnityEngine;
using SpiceSharp;

/// <summary>
/// Represents a ground node in the VR scene. In SPICE, it just means node "0".
/// This component does NOT itself add any entity to the circuit.
/// </summary>
public class GroundNode : CircuitComponentBase
{
    // If CircuitComponentBase requires a 2-terminal implementation,
    // we still must override AddToSpice, but it's a NO-OP.
    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        // No entity to add – we just use node name "0" elsewhere.
        Debug.Log("[GroundNode] No Spice entity added, acts as reference node 0.");
    }
}

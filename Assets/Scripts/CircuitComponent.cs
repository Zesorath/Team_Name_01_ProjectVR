using UnityEngine;
using SpiceSharp;

public abstract class CircuitComponent : MonoBehaviour
{
    [Tooltip("Ports (sockets) on this component, in terminal index order")]
    public PortSocketBinder[] terminals;

    // Called by CircuitManager with the node names corresponding to each terminal
    public abstract void AddToSpice(Circuit circuit, string[] nodeNames);
}

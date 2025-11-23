using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Represents a single logical wire that can connect two components via two ends.
/// The ends themselves are handled by WireEnd + XRGrab, but this class is what
/// CircuitManager uses to build the adjacency graph.
/// </summary>
public class Wire : MonoBehaviour
{
    [Header("Wire Ends (assign in Inspector)")]
    public WireEnd endA;
    public WireEnd endB;

    // The components currently connected at each end (via sockets)
    [NonSerialized] public CircuitComponentBase compA;
    [NonSerialized] public CircuitComponentBase compB;

    public bool IsComplete => compA != null && compB != null;

    private void Awake()
    {
        // Make sure ends know their parent wire
        if (endA != null)
        {
            endA.parentWire = this;
            endA.endLabel = "A";
        }
        if (endB != null)
        {
            endB.parentWire = this;
            endB.endLabel = "B";
        }
    }

    public (CircuitComponentBase, CircuitComponentBase) GetConnectionPair()
    {
        return (compA, compB);
    }

    /// <summary>
    /// Called by a WireEnd when it gets plugged into a component socket.
    /// </summary>
    public void NotifyEndConnected(WireEnd end, CircuitComponentBase comp)
    {
        if (end == endA)
        {
            compA = comp;
            Debug.Log($"[Wire] {name}: endA -> {comp.componentId}");
        }
        else if (end == endB)
        {
            compB = comp;
            Debug.Log($"[Wire] {name}: endB -> {comp.componentId}");
        }
        else
        {
            Debug.LogWarning($"[Wire] {name}: Unknown end tried to connect.");
        }
    }

    /// <summary>
    /// Called by a WireEnd when it gets unplugged from a component socket.
    /// </summary>
    public void NotifyEndDisconnected(WireEnd end, CircuitComponentBase comp)
    {
        if (end == endA && compA == comp)
        {
            Debug.Log($"[Wire] {name}: endA disconnected from {comp.componentId}");
            compA = null;
        }
        else if (end == endB && compB == comp)
        {
            Debug.Log($"[Wire] {name}: endB disconnected from {comp.componentId}");
            compB = null;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[Wire] Destroyed, notifying CircuitManager.");
        if (CircuitManager.Instance != null)
            CircuitManager.Instance.NotifyConnectionChanged();
    }
}

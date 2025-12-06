using UnityEngine;
using System;

public class Wire : MonoBehaviour
{
    [Header("Wire Ends (assign in Inspector)")]
    public WireEnd startpoint;
    public WireEnd endpoint;

    [NonSerialized] public CircuitComponentBase compStart;
    [NonSerialized] public CircuitComponentBase compEnd;
    [NonSerialized] public PortSocketBinder portStart;
    [NonSerialized] public PortSocketBinder portEnd;
    public bool IsComplete => compStart != null && compEnd != null;

    public PortSocketBinder portA => portStart;
    public PortSocketBinder portB => portEnd;

    private void Awake()
    {
        if (startpoint != null)
        {
            startpoint.parentWire = this;
            startpoint.endLabel = "Start";
            startpoint.OnGrabStart += HandleGrabStart;
            startpoint.OnGrabEnd += HandleGrabEnd;
        }
        if (endpoint != null)
        {
            endpoint.parentWire = this;
            endpoint.endLabel = "End";
            endpoint.OnGrabStart += HandleGrabStart;
            endpoint.OnGrabEnd += HandleGrabEnd;
        }
    }

    public (CircuitComponentBase, CircuitComponentBase) GetConnectionPair()
    {
        return (compStart, compEnd);
    }

    public void NotifyEndConnected(WireEnd end, CircuitComponentBase comp, PortSocketBinder port)
    {
        if (end == startpoint)
        {
            compStart = comp;
            portStart = port;
            Debug.Log($"[Wire] {name}: startpoint -> {comp.componentId}, PortSocketBinder={port?.name}");
        }
        else if (end == endpoint)
        {
            compEnd = comp;
            portEnd = port;
            Debug.Log($"[Wire] {name}: endpoint -> {comp.componentId}, PortSocketBinder={port?.name}");
        }
        else
        {
            Debug.LogWarning($"[Wire] {name}: Unknown end tried to connect.");
        }
    }

    public void NotifyEndDisconnected(WireEnd end, CircuitComponentBase comp)
    {
        if (end == startpoint && compStart == comp)
        {
            Debug.Log($"[Wire] {name}: startpoint disconnected from {comp.componentId}");
            compStart = null;
        }
        else if (end == endpoint && compEnd == comp)
        {
            Debug.Log($"[Wire] {name}: endpoint disconnected from {comp.componentId}");
            compEnd = null;
        }
    }

    // Movement logic
    private void HandleGrabStart(WireEnd grabbedEnd)
    {
        // If neither end is plugged in, move the parent wire
        if (compStart == null && compEnd == null)
        {
            // Move parent wire (and both ends)
            grabbedEnd.SetMoveMode(WireEnd.MoveMode.ParentWire);
        }
        // If one end is plugged in, move only the free end
        else if ((grabbedEnd == startpoint && compStart == null) ||
                 (grabbedEnd == endpoint && compEnd == null))
        {
            grabbedEnd.SetMoveMode(WireEnd.MoveMode.FreeEnd);
        }
        else
        {
            // This end is plugged in, do not allow movement
            grabbedEnd.SetMoveMode(WireEnd.MoveMode.Locked);
        }
    }

    private void HandleGrabEnd(WireEnd grabbedEnd)
    {
        // Reset move mode when grab ends
        grabbedEnd.SetMoveMode(WireEnd.MoveMode.None);
    }

    private void OnDestroy()
    {
        Debug.Log("[Wire] Destroyed, notifying CircuitManager.");
        if (CircuitManager.Instance != null)
            CircuitManager.Instance.NotifyConnectionChanged();
    }
}

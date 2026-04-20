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



    private bool hasBeenGrabbed = false;

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

    // Keep the Wire parent positioned at the midpoint of its two ends.
    // This makes the ItemSpawner's distance check work correctly —
    // the spawner watches LastItem.transform.position, which is this
    // GameObject. Without this, the parent never moves even when the
    // ends are dragged, so the spawner never sees the wire leave and
    // never spawns a replacement.
    private void LateUpdate()
    {
        if (startpoint == null || endpoint == null) return;
        transform.position = (startpoint.transform.position + endpoint.transform.position) * 0.5f;
    }


    public (CircuitComponentBase, CircuitComponentBase) GetConnectionPair()
    {
        return (compStart, compEnd);
    }

    public void NotifyEndConnected(WireEnd end, CircuitComponentBase comp, PortSocketBinder port)
    {
        // Kill the wire end's velocity immediately on connection.
        // VelocityTracking leaves residual velocity on the Rigidbody at the moment
        // of snapping — without this, that velocity transfers as a physics impulse
        // to the component and causes it to spin.
        var endRb = end.GetComponent<Rigidbody>();
        if (endRb != null)
        {
            endRb.linearVelocity  = Vector3.zero;
            endRb.angularVelocity = Vector3.zero;
        }

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
        // First grab ever — unparent from the spawner so it doesn't stay
        // locked to the shelf, and the spawner can cleanly spawn a replacement.
        if (!hasBeenGrabbed)
        {
            hasBeenGrabbed = true;
            transform.SetParent(null, worldPositionStays: true);
            Debug.Log($"[Wire] {name}: unparented from spawner on first grab.");
        }

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
        grabbedEnd.SetMoveMode(WireEnd.MoveMode.None);

        // Kill velocity on both ends immediately so neither drifts after release.
        // The grabbed end gets zeroed in WireEnd.OnReleased, but the OTHER end
        // still has the matching velocity we assigned in FixedUpdate — stop it here.
        StopEnd(startpoint);
        StopEnd(endpoint);
    }

    private void StopEnd(WireEnd end)
    {
        if (end == null) return;
        var rb = end.GetComponent<Rigidbody>();
        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity  = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    private void OnDestroy()
    {
        Debug.Log("[Wire] Destroyed, notifying CircuitManager.");
        if (CircuitManager.Instance != null)
            CircuitManager.Instance.NotifyConnectionChanged();
    }
}

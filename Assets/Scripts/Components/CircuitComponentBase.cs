using UnityEngine;
using SpiceSharp;
using System.Collections.Generic;
using System.Linq;
using System;
public abstract class CircuitComponentBase : MonoBehaviour
{
    public string componentId;

    [Header("NEW: Terminal Clusters (supports parallel)")]
    public PortCluster terminalA;
    public PortCluster terminalB;

    [Header("OLD (optional): single-socket ports (legacy)")]
    public PortSocketBinder portA;
    public PortSocketBinder portB;

    protected virtual void Awake()
    {
        // Prefer SaveSystem's ComponentID label if present (unique & human-readable)
        var id = GetComponent<ComponentID>();
        if (id != null && !string.IsNullOrEmpty(id.label))
        {
            componentId = id.label;
            return;
        }

        // Fallback: ensure uniqueness even for spawned clones
        if (string.IsNullOrEmpty(componentId))
            componentId = gameObject.name;

        // Ensure clones don't collide (e.g., lots of "R1" or "RESISTOR(Clone)")
        componentId = $"{componentId}_{GetInstanceID()}";
    }


    public abstract void AddToSpice(Circuit ckt, string nodeA, string nodeB);

    // Auto-discover ports from children instead of manually assigning portA/portB

    public virtual IEnumerable<PortCluster> GetTerminals()
    {
        // Preferred: clusters
        if (terminalA != null && terminalB != null)
            return new[] { terminalA, terminalB };


        return new PortCluster[] { null, null };
    }
    public virtual IEnumerable<PortSocketBinder> GetPorts()
    { // If clusters exist, return one socket from each cluster
        if (terminalA != null && terminalB != null)
        {
            var a = terminalA.AnySocket();
            var b = terminalB.AnySocket();
            if (a == null || b == null)
                Debug.LogError($"{componentId}: terminalA/terminalB missing sockets.");
            return new[] { a, b };
        }
        // Otherwise use legacy ports
        if (portA == null || portB == null)
            Debug.LogError($"{componentId}: portA or portB is not assigned!");

        return new[] { portA, portB };
    }
}

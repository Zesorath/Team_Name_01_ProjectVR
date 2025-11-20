using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SpiceSharp;
using SpiceSharp.Simulations;
using SpiceSharp.Simulations.Base;
using SpiceSharp.Components;

public partial class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance { get; private set; }

    [Tooltip("Rebuild and run the circuit once on Start")]
    public bool rebuildOnStart = false;

    private Circuit lastCircuit;

    [Header("Voltage Measurement")]
    [Tooltip("Optional default port to measure (for future use)")]
    public PortSocketBinder measurementPort;

    // Terminal (port) -> node ID (1, 2, ...)
    private Dictionary<PortSocketBinder, int> lastTerminalToNodeId = new Dictionary<PortSocketBinder, int>();

    // Node name ("N1", "N2", ...) -> DC voltage in volts
    public Dictionary<string, double> nodeVoltages = new Dictionary<string, double>();

    // Overall circuit voltage = max |node voltage|
    [HideInInspector] public double overallVoltage = 0.0;
    public static double LatestOverallVoltage { get; private set; }
    public static event Action<double> OnVoltageUpdated;
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (rebuildOnStart)
            RebuildAndRun();
    }

    /// <summary>
    /// Called by PortSocketBinder when a wire connection is made or broken.
    /// </summary>
    public void OnConnectionChanged()
    {
        if (!isActiveAndEnabled)
            return;

        Debug.Log("[CircuitManager] Connection changed, rebuilding circuit.");
        RebuildAndRun();
    }

    /// <summary>
    /// Rebuild the circuit from the current scene and run a DC operating point.
    /// </summary>
    public void RebuildAndRun()
    {
        nodeVoltages.Clear();
        overallVoltage = 0.0;

        var circuit = BuildCircuitFromScene();
        lastCircuit = circuit;

        if (circuit == null)
        {
            Debug.LogWarning("[CircuitManager] No circuit built.");
            return;
        }

        // Build list of node names like "N1", "N2", ...
        var nodeNames = lastTerminalToNodeId.Values
            .Distinct()
            .Select(id => $"N{id}")
            .ToList();

        RunDcOp(circuit, nodeNames);
    }

    /// <summary>
    /// Scan the scene for components, wires, and ports, build a SpiceSharp circuit.
    /// </summary>
    private Circuit BuildCircuitFromScene()
    {
        // Find all circuit components in the scene
        var components = FindObjectsByType<CircuitComponent>(FindObjectsSortMode.None);
        if (components == null || components.Length == 0)
        {
            Debug.LogWarning("[CircuitManager] No CircuitComponent instances found in scene.");
            return null;
        }

        // Collect all terminals (ports) referenced by components
        var allTerminals = new HashSet<PortSocketBinder>();
        foreach (var comp in components)
        {
            if (comp == null || comp.terminals == null)
                continue;

            foreach (var t in comp.terminals)
            {
                if (t != null)
                    allTerminals.Add(t);
            }
        }

        if (allTerminals.Count == 0)
        {
            Debug.LogWarning("[CircuitManager] No terminals found in scene.");
            return null;
        }

        // Build adjacency between terminals based on wires
        var adjacency = new Dictionary<PortSocketBinder, List<PortSocketBinder>>();
        foreach (var t in allTerminals)
            adjacency[t] = new List<PortSocketBinder>();

        var wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);
        if (wires != null)
        {
            foreach (var wire in wires)
            {
                if (wire == null) continue;

                var a = wire.endA != null ? wire.endA.pluggedInto : null;
                var b = wire.endB != null ? wire.endB.pluggedInto : null;

                if (a != null && b != null && a != b)
                {
                    if (!adjacency.ContainsKey(a))
                        adjacency[a] = new List<PortSocketBinder>();
                    if (!adjacency.ContainsKey(b))
                        adjacency[b] = new List<PortSocketBinder>();

                    adjacency[a].Add(b);
                    adjacency[b].Add(a);
                }
            }
        }

        // Flood-fill to assign node IDs to connected terminals
        var terminalToNodeId = new Dictionary<PortSocketBinder, int>();
        int nextNodeId = 1;

        foreach (var t in allTerminals)
        {
            if (terminalToNodeId.ContainsKey(t))
                continue;

            // Only start a node if this terminal is actually connected to something
            if (!adjacency.TryGetValue(t, out var neighbors) || neighbors.Count == 0)
                continue;

            var queue = new Queue<PortSocketBinder>();
            queue.Enqueue(t);
            terminalToNodeId[t] = nextNodeId;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!adjacency.TryGetValue(current, out var neighs))
                    continue;

                foreach (var n in neighs)
                {
                    if (!terminalToNodeId.ContainsKey(n))
                    {
                        terminalToNodeId[n] = nextNodeId;
                        queue.Enqueue(n);
                    }
                }
            }

            nextNodeId++;
        }

        lastTerminalToNodeId = new Dictionary<PortSocketBinder, int>(terminalToNodeId);

        nodeVoltages.Clear();
        overallVoltage = 0.0;

        if (terminalToNodeId.Count == 0)
        {
            Debug.LogWarning("[CircuitManager] No wired nodes found (no connections via wires).");
        }

        // Build the SpiceSharp circuit
        var circuit = new Circuit();

        foreach (var comp in components)
        {
            if (comp == null)
                continue;

            var termArray = comp.terminals;
            if (termArray == null || termArray.Length == 0)
                continue;

            var nodeNames = new string[termArray.Length];
            for (int i = 0; i < termArray.Length; i++)
            {
                var t = termArray[i];
                if (t != null && terminalToNodeId.TryGetValue(t, out int nodeId))
                    nodeNames[i] = $"N{nodeId}";
                else
                    nodeNames[i] = "0"; // unconnected -> ground
            }

            comp.AddToSpice(circuit, nodeNames);
        }

        Debug.Log($"[CircuitManager] Built circuit with {components.Length} components and {Mathf.Max(0, nextNodeId - 1)} nodes.");
        return circuit;
    }

    /// <summary>
    /// Run a DC operating point and capture voltages for all nodes.
    /// </summary>
    private void RunDcOp(Circuit circuit, List<string> measurementNodes)
    {
        if (circuit == null)
        {
            Debug.LogWarning("[CircuitManager] No circuit built, skipping DC OP.");
            overallVoltage = 0f;
            OnVoltageUpdated?.Invoke(overallVoltage);
            return;
        }

        if (measurementNodes == null || measurementNodes.Count == 0)
        {
            Debug.Log("[CircuitManager] OP run, but no nodes to measure.");
            overallVoltage = 0f;
            OnVoltageUpdated?.Invoke(overallVoltage);
            return;
        }

        // Clean up the node list
        var distinctNodes = measurementNodes
            .Where(n => !string.IsNullOrWhiteSpace(n))
            .Distinct()
            .ToList();

        if (distinctNodes.Count == 0)
        {
            Debug.Log("[CircuitManager] OP run, but all measurement nodes were null/empty.");
            overallVoltage = 0f;
            OnVoltageUpdated?.Invoke(overallVoltage);
            return;
        }

        Debug.Log($"[CircuitManager] OP starting. Raw measurementNodes count = {measurementNodes.Count}, distinct = {distinctNodes.Count}, nodes = [{string.Join(", ", distinctNodes)}]");

        // "Furthest" node = last in the list (for your series chain mental model)
        string furthestNode = distinctNodes[distinctNodes.Count - 1];
        Debug.Log($"[CircuitManager] OP: using '{furthestNode}' as furthest node for overall voltage.");

        var op = new OP("dc-op");

        // Create exports for all measurement nodes (vs ground "0")
        var exports = new Dictionary<string, RealVoltageExport>();
        foreach (var node in distinctNodes)
        {
            try
            {
                var export = new RealVoltageExport(op, node, "0");
                exports[node] = export;
                Debug.Log($"[CircuitManager] OP: created voltage export for node '{node}' vs ground.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[CircuitManager] OP: failed to create export for node '{node}': {e.Message}");
            }
        }

        if (exports.Count == 0)
        {
            Debug.LogWarning("[CircuitManager] OP run, but no valid RealVoltageExport objects were created.");
            overallVoltage = 0f;
            OnVoltageUpdated?.Invoke(overallVoltage);
            return;
        }

        try
        {
            Debug.Log("[CircuitManager] OP: running SpiceSharp operating point analysis...");
            op.Run(circuit);
            Debug.Log("[CircuitManager] OP: SpiceSharp run complete, reading node voltages...");

            double furthestVoltage = 0.0;
            double maxAbs = 0.0;

            for (int i = 0; i < distinctNodes.Count; i++)
            {
                string name = distinctNodes[i];

                if (!exports.TryGetValue(name, out var export))
                {
                    Debug.LogWarning($"[CircuitManager] OP: node '{name}' has no associated export.");
                    continue;
                }

                double v = export.Value;  // This is V(node) - V(0)
                Debug.Log($"[CircuitManager] OP: Node[{i}] '{name}' = {v:F4} V");

                // Track "furthest" node by position
                if (name == furthestNode)
                {
                    furthestVoltage = v;
                }

                // Also track the largest |V| just for sanity / potential future use
                double abs = Math.Abs(v);
                if (abs > maxAbs)
                    maxAbs = abs;
            }

            Debug.Log($"[CircuitManager] OP: -> Furthest node '{furthestNode}' current voltage = {furthestVoltage:F4} V");

            overallVoltage = (float)furthestVoltage;
            OnVoltageUpdated?.Invoke(overallVoltage);

            Debug.Log($"[CircuitManager] OP done. Furthest node '{furthestNode}' = {overallVoltage:F4} V, overallVoltage set.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CircuitManager] DC OP failed: {ex}");
            overallVoltage = 0f;
            OnVoltageUpdated?.Invoke(overallVoltage);
        }
    }
}
/// <summary>
/// Get the DC voltage at a specific port (socket), if it belongs to a wired node.
/// </summary>


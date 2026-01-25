using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using SpiceSharp;
using SpiceSharp.Simulations;

/// <summary>
/// Graph-based circuit manager with Spice integration:
/// - Detects closed loops.
/// - If a loop contains a DC source, builds a Spice circuit
///   and solves actual voltages instead of fake logic.
/// </summary>
public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    public float lastVoltage = 0f;

    // keep graph accessible for Spice building
    private Dictionary<CircuitComponentBase, List<CircuitComponentBase>> _graph =
        new Dictionary<CircuitComponentBase, List<CircuitComponentBase>>();
  
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RebuildAndSimulate();
    }

    public void NotifyConnectionChanged()
    {
        Debug.Log("[CircuitManager] Connection changed — rebuilding...");
        RebuildAndSimulate();
    }
    private string GetNodeName(
    CircuitComponentBase comp,
    Dictionary<CircuitComponentBase, string> nodeNames,
    ref int nextId)
    {
        // All GroundNode instances are mapped to Spice node "0"
        if (comp is GroundNode)
            return "0";

        if (!nodeNames.TryGetValue(comp, out var name))
        {
            name = "N" + nextId++;
            nodeNames[comp] = name;
        }

        return name;
    }
    private bool _isRebuilding = false;

    private void RebuildAndSimulate()
    {
        if (_isRebuilding) return;
        _isRebuilding = true;
        try
        {
            var wires = Object.FindObjectsByType<Wire>(FindObjectsSortMode.None);
            var allPorts = Object.FindObjectsByType<PortSocketBinder>(FindObjectsSortMode.None);
            var allComponents = Object.FindObjectsByType<CircuitComponentBase>(FindObjectsSortMode.None);

            // 1. Build node groups using union-find
            var nodeGroups = new DisjointSet<PortSocketBinder>();

            // 1a. Union complete wires
            foreach (var wire in wires)
            {
                if (!wire || !wire.IsComplete) continue;

                var (a, b) = (wire.portA, wire.portB);
                if (a != null && b != null)
                    nodeGroups.Union(a, b);
            }

            // 1b. Ensure all ports exist in the disjoint set
            foreach (var p in allPorts)
            {
                if (p != null)
                    nodeGroups.Find(p); // auto-add singleton ports
            }

            // 1c. Treat CLOSED switches like wires
            foreach (var sw in allComponents.OfType<SwitchComponent>())
            {
                if (!sw.IsClosed)
                    continue;

                var ports = sw.GetPorts().ToArray();
                if (ports.Length < 2 || ports[0] == null || ports[1] == null)
                    continue;

                nodeGroups.Union(ports[0], ports[1]);
            }

            // 2. Assign node names (deep snapshot to avoid modification during enumeration)
            var nodeNameMap = new Dictionary<PortSocketBinder, string>();
            int nextNodeId = 1;
            var groupsSnapshot = nodeGroups.GroupsSnapshot(); // <-- Use the new method here
            foreach (var group in groupsSnapshot)
            {
                // If any port in the group is attached to a GroundNode, use "0"
                bool isGround = group.Any(port =>
                {
                    var comp = port.GetComponentInParent<GroundNode>();
                    return comp != null;
                });
                string nodeName = isGround ? "0" : $"N{nextNodeId++}";
                foreach (var port in group)
                    nodeNameMap[port] = nodeName;
            }

            // 3. For each component, get its ports and node names
            var spiceComponents = new List<(CircuitComponentBase comp, string nodeA, string nodeB)>();
            foreach (var comp in allComponents)
            {
                // Skip OPEN switches so they don't connect the graph/netlist
                if (comp is SwitchComponent sw && !sw.IsClosed)
                    continue;

                var ports = comp.GetPorts().ToArray();
                if (ports.Length < 2) continue;

                if (!nodeNameMap.TryGetValue(ports[0], out var nodeA) ||
                    !nodeNameMap.TryGetValue(ports[1], out var nodeB))
                    continue;

                // Optional: if a CLOSED switch merged both ports into same node, nodeA == nodeB.
                // Skipping avoids weird "component between same node" situations.
                if (nodeA == nodeB)
                    continue;

                spiceComponents.Add((comp, nodeA, nodeB));
            }

            // Debug: Print port assignments
            foreach (var comp in allComponents)
            {
                var ports = comp.GetPorts().ToArray();
                Debug.Log($"[DEBUG] {comp.componentId}: portA={ports.ElementAtOrDefault(0)?.name}, portB={ports.ElementAtOrDefault(1)?.name}");
            }

            // Debug: Print nodeNameMap snapshot
            var nodeNameMapSnapshot = nodeNameMap.ToList();
            foreach (var kvp in nodeNameMapSnapshot)
            {
                Debug.Log($"[NODEMAP_ID] PortSocketBinder {kvp.Key.name}({kvp.Key.GetInstanceID()}) => Node {kvp.Value}");
            }

            // Debug: Print component node mapping
            foreach (var comp in allComponents)
            {
                var ports = comp.GetPorts().ToArray();
                if (ports.Length < 2) continue;
                string nodeA = nodeNameMap.ContainsKey(ports[0]) ? nodeNameMap[ports[0]] : "MISSING";
                string nodeB = nodeNameMap.ContainsKey(ports[1]) ? nodeNameMap[ports[1]] : "MISSING";
                Debug.Log($"[CHECK] {comp.componentId}: portA={ports[0]?.name}({nodeA}), portB={ports[1]?.name}({nodeB})");
            }
            foreach (var comp in allComponents)
            {
                var ports = comp.GetPorts().ToArray();
                if (ports.Length < 2) continue;
                string idA = ports[0] ? ports[0].GetInstanceID().ToString() : "null";
                string idB = ports[1] ? ports[1].GetInstanceID().ToString() : "null";
                Debug.Log($"[CHECK_ID] {comp.componentId}: portA={ports[0]?.name}({idA}), portB={ports[1]?.name}({idB})");
            }

            // 4. Group by connectivity (BFS as before)
            _graph.Clear();
            foreach (var (comp, nodeA, nodeB) in spiceComponents)
            {
                if (!_graph.ContainsKey(comp)) _graph[comp] = new List<CircuitComponentBase>();
                // Find neighbors by shared node
                foreach (var (other, otherA, otherB) in spiceComponents)
                {
                    if (other == comp) continue;
                    if (nodeA == otherA || nodeA == otherB || nodeB == otherA || nodeB == otherB)
                        _graph[comp].Add(other);
                }
            }

            // 5. Find groups and simulate as before
            var globalVisited = new HashSet<CircuitComponentBase>();
            TurnAllLedsOff();
            float highestVoltageSeen = 0f;

            foreach (var start in _graph.Keys)
            {
                if (globalVisited.Contains(start))
                    continue;

                // BFS to get component group
                var group = new List<CircuitComponentBase>();
                var q = new Queue<CircuitComponentBase>();
                q.Enqueue(start);
                globalVisited.Add(start);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    group.Add(cur);

                    if (_graph.TryGetValue(cur, out var neigh))
                    {
                        foreach (var n in neigh)
                            if (globalVisited.Add(n))
                                q.Enqueue(n);
                    }
                }

                // Does group have power?
                var dc = group.OfType<DCSource>().FirstOrDefault();
                bool hasCycle = HasCycleInGroup(group, _graph);

                Debug.Log($"[CircuitManager] Group: {group.Count} items, DC={dc != null}, Cycle={hasCycle}");

                if (dc == null || !hasCycle)
                    continue;

                RunSpiceForGroupNetlist(group, spiceComponents, nodeNameMap);

                highestVoltageSeen = Mathf.Max(highestVoltageSeen, lastVoltage);
            }

            lastVoltage = highestVoltageSeen;
            Debug.Log($"[CircuitManager] Simulation complete — lastVoltage={lastVoltage:F3}V");
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    // New netlist-based SPICE runner
    void RunSpiceForGroupNetlist(List<CircuitComponentBase> group, List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents, Dictionary<PortSocketBinder, string> nodeNameMap)
    {
        Debug.Log($"[CircuitManager] Running SPICE for group of {group.Count} components...");

        var ckt = new Circuit();

        // Add all group components to the circuit
        foreach (var (comp, nodeA, nodeB) in spiceComponents)
        {
            if (!group.Contains(comp)) continue;
            if (comp is GroundNode) continue;
            comp.AddToSpice(ckt, nodeA, nodeB);
        }

        // --- DEBUG: Print Netlist ---
        Debug.Log("[SPICE NETLIST]");
        foreach (var entity in ckt)
            Debug.Log(entity.ToString());

        // Find a DC source in the group
        var dcComponent = group.OfType<DCSource>().FirstOrDefault();
        if (dcComponent == null) return;

        double vdc = dcComponent.voltage;
        var dc = new DC("dc", dcComponent.gameObject.name, vdc, vdc, 0.1);

        try
        {
            foreach (var _ in dc.Run(ckt))
            {
                // Print voltages at each node
                Debug.Log("[SPICE NODE VOLTAGES]");
                var groupPorts = group
                .SelectMany(c => c.GetPorts())
                .Where(p => p != null)
                .Distinct();

                var groupNodeNames = groupPorts
                    .Where(p => nodeNameMap.ContainsKey(p))
                    .Select(p => nodeNameMap[p])
                    .Distinct();

                Debug.Log("[SPICE NODE VOLTAGES]");
                foreach (var node in groupNodeNames)
                {
                    // node should exist in this circuit now
                    double v = dc.GetVoltage(node);
                    Debug.Log($"{node}: {v:F6} V");
                }

                // Update LEDs (if any)
                foreach (var led in group.OfType<LED_Component>())
                {
                    var ports = led.GetPorts().ToArray();
                    if (ports.Length < 2) continue;
                    if (!nodeNameMap.TryGetValue(ports[0], out var nodeA) || !nodeNameMap.TryGetValue(ports[1], out var nodeB))
                        continue;
                    double vA = dc.GetVoltage(nodeA);
                    double vB = dc.GetVoltage(nodeB);
                    float ledDrop = (float)Mathf.Abs((float)(vA - vB));
                    led.UpdateLEDState(ledDrop);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SPICE ERROR] {ex.Message}");
            // Don't force lastVoltage to 0 here; printing a missing node shouldn't kill the solve.
        }

    }



    private List<CircuitComponentBase> BuildOrderedLoop(List<CircuitComponentBase> group, DCSource dc)
    {
        var visited = new HashSet<CircuitComponentBase>();
        var path = new List<CircuitComponentBase>();

        if (!_graph.TryGetValue(dc, out var neighbors) || neighbors.Count == 0)
            return null;

        CircuitComponentBase cur = dc;
        CircuitComponentBase prev = null;

        while (true)
        {
            visited.Add(cur);
            path.Add(cur);

            if (!_graph.TryGetValue(cur, out var neigh)) break;

            CircuitComponentBase next =
                neigh.FirstOrDefault(n => group.Contains(n) && n != prev);

            if (next == null) break;

            if (next == dc)
            {
               
                break;
            }

            prev = cur;
            cur = next;

            if (visited.Contains(cur))
                break;
        }

        return path;
    }

    private void TurnAllLedsOff()
    {
        foreach (var l in Object.FindObjectsByType<LED_Component>(FindObjectsSortMode.None))
            l.UpdateLEDState(0f);
    }

    private bool HasCycleInGroup(List<CircuitComponentBase> group,
        Dictionary<CircuitComponentBase, List<CircuitComponentBase>> graph)
    {
        var visited = new HashSet<CircuitComponentBase>();

        foreach (var node in group)
        {
            if (!visited.Contains(node) &&
                DFS(node, null, visited, graph, group))
                return true;
        }
        return false;
    }

    private bool DFS(CircuitComponentBase cur,
        CircuitComponentBase parent,
        HashSet<CircuitComponentBase> visited,
        Dictionary<CircuitComponentBase, List<CircuitComponentBase>> graph,
        List<CircuitComponentBase> group)
    {
        visited.Add(cur);

        if (!graph.TryGetValue(cur, out var neigh))
            return false;

        foreach (var n in neigh)
        {
            if (!group.Contains(n)) continue;
            if (n == parent) continue;

            if (visited.Contains(n)) return true;
            if (DFS(n, cur, visited, graph, group)) return true;
        }
        return false;
    }
}

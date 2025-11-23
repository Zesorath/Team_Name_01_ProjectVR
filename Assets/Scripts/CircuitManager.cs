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
    private void RebuildAndSimulate()
    {
        var wires = Object.FindObjectsByType<Wire>(FindObjectsSortMode.None);
        Debug.Log($"[CircuitManager] Rebuild: found {wires.Length} wires.");

        _graph.Clear();

        foreach (var w in wires)
        {
            if (!w || !w.IsComplete)
                continue;

            var (a, b) = w.GetConnectionPair();
            if (a == null || b == null)
                continue;

            if (!_graph.TryGetValue(a, out var listA))
                _graph[a] = listA = new List<CircuitComponentBase>();

            if (!_graph.TryGetValue(b, out var listB))
                _graph[b] = listB = new List<CircuitComponentBase>();

            if (!listA.Contains(b)) listA.Add(b);
            if (!listB.Contains(a)) listB.Add(a);

            Debug.Log($"[CircuitManager] Graph edge: {a.componentId} ↔ {b.componentId}");
        }

        Debug.Log($"[CircuitManager] Nodes dict contains {_graph.Count} components.");

        if (_graph.Count < 2)
        {
            Debug.LogWarning("[CircuitManager] Need at least 2 connected components.");
            TurnAllLedsOff();
            lastVoltage = 0;
            return;
        }

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

            RunSpiceForGroup(group, dc);

            highestVoltageSeen = Mathf.Max(highestVoltageSeen, lastVoltage);
        }

        lastVoltage = highestVoltageSeen;
        Debug.Log($"[CircuitManager] Simulation complete — lastVoltage={lastVoltage:F3}V");
    }

    // ------------------------- SPICE ENGINE ----------------------------

    void RunSpiceForGroup(List<CircuitComponentBase> group, DCSource dcComponent)
    {
        Debug.Log($"[CircuitManager] Running SPICE for group of {group.Count} components...");

        var ckt = new Circuit();

        string ground = "0";
        string n1 = "N1";
        string n2 = "N2";

        // DC source 0 -> N1
        dcComponent.AddToSpice(ckt, ground, n1);

        // Resistors N1 -> N2
        foreach (var comp in group.OfType<Ohms>())
        {
            comp.AddToSpice(ckt, n1, n2);
        }

        // LED N2 -> 0
        foreach (var led in group.OfType<LED_Component>())
        {
            led.AddToSpice(ckt, n2, ground);
        }

        double vdc = dcComponent.voltage;
        var dc = new DC("dc", dcComponent.gameObject.name, vdc, vdc, 0.1);

        try
        {
            foreach (var _ in dc.Run(ckt))
            {
                double v0 = dc.GetVoltage(ground);
                double vN1 = dc.GetVoltage(n1);
                double vN2 = dc.GetVoltage(n2);

                Debug.Log($"[SPICE DEBUG] V(0)  = {v0:F6} V");
                Debug.Log($"[SPICE DEBUG] V(N1) = {vN1:F6} V");
                Debug.Log($"[SPICE DEBUG] V(N2) = {vN2:F6} V");

                float ledDrop = (float)(vN2 - v0);

                foreach (var led in group.OfType<LED_Component>())
                {
                    led.UpdateLEDState(ledDrop);
                    Debug.Log($"[SPICE] {led.gameObject.name} drop = {ledDrop:F6} V");
                }

                lastVoltage = ledDrop;
            }

            Debug.Log($"[CircuitManager] Simulation complete — lastVoltage={lastVoltage:F3}V");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SPICE ERROR] {ex.Message}");
            lastVoltage = 0f;
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

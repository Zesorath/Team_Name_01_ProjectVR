using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SpiceSharp;
using SpiceSharp.Simulations;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;

    [Header("Transient Settings")]
    public float timeStep = 0.01f;
    public float simDuration = 5.0f;
    public bool runTransient = true;

    public float lastVoltage = 0f;

    private Dictionary<CircuitComponentBase, List<CircuitComponentBase>> _graph =
        new Dictionary<CircuitComponentBase, List<CircuitComponentBase>>();

    private bool _isRebuilding = false;
    private Coroutine _transientRoutine;
    [Header("Cap Debug")]
    public bool debugCapacitor = true;
    public float capDebugPrintEverySeconds = 0.10f; // throttle so console doesn't explode
    private double _nextCapPrintTime = 0.0;

    // Persist capacitor voltage across rebuilds (by componentId)
    private readonly Dictionary<string, double> _capVoltageById = new Dictionary<string, double>();

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        NotifyConnectionChanged();
    }

    public void NotifyConnectionChanged()
    {
        Debug.Log("[CircuitManager] Connection changed — rebuilding...");

        if (_transientRoutine != null)
        {
            StopCoroutine(_transientRoutine);
            _transientRoutine = null;
        }

        RebuildAndSimulate();
    }

    private void RebuildAndSimulate()
    {
        if (_isRebuilding) return;
        _isRebuilding = true;

        try
        {
            var wires = UnityEngine.Object.FindObjectsByType<Wire>(FindObjectsSortMode.None);
            var allComponents = UnityEngine.Object.FindObjectsByType<CircuitComponentBase>(FindObjectsSortMode.None);

            // 1) Union-Find node groups from complete wires
            var nodeGroups = new DisjointSet<PortSocketBinder>();
            foreach (var wire in wires)
            {
                if (!wire || !wire.IsComplete) continue;
                var a = wire.portA;
                var b = wire.portB;
                if (a != null && b != null)
                    nodeGroups.Union(a, b);
            }

            // 2) Assign Spice node names to each port in each union-find set
            var nodeNameMap = new Dictionary<PortSocketBinder, string>();
            int nextNodeId = 1;

            var groupsSnapshot = nodeGroups.GroupsSnapshot();
            foreach (var group in groupsSnapshot)
            {
                bool isGround = group.Any(port =>
                {
                    return port != null && port.GetComponentInParent<GroundNode>() != null;
                });

                string nodeName = isGround ? "0" : $"N{nextNodeId++}";
                foreach (var port in group)
                    nodeNameMap[port] = nodeName;
            }

            // 3) Build typed list of (comp,nodeA,nodeB)
            var spiceComponents = new List<(CircuitComponentBase comp, string nodeA, string nodeB)>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                var ports = comp.GetPorts().ToArray();
                if (ports.Length < 2) continue;
                if (!ports[0] || !ports[1]) continue;

                if (!nodeNameMap.TryGetValue(ports[0], out var nodeA)) continue;
                if (!nodeNameMap.TryGetValue(ports[1], out var nodeB)) continue;

                spiceComponents.Add((comp, nodeA, nodeB));
            }

            // DEBUG: print port pairs
            foreach (var comp in allComponents)
            {
                var ports = comp.GetPorts().ToArray();
                if (ports.Length < 2) continue;
                Debug.Log($"[DEBUG] {comp.componentId}: portA={ports[0]?.name}, portB={ports[1]?.name}");
            }

            // 4) Connectivity graph by shared node name
            _graph.Clear();
            foreach (var entry in spiceComponents)
            {
                var comp = entry.comp;
                var nodeA = entry.nodeA;
                var nodeB = entry.nodeB;

                if (!_graph.ContainsKey(comp))
                    _graph[comp] = new List<CircuitComponentBase>();

                foreach (var otherEntry in spiceComponents)
                {
                    var other = otherEntry.comp;
                    if (other == comp) continue;

                    bool share =
                        nodeA == otherEntry.nodeA || nodeA == otherEntry.nodeB ||
                        nodeB == otherEntry.nodeA || nodeB == otherEntry.nodeB;

                    if (share)
                        _graph[comp].Add(other);
                }
            }

            // 5) Find connected groups
            TurnAllLedsOff();
            lastVoltage = 0f;

            var globalVisited = new HashSet<CircuitComponentBase>();
            var groups = new List<List<CircuitComponentBase>>();

            foreach (var start in _graph.Keys)
            {
                if (globalVisited.Contains(start)) continue;

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

                groups.Add(group);
            }

            // 6) Simulate any group with a DC source or a capacitor
            foreach (var group in groups)
            {
                bool hasDC = group.OfType<DCSource>().Any();
                bool hasCap = group.Any(c => c != null && c.GetType().Name == "CapacitorComponent");

                if (!hasDC && !hasCap) continue;

                Debug.Log($"[CircuitManager] Group: {group.Count} items, DC={hasDC}, CAP={hasCap}");

                if (runTransient)
                {
                    var ckt = BuildSpiceCircuitForGroup(group, spiceComponents);
                    if (ckt == null) continue;

                    // run one transient at a time
                    if (_transientRoutine != null)
                        StopCoroutine(_transientRoutine);

                    _transientRoutine = StartCoroutine(RunTransientForGroup(
                        ckt, group, spiceComponents, nodeNameMap
                    ));
                }
            }
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    private Circuit BuildSpiceCircuitForGroup(
        List<CircuitComponentBase> group,
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents)
    {
        var ckt = new Circuit();

        // Seed capacitor initial voltages (reflection so it won't break if you rename)
        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            if (_capVoltageById.TryGetValue(cap.componentId, out var v0))
            {
                var field = cap.GetType().GetField("initialVoltage");
                if (field != null && field.FieldType == typeof(double))
                    field.SetValue(cap, v0);
            }
        }

        // Add components
        foreach (var entry in spiceComponents)
        {
            var comp = entry.comp;
            if (comp == null) continue;
            if (!group.Contains(comp)) continue;
            if (comp is GroundNode) continue;

            try
            {
                comp.AddToSpice(ckt, entry.nodeA, entry.nodeB);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SPICE BUILD ERROR] {comp.componentId}: {ex.Message}");
            }
        }

        Debug.Log("[SPICE NETLIST]");
        foreach (var entity in ckt)
            Debug.Log(entity.ToString());

        return ckt;
    }

    private IEnumerator RunTransientForGroup(
        Circuit ckt,
        List<CircuitComponentBase> group,
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents,
        Dictionary<PortSocketBinder, string> nodeNameMap)
    {
        Debug.Log($"[CircuitManager] Running TRANSIENT for group of {group.Count} components...");

        var tran = new Transient("tran", timeStep, simDuration);
        IEnumerator enumerator = null;

        // Start Spice run inside try/catch, but DO NOT yield inside that try/catch block.
        try
        {
            enumerator = tran.Run(ckt).GetEnumerator();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SPICE TRANSIENT START ERROR] {ex.Message}");
            yield break;
        }

        double highestVSeen = 0.0;

        while (true)
        {
            bool movedNext = false;

            try
            {
                movedNext = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SPICE TRANSIENT ERROR] {ex.Message}");
                break;
            }

            if (!movedNext)
                break;

            // Update LEDs
            foreach (var led in group.OfType<LED_Component>())
            {
                var ports = led.GetPorts().ToArray();
                if (ports.Length < 2) continue;

                if (!nodeNameMap.TryGetValue(ports[0], out var nodeA)) continue;
                if (!nodeNameMap.TryGetValue(ports[1], out var nodeB)) continue;

                double vA, vB;

                try { vA = tran.GetVoltage(nodeA); }
                catch { continue; }

                try { vB = tran.GetVoltage(nodeB); }
                catch { continue; }

                float drop = Mathf.Abs((float)(vA - vB));
                led.UpdateLEDState(drop);

                highestVSeen = Math.Max(highestVSeen, Math.Abs(vA - vB));
            }

            // Save capacitor state each step
            SaveCapStates(tran, group, spiceComponents);
            DebugCapacitorsStep(tran, group, spiceComponents);
            // Yield outside of try/catch (fixes CS1626)
            yield return null;
        }

        lastVoltage = (float)highestVSeen;
        Debug.Log($"[CircuitManager] Transient done — lastVoltage={lastVoltage:F3}V");
    }

    private void SaveCapStates(
        Transient tran,
        List<CircuitComponentBase> group,
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents)
    {
        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            var entry = spiceComponents.FirstOrDefault(x => x.comp == cap);
            if (entry.comp == null) continue;

            try
            {
                double vA = tran.GetVoltage(entry.nodeA);
                double vB = tran.GetVoltage(entry.nodeB);
                _capVoltageById[cap.componentId] = vA - vB;
            }
            catch
            {
                // ignore missing node
            }
        }
    }

    private void TurnAllLedsOff()
    {
        foreach (var l in UnityEngine.Object.FindObjectsByType<LED_Component>(FindObjectsSortMode.None))
            l.UpdateLEDState(0f);
    }
    private double GetCapacitanceFarads(CircuitComponentBase cap)
    {
        if (cap == null) return 0.0;

        var t = cap.GetType();

        // Try common field names
        var f =
            t.GetField("capacitance") ??
            t.GetField("capacitanceFarads") ??
            t.GetField("C") ??
            t.GetField("c");

        if (f != null && f.FieldType == typeof(double))
            return (double)f.GetValue(cap);

        if (f != null && f.FieldType == typeof(float))
            return (float)f.GetValue(cap);

        // Try common property names
        var p =
            t.GetProperty("capacitance") ??
            t.GetProperty("capacitanceFarads") ??
            t.GetProperty("C");

        if (p != null && p.PropertyType == typeof(double))
            return (double)p.GetValue(cap);

        if (p != null && p.PropertyType == typeof(float))
            return (float)p.GetValue(cap);

        return 0.0;
    }

    private void DebugCapacitorsStep(
        Transient tran,
        List<CircuitComponentBase> group,
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents)
    {
        if (!debugCapacitor) return;

        // Throttle prints
        if (tran.Time < _nextCapPrintTime) return;
        _nextCapPrintTime = tran.Time + capDebugPrintEverySeconds;

        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            var entry = spiceComponents.FirstOrDefault(x => x.comp == cap);
            if (entry.comp == null) continue;

            double vA, vB;
            try
            {
                vA = tran.GetVoltage(entry.nodeA);
                vB = tran.GetVoltage(entry.nodeB);
            }
            catch
            {
                continue;
            }

            double vcap = vA - vB;                 // capacitor voltage (nodeA - nodeB)
            double cF = GetCapacitanceFarads(cap);  // Farads
            double qC = cF * vcap;                 // Coulombs
            double eJ = 0.5 * cF * vcap * vcap;    // Joules

            Debug.Log($"[CAP] t={tran.Time:F3}s  {cap.componentId}  Vcap={vcap:F4}V  C={cF:E3}F  Q={qC:E3}C  E={eJ:E3}J  ({entry.nodeA}-{entry.nodeB})");
        }
    }
}

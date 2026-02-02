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
    private readonly Dictionary<string, double> _capIcById = new();
    public float lastVoltage = 0f;

    private readonly Dictionary<CircuitComponentBase, List<CircuitComponentBase>> _graph =
        new Dictionary<CircuitComponentBase, List<CircuitComponentBase>>();

    private bool _isRebuilding = false;
    private Coroutine _transientRoutine;

    [Header("Cap Debug")]
    public bool debugCapacitor = true;
    public float capDebugPrintEverySeconds = 0.10f;
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

            // Collect all clusters and sockets
            var allClusters = new List<PortCluster>();
            var allSockets = new List<PortSocketBinder>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                foreach (var t in comp.GetTerminals())
                {
                    if (t == null) continue;
                    allClusters.Add(t);
                    allSockets.AddRange(t.GetSockets());
                }
            }

            // Union-Find over sockets
            var nodeGroups = new DisjointSet<PortSocketBinder>();

            // (0) Ensure every socket exists in DS even if it has no wire yet
            foreach (var s in allSockets)
            {
                if (s == null) continue;
                nodeGroups.Find(s);
            }

            // (A) Union sockets inside each cluster => parallel-ready terminal
            foreach (var cluster in allClusters)
            {
                var sockets = cluster.GetSockets().Where(x => x != null).ToList();
                if (sockets.Count == 0) continue;

                for (int i = 1; i < sockets.Count; i++)
                    nodeGroups.Union(sockets[0], sockets[i]);
            }

            // (B) Union wire endpoints
            foreach (var wire in wires)
            {
                if (!wire || !wire.IsComplete) continue;

                var a = wire.portA;
                var b = wire.portB;

                if (a != null && b != null)
                    nodeGroups.Union(a, b);
            }

            // (2) Assign SPICE node names to each socket group
            var nodeNameBySocket = new Dictionary<PortSocketBinder, string>();
            int nextNodeId = 1;

            foreach (var group in nodeGroups.GroupsSnapshot())
            {
                bool isGround = group.Any(sock =>
                    sock != null && sock.GetComponentInParent<GroundNode>() != null);

                string nodeName = isGround ? "0" : $"N{nextNodeId++}";
                foreach (var sock in group)
                {
                    if (sock != null)
                        nodeNameBySocket[sock] = nodeName;
                }
            }

            // (3) Build list of (comp,nodeA,nodeB) using terminalA/terminalB clusters
            var spiceComponents = new List<(CircuitComponentBase comp, string nodeA, string nodeB)>();

            foreach (var comp in allComponents)
            {
                if (comp == null) continue;

                var terms = comp.GetTerminals().Where(t => t != null).ToArray();
                if (terms.Length < 2) continue;

                var aSock = terms[0].AnySocket();
                var bSock = terms[1].AnySocket();
                if (aSock == null || bSock == null) continue;

                if (!nodeNameBySocket.TryGetValue(aSock, out var nodeA)) continue;
                if (!nodeNameBySocket.TryGetValue(bSock, out var nodeB)) continue;

                spiceComponents.Add((comp, nodeA, nodeB));
            }

            // DEBUG: print terminal mapping
            foreach (var entry in spiceComponents)
                Debug.Log($"[DEBUG] {entry.comp.componentId}: {entry.nodeA} <-> {entry.nodeB}");

            // (4) Connectivity graph by shared node name
            _graph.Clear();
            foreach (var entry in spiceComponents)
            {
                if (!_graph.ContainsKey(entry.comp))
                    _graph[entry.comp] = new List<CircuitComponentBase>();

                foreach (var other in spiceComponents)
                {
                    if (other.comp == entry.comp) continue;

                    bool share =
                        entry.nodeA == other.nodeA || entry.nodeA == other.nodeB ||
                        entry.nodeB == other.nodeA || entry.nodeB == other.nodeB;

                    if (share)
                        _graph[entry.comp].Add(other.comp);
                }
            }

            // (5) Find connected groups
            TurnAllLedsOff();
            lastVoltage = 0f;

            var visited = new HashSet<CircuitComponentBase>();
            var groups = new List<List<CircuitComponentBase>>();

            foreach (var start in _graph.Keys)
            {
                if (!visited.Add(start)) continue;

                var group = new List<CircuitComponentBase>();
                var q = new Queue<CircuitComponentBase>();
                q.Enqueue(start);

                while (q.Count > 0)
                {
                    var cur = q.Dequeue();
                    group.Add(cur);

                    if (_graph.TryGetValue(cur, out var neigh))
                    {
                        foreach (var n in neigh)
                            if (visited.Add(n))
                                q.Enqueue(n);
                    }
                }

                groups.Add(group);
            }

            // (6) Pick ONE best group to simulate (otherwise the last group stops the good one)
            List<CircuitComponentBase> bestGroup = null;
            int bestScore = int.MinValue;

            foreach (var group in groups)
            {
                bool hasDC = group.OfType<DCSource>().Any();
                bool hasCap = group.Any(c => c != null && c.GetType().Name == "CapacitorComponent");
                bool hasLed = group.OfType<LED_Component>().Any();

                // We only care about groups that could produce something meaningful
                if (!hasDC && !hasCap) continue;

                // Ignore degenerate groups like "DC source by itself"
                if (group.Count < 2) continue;

                // Score groups so we choose the most interesting one
                int score = 0;
                score += group.Count;          // bigger group = usually the real circuit
                if (hasDC) score += 10;
                if (hasLed) score += 50;
                if (hasCap) score += 100;      // prioritize capacitor groups

                if (score > bestScore)
                {
                    bestScore = score;
                    bestGroup = group;
                }
            }

            if (bestGroup != null)
            {
                bool hasDC = bestGroup.OfType<DCSource>().Any();
                bool hasCap = bestGroup.Any(c => c != null && c.GetType().Name == "CapacitorComponent");

                Debug.Log($"[CircuitManager] Simulating BEST group: {bestGroup.Count} items, DC={hasDC}, CAP={hasCap}");

                if (runTransient)
                {
                    var ckt = BuildSpiceCircuitForGroup(bestGroup, spiceComponents);
                    if (ckt != null)
                    {
                        if (_transientRoutine != null)
                            StopCoroutine(_transientRoutine);

                        _transientRoutine = StartCoroutine(RunTransientForGroup(
                            ckt, bestGroup, spiceComponents
                        ));
                    }
                }
            }
            else
            {
                Debug.Log("[CircuitManager] No valid groups to simulate.");
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

        // Seed capacitor initial voltages
        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            if (_capVoltageById.TryGetValue(cap.componentId, out var v0))
            {
                var field = cap.GetType().GetField("initialVoltage");
                if (field != null && field.FieldType == typeof(double))
                    field.SetValue(cap, v0);
            }
        }

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
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> spiceComponents)
    {
        Debug.Log($"[CircuitManager] Running TRANSIENT for group of {group.Count} components...");

        var tran = new Transient("tran", timeStep, simDuration);

        IEnumerator enumerator;
        try
        {
            enumerator = tran.Run(ckt).GetEnumerator();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SPICE TRANSIENT ERROR] {ex.Message}");

            // Try to print inner exception details too (SpiceSharp often nests the useful info)
            if (ex.InnerException != null)
                Debug.LogError($"[SPICE TRANSIENT ERROR - INNER] {ex.InnerException.Message}");

           yield break;

        }

        // Quick lookup so LED updates use the *same nodes used in netlist*
        var nodesByComp = spiceComponents
            .Where(e => e.comp != null && group.Contains(e.comp))
            .ToDictionary(e => e.comp, e => (e.nodeA, e.nodeB));

        double highestVSeen = 0.0;

        while (true)
        {
            bool movedNext;
            try
            {
                movedNext = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SPICE TRANSIENT ERROR] {ex.Message}");
                break;
            }

            if (!movedNext) break;

            // Update LEDs
            foreach (var led in group.OfType<LED_Component>())
            {
                if (led == null) continue;
                if (!nodesByComp.TryGetValue(led, out var nn)) continue;

                double vA, vB;
                try { vA = tran.GetVoltage(nn.nodeA); }
                catch { continue; }
                try { vB = tran.GetVoltage(nn.nodeB); }
                catch { continue; }

                float drop = Mathf.Abs((float)(vA - vB));
                led.UpdateLEDState(drop);

                highestVSeen = Math.Max(highestVSeen, Math.Abs(vA - vB));
            }

            SaveCapStates(tran, group, nodesByComp);
            DebugCapacitorsStep(tran, group, nodesByComp);

            yield return null;
        }

        lastVoltage = (float)highestVSeen;
        Debug.Log($"[CircuitManager] Transient done — lastVoltage={lastVoltage:F3}V");
    }

    private void SaveCapStates(
        Transient tran,
        List<CircuitComponentBase> group,
        Dictionary<CircuitComponentBase, (string nodeA, string nodeB)> nodesByComp)
    {
        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            if (!nodesByComp.TryGetValue(cap, out var nn)) continue;

            try
            {
                double vA = tran.GetVoltage(nn.nodeA);
                double vB = tran.GetVoltage(nn.nodeB);
                _capVoltageById[cap.componentId] = vA - vB;
            }
            catch { }
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
        var f =
            t.GetField("capacitance") ??
            t.GetField("capacitanceFarads") ??
            t.GetField("C") ??
            t.GetField("c");

        if (f != null && f.FieldType == typeof(double))
            return (double)f.GetValue(cap);

        if (f != null && f.FieldType == typeof(float))
            return (float)f.GetValue(cap);

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
        Dictionary<CircuitComponentBase, (string nodeA, string nodeB)> nodesByComp)
    {
        if (!debugCapacitor) return;

        if (tran.Time < _nextCapPrintTime) return;
        _nextCapPrintTime = tran.Time + capDebugPrintEverySeconds;

        foreach (var cap in group.Where(x => x != null && x.GetType().Name == "CapacitorComponent"))
        {
            if (!nodesByComp.TryGetValue(cap, out var nn)) continue;

            double vA, vB;
            try
            {
                vA = tran.GetVoltage(nn.nodeA);
                vB = tran.GetVoltage(nn.nodeB);
            }
            catch { continue; }

            double vcap = vA - vB;
            double cF = GetCapacitanceFarads(cap);
            double qC = cF * vcap;
            double eJ = 0.5 * cF * vcap * vcap;

            Debug.Log($"[CAP] t={tran.Time:F3}s  {cap.componentId}  Vcap={vcap:F4}V  C={cF:E3}F  Q={qC:E3}C  E={eJ:E3}J  ({nn.nodeA}-{nn.nodeB})");
        }
    }
}

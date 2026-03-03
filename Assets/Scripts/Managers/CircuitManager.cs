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
    public float maxSimTime = 1000f;

    private Coroutine _transientRoutine;
    private Transient _activeTran;
    private Action _pendingSpiceMutation;
    private Dictionary<CircuitComponentBase, (string nodeA, string nodeB)> _nodesByComponent =
        new Dictionary<CircuitComponentBase, (string nodeA, string nodeB)>();

    private List<CircuitComponentBase> _activeGroup;
    
    private bool _isRebuilding = false;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        RebuildAndSimulate();
    }

    // =========================================================
    // PUBLIC: Call this ONLY when topology changes
    // =========================================================
    public void NotifyConnectionChanged()
    {
        NotifyTopologyChanged();
    }

    public void NotifyTopologyChanged()
    {
        if (_isRebuilding) return;

        Debug.Log("[CircuitManager] Topology changed → rebuilding.");

        RebuildAndSimulate();
    }

    // =========================================================

    private bool _pendingParameterChange = false;

    public void QueueSpiceMutation(Action mutation)
    {
        _pendingSpiceMutation = mutation;
    }

    // =========================================================
    private void RebuildAndSimulate()
    {
        _isRebuilding = true;

        try
        {
            // Stop old transient safely
            if (_transientRoutine != null)
            {
                StopCoroutine(_transientRoutine);
                _transientRoutine = null;
            }

            _activeTran = null;
            _nodesByComponent.Clear();
            _activeGroup = null;

            var allComponents = FindObjectsByType<CircuitComponentBase>(FindObjectsSortMode.None);
            var wires = FindObjectsByType<Wire>(FindObjectsSortMode.None);

            var nodeMap = BuildNodeMap(allComponents, wires);
            var spiceEntries = BuildSpiceEntries(allComponents, nodeMap);



            var group = ChooseBestGroup(spiceEntries);
            if (group == null)
            {
                Debug.Log("[CircuitManager] No valid group.");
                TurnAllLedsOff();
                return;
            }

            var ckt = BuildSpiceCircuit(group, spiceEntries);

            // Start persistent transient
            _transientRoutine = StartCoroutine(
                RunContinuousTransient(ckt, group, spiceEntries)
            );
        }
        finally
        {
            _isRebuilding = false;
        }
    }

    // =========================================================
    private Dictionary<PortSocketBinder, string> BuildNodeMap(
        CircuitComponentBase[] components,
        Wire[] wires)
    {
        var clusters = new List<PortCluster>();
        var sockets = new List<PortSocketBinder>();

        foreach (var comp in components)
        {
            foreach (var t in comp.GetTerminals())
            {
                clusters.Add(t);
                sockets.AddRange(t.GetSockets());
            }
        }

        var ds = new DisjointSet<PortSocketBinder>();

        foreach (var s in sockets)
            ds.Find(s);

        foreach (var cluster in clusters)
        {
            var sockList = cluster.GetSockets().ToList();
            for (int i = 1; i < sockList.Count; i++)
                ds.Union(sockList[0], sockList[i]);
        }

        foreach (var wire in wires)
        {
            if (!wire.IsComplete) continue;
            ds.Union(wire.portA, wire.portB);
        }

        var map = new Dictionary<PortSocketBinder, string>();
        int nextNode = 1;

        foreach (var group in ds.GroupsSnapshot())
        {
            bool isGround = group.Any(s => s.GetComponentInParent<GroundNode>() != null);
            string name = isGround ? "0" : $"N{nextNode++}";

            foreach (var s in group)
                map[s] = name;
        }

        return map;
    }

    // =========================================================
    private List<(CircuitComponentBase comp, string nodeA, string nodeB)> BuildSpiceEntries(
        CircuitComponentBase[] components,
        Dictionary<PortSocketBinder, string> nodeMap)
    {
        var list = new List<(CircuitComponentBase, string, string)>();

        foreach (var comp in components)
        {
            var terms = comp.GetTerminals().ToArray();
            if (terms.Length < 2) continue;

            var aSock = terms[0].AnySocket();
            var bSock = terms[1].AnySocket();
            if (aSock == null || bSock == null) continue;

            if (!nodeMap.TryGetValue(aSock, out var nodeA)) continue;
            if (!nodeMap.TryGetValue(bSock, out var nodeB)) continue;

            list.Add((comp, nodeA, nodeB));
        }

        return list;
    }

    // =========================================================
    private List<CircuitComponentBase> ChooseBestGroup(
    List<(CircuitComponentBase comp, string nodeA, string nodeB)> entries)
    {
        var graph = new Dictionary<CircuitComponentBase, List<CircuitComponentBase>>();

        foreach (var e in entries)
        {
            if (!graph.ContainsKey(e.comp))
                graph[e.comp] = new List<CircuitComponentBase>();

            foreach (var other in entries)
            {
                if (other.comp == e.comp) continue;

                bool share =
                    e.nodeA == other.nodeA || e.nodeA == other.nodeB ||
                    e.nodeB == other.nodeA || e.nodeB == other.nodeB;

                if (share)
                    graph[e.comp].Add(other.comp);
            }
        }

        var visited = new HashSet<CircuitComponentBase>();
        var groups = new List<List<CircuitComponentBase>>();

        foreach (var start in graph.Keys)
        {
            if (!visited.Add(start)) continue;

            var group = new List<CircuitComponentBase>();
            var q = new Queue<CircuitComponentBase>();
            q.Enqueue(start);

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                group.Add(cur);

                foreach (var n in graph[cur])
                    if (visited.Add(n))
                        q.Enqueue(n);
            }

            groups.Add(group);
        }

        // Choose group that contains a DC source
        return groups
            .Where(g => g.Any(c => c is DCSource))
            .OrderByDescending(g => g.Count)
            .FirstOrDefault();
    }

    // =========================================================
    private Circuit BuildSpiceCircuit(
        List<CircuitComponentBase> group,
        List<(CircuitComponentBase comp, string nodeA, string nodeB)> entries)
    {
        var ckt = new Circuit();

        foreach (var e in entries)
        {
            if (!group.Contains(e.comp)) continue;
            if (e.comp is GroundNode) continue;

            e.comp.AddToSpice(ckt, e.nodeA, e.nodeB);
        }
        foreach (var e in entries)
        {
            Debug.Log($"{e.comp.componentId} => {e.nodeA} <-> {e.nodeB}");
        }
        return ckt;
    }

    // =========================================================

    private IEnumerator RunContinuousTransient(
     Circuit ckt,
     List<CircuitComponentBase> group,
     List<(CircuitComponentBase comp, string nodeA, string nodeB)> entries)
    {
        Debug.Log("[CircuitManager] Starting persistent transient...");

        var tran = new Transient("tran", timeStep, maxSimTime);
        _activeTran = tran;
        _activeGroup = group;

        _nodesByComponent = entries
            .Where(e => group.Contains(e.comp))
            .ToDictionary(e => e.comp, e => (e.nodeA, e.nodeB));

        var enumerator = tran.Run(ckt).GetEnumerator();

        while (true)
        {
            if (_activeTran != tran)
                yield break;

            bool ok;

            try
            {
                ok = enumerator.MoveNext();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[CircuitManager] Transient crashed: {ex}");
                yield break;
            }

            if (!ok)
            {
                Debug.LogError("[CircuitManager] Transient ended unexpectedly.");
                yield break;
            }

            //  SAFE POINT: timestep fully solved
            if (_pendingSpiceMutation != null)
            {
                _pendingSpiceMutation.Invoke();
                _pendingSpiceMutation = null;
            }

            // Read capacitor voltages
            foreach (var cap in group.OfType<CapacitorComponent>())
            {
                if (_nodesByComponent.TryGetValue(cap, out var nn))
                {
                    double vA = nn.nodeA == "0" ? 0.0 : tran.GetVoltage(nn.nodeA);
                    double vB = nn.nodeB == "0" ? 0.0 : tran.GetVoltage(nn.nodeB);

                    Debug.Log($"[CAP LIVE] {cap.componentId} = {(vA - vB):F4} V");
                }
            }

            UpdateLeds(tran);

            yield return null;
        }
    }
    // =========================================================
    private void UpdateLeds(Transient tran)
    {
        foreach (var led in _activeGroup.OfType<LED_Component>())
        {
            if (!_nodesByComponent.TryGetValue(led, out var nn))
                continue;

            double vA = nn.nodeA == "0" ? 0.0 : tran.GetVoltage(nn.nodeA);
            double vB = nn.nodeB == "0" ? 0.0 : tran.GetVoltage(nn.nodeB);

            float drop = Mathf.Abs((float)(vA - vB));
            led.UpdateLEDState(drop);
        }
    }

    // =========================================================
    private void TurnAllLedsOff()
    {
        foreach (var l in FindObjectsByType<LED_Component>(FindObjectsSortMode.None))
            l.UpdateLEDState(0f);
    }
}
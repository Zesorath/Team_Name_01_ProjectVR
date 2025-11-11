using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;   // optional with the fully-qualified type, but good to keep
using SpiceSharp.Simulations;

// Optional ground tag: any node connected to this port becomes Spice node "0"
public class GroundDevice : MonoBehaviour
{
    public PortAnchor port;
}

public class CircuitManager : MonoBehaviour
{
    private bool TryNode(PortAnchor p, out string node)
    {
        node = null;
        if (p == null) return false;
        return lastNodeOfPort != null && lastNodeOfPort.TryGetValue(p, out node) && !string.IsNullOrEmpty(node);
    }
    public static CircuitManager Instance { get; private set; }
    readonly HashSet<(PortAnchor, PortAnchor)> _junctions = new();

    void Awake()
    {
        Instance = this;
    }

    public void RegisterJunction(PortAnchor a, PortAnchor b)
    {
        if (a && b)
        {
            _junctions.Add((a, b));
            RebuildAndSolve();
        }
    }

    public void UnregisterJunction(PortAnchor a, PortAnchor b)
    {
        if (a && b && _junctions.Contains((a, b)))
        {
            _junctions.Remove((a, b));
            RebuildAndSolve();
        }
    }
    [Header("Rebuild triggers")]
    public bool autoRebuildOnStart = true;

    [Header("Results")]
    // Node voltages by node name: "N1","N2","0",...
    public Dictionary<string, double> nodeVoltages = new Dictionary<string, double>();
    // Device currents by device id (e.g., "R1","R2"). Computed for resistors as (Va - Vb)/R.
    public Dictionary<string, double> deviceCurrents = new Dictionary<string, double>();

    [Header("Events / UI")]
    // Fired after every successful solve so HUDs can refresh.
    public Action OnSolved;
    // Port-to-node map from the last solve (for per-port probe labels).
    public Dictionary<PortAnchor, string> lastNodeOfPort = new Dictionary<PortAnchor, string>();

    [Header("Debug")]
    public bool debugVerbose = true;   // toggle detailed logs
    public int debugRebuildCount = 0;  // counts rebuilds

    private readonly StringBuilder _sb = new StringBuilder();

    private void D(string msg)
    {
        if (!debugVerbose) return;
        Debug.Log("[Circuit DBG] " + msg);
    }

    void Start()
    {
        if (autoRebuildOnStart) RebuildAndSolve();
    }

    // Call this after any connect/disconnect or parameter change
    public void RebuildAndSolve()
    {
        // ---- 1) Gather ports & wires ----
        var ports = UnityEngine.Object.FindObjectsByType<PortAnchor>(FindObjectsSortMode.None);
        var wires = UnityEngine.Object.FindObjectsByType<WireLink>(FindObjectsSortMode.None);

        if (wires == null || wires.Length == 0)
            D("No wires found. Nothing to solve.");
        if (ports == null || ports.Length == 0)
            D("No ports found. Nothing to solve.");

        // ---- 2) Union-Find: group electrically connected ports into nodes ----
        var uf = new UnionFind<PortAnchor>();
        if (ports != null) foreach (var p in ports) uf.Add(p);
        if (wires != null) foreach (var w in wires) if (w != null && w.IsComplete) uf.Union(w.endA, w.endB);
        if (_junctions.Count > 0)
        {
            foreach (var j in _junctions)
            {
                var (a, b) = j;
                if (a && b) uf.Union(a, b);
            }
        }
        // ---- 3) Assign node names (N1, N2, ...) ----
        var nodeName = new Dictionary<PortAnchor, string>();
        int idx = 1;
        foreach (var group in uf.Groups())
        {
            string name = "N" + idx++;
            foreach (var p in group) nodeName[p] = name;
        }

        // Optional: remap one connected group to ground "0" if a GroundDevice exists
        var ground = UnityEngine.Object.FindFirstObjectByType<GroundDevice>();
        if (ground != null && ground.port != null && nodeName.TryGetValue(ground.port, out var gname))
        {
            var keys = new List<PortAnchor>(nodeName.Keys);
            foreach (var k in keys)
                if (nodeName[k] == gname) nodeName[k] = "0";
        }

        // Store mapping for HUDs
        lastNodeOfPort = nodeName;
        // --- TEMP REFERENCE (no GroundDevice): choose a node to be the reference ---
        // We'll prefer the DC negative node if present; otherwise the first node we find.
        string tempRefNode = null;
        {
            foreach (var kv in nodeName) { tempRefNode = kv.Value; break; } // first node (if any)
                                                                            // We'll try to improve this later once we know which node is DC negative.
        }

        // Debug: node map
        debugRebuildCount++;
        D("----- Rebuild #" + debugRebuildCount + " -----");
        _sb.Length = 0;
        _sb.AppendLine("Node map (PortAnchor -> Node):");
        foreach (var kv in nodeName)
        {
            var port = kv.Key;
            var node = kv.Value;
            var owner = port ? port.gameObject.name : "(null)";
            _sb.Append("  ").Append(owner);
            if (port && !string.IsNullOrEmpty(port.pinName))
                _sb.Append(".").Append(port.pinName);
            _sb.Append(" => ").Append(node).AppendLine();
        }
        D(_sb.ToString());

        // Helper: get a node name from a port (defaults to ground if missing)
        string NodeByPort(PortAnchor p) => (p != null && nodeName.TryGetValue(p, out var n)) ? n : "0";

        // ---- 4) Build the Spice circuit from devices ----
        var ckt = new Circuit();
        int counterR = 1, counterV = 1, counterX = 1;

        var resistorList = new List<ResistorComponet>();
        var sourceList   = new List<DC>();

        var behaviours = UnityEngine.Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in behaviours)
        {
            if (mb is ISpiceDevice dev)
            {
                // Prefix by declared SpiceName, default to X
                string prefix = dev.SpiceName ?? "X";
                string unique;
                if (prefix.StartsWith("R")) unique = "R" + (counterR++);
                else if (prefix.StartsWith("V")) unique = "V" + (counterV++);
                else unique = "X" + (counterX++);

                // If the device exposes a public field 'spiceId', push the unique id into it
                var t = dev.GetType();
                var f = t.GetField("spiceId");
                if (f != null) f.SetValue(dev, unique);

                // Capture param echoes for debug
                if (mb is ResistorComponet rDev)
                {
                    resistorList.Add(rDev);
                    D("Resistor found: id=" + (unique) +
                      " R=" + rDev.resistance +
                      " a=" + (rDev.a ? rDev.a.pinName : "(null)") +
                      " b=" + (rDev.b ? rDev.b.pinName : "(null)"));
                }
                else if (mb is DC vDev)
                {
                    sourceList.Add(vDev);
                    D("DC source found: id=" + (unique) +
                      " V=" + vDev.voltage +
                      " +=" + (vDev.positive ? vDev.positive.pinName : "(null)") +
                      " -=" + (vDev.negative ? vDev.negative.pinName : "(null)"));
                }

                // Contribute to the Circuit
                dev.Contribute(ckt, NodeByPort);
            }
        }
       // ---TEMP REFERENCE(no GroundDevice): anchor one node to "0" with a 0V source ---
try
        {
            // Pick a reference candidate (prefer DC negative if present)
            string refNodeCandidate = null;

            // 1) Try DC negative
            foreach (var s in sourceList)
            {
                if (s != null && s.negative != null && lastNodeOfPort != null)
                {
                    string n;
                    if (lastNodeOfPort.TryGetValue(s.negative, out n) && !string.IsNullOrEmpty(n))
                    {
                        refNodeCandidate = n;
                        break;
                    }
                }
            }

            // 2) Otherwise fall back to the first node name we discovered
            if (string.IsNullOrEmpty(refNodeCandidate) && lastNodeOfPort != null && lastNodeOfPort.Count > 0)
            {
                foreach (var kv in lastNodeOfPort) { refNodeCandidate = kv.Value; break; }
            }

            // 3) Only add VREF if we have at least one node and there's no explicit ground "0"
            bool hasExplicitGround = false;
            if (lastNodeOfPort != null)
            {
                foreach (var n in lastNodeOfPort.Values)
                {
                    if (n == "0") { hasExplicitGround = true; break; }
                }
            }

            if (!hasExplicitGround && !string.IsNullOrEmpty(refNodeCandidate))
            {
                // Add a 0 V source between the chosen node and "0" to provide a reference
                ckt.Add(new SpiceSharp.Components.VoltageSource("VREF", refNodeCandidate, "0", 0.0));
                D("[Circuit DBG] TEMP reference added: VREF between " + refNodeCandidate + " and 0 (0V).");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Circuit DBG] Could not add temp reference: " + ex.Message);
        }


        nodeVoltages.Clear();
        deviceCurrents.Clear();

        var op = new OP("op");
        try
        {
            op.Run(ckt);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[Circuit] OP solve failed: " + ex.Message + "\n" + ex.StackTrace);
            OnSolved?.Invoke();
            return;
        }
        nodeVoltages.Clear();

        // Collect all node names we know about
        var allNodes = new HashSet<string>(lastNodeOfPort.Values);

        // Read node voltages using the helper (works across SpiceSharp versions)
        foreach (var n in allNodes)
        {
            try
            {
                // Ground is always 0
                if (n == "0") { nodeVoltages[n] = 0.0; continue; }

                // This helper existed in your earlier code and compiled
                double v = SimulationHelper.GetVoltage(op, n);
                nodeVoltages[n] = v;
            }
            catch
            {
                nodeVoltages[n] = double.NaN;
            }
        }

        // Debug: inputs (sources and resistors)
        _sb.Length = 0;
        _sb.AppendLine("Inputs:");
        foreach (var s in sourceList)
            _sb.Append("  ").Append(s.spiceId).Append(" = ")
               .Append(s.voltage.ToString("F6")).Append(" V").AppendLine();
        foreach (var r in resistorList)
            _sb.Append("  ").Append(r.spiceId).Append(" = ")
               .Append(r.resistance.ToString("F6")).Append(" Ohms").AppendLine();
        D(_sb.ToString());

        // Debug: node voltages
        _sb.Length = 0;
        _sb.AppendLine("Node voltages:");
        foreach (var kv in nodeVoltages)
            _sb.Append("  ").Append(kv.Key).Append(" = ")
               .Append(kv.Value.ToString("F6")).Append(" V").AppendLine();
        D(_sb.ToString());

        // ---- 6) Per-resistor currents: compute from node voltages (I = (Va - Vb)/R) ----
        _sb.Length = 0;
        _sb.AppendLine("Resistor currents (I = (Va - Vb)/R):");

        foreach (var r in resistorList)
        {
            string na = (r.a && nodeName.ContainsKey(r.a)) ? nodeName[r.a] : "0";
            string nb = (r.b && nodeName.ContainsKey(r.b)) ? nodeName[r.b] : "0";

            double va = nodeVoltages.ContainsKey(na) ? nodeVoltages[na] : 0.0;
            double vb = nodeVoltages.ContainsKey(nb) ? nodeVoltages[nb] : 0.0;

            double ohms = Mathf.Max(1e-6f, r.resistance);
            double i = (va - vb) / ohms;

            string id = string.IsNullOrEmpty(r.spiceId) ? "R?" : r.spiceId;
            deviceCurrents[id] = i;

            _sb.Append("  ").Append(id)
               .Append(" : Va(").Append(na).Append(")=").Append(va.ToString("F6"))
               .Append(" V, Vb(").Append(nb).Append(")=").Append(vb.ToString("F6"))
               .Append(" V, R=").Append(ohms.ToString("F6"))
               .Append(" Ohms, I=").Append(i.ToString("F9")).Append(" A")
               .AppendLine();
        }
        D(_sb.ToString());

        Debug.Log("[Circuit] Rebuilt " + allNodes.Count + " nodes; OP solved.");

        OnSolved?.Invoke();
    }

#if UNITY_EDITOR
    [ContextMenu("Debug: Rebuild And Solve")]
    private void ContextRebuild()
    {
        RebuildAndSolve();
    }
#endif

    // ---------- Simple Union-Find ----------
    class UnionFind<T>
    {
        private readonly Dictionary<T, T> parent = new();
        private readonly Dictionary<T, int> rank = new();

        public void Add(T x)
        {
            if (!parent.ContainsKey(x))
            {
                parent[x] = x;
                rank[x] = 0;
            }
        }

        public T Find(T x)
        {
            // Ensure existence (useful if caller unions unseen items)
            if (!parent.ContainsKey(x))
                Add(x);

            var p = parent[x];
            if (!EqualityComparer<T>.Default.Equals(p, x))
                parent[x] = Find(p); // path compression
            return parent[x];
        }

        public void Union(T a, T b)
        {
            Add(a);
            Add(b);
            var ra = Find(a);
            var rb = Find(b);
            if (EqualityComparer<T>.Default.Equals(ra, rb)) return;

            // union by rank
            var raRank = rank[ra];
            var rbRank = rank[rb];
            if (raRank < rbRank)
                parent[ra] = rb;
            else if (raRank > rbRank)
                parent[rb] = ra;
            else
            {
                parent[rb] = ra;
                rank[ra] = raRank + 1;
            }
        }

        public IEnumerable<List<T>> Groups()
        {
            // SNAPSHOT keys so path compression won't mutate the enumerated collection.
            var keys = new List<T>(parent.Keys);

            // Optional: compress all roots first (on the snapshot)
            foreach (var k in keys)
                Find(k);

            // Build buckets off the (possibly-compressed) parents — still safe because we only iterate 'keys'
            var buckets = new Dictionary<T, List<T>>();
            foreach (var k in keys)
            {
                var r = parent[k]; // already compressed
                if (!buckets.TryGetValue(r, out var list))
                {
                    list = new List<T>();
                    buckets[r] = list;
                }
                list.Add(k);
            }
            return buckets.Values;
        }
    }
}
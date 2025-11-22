using UnityEngine;
using System.Collections.Generic;
using SpiceSharp;
using SpiceSharp.Simulations;
using UnityEngine.SceneManagement;

public class CircuitManager : MonoBehaviour
{
    public static CircuitManager Instance;
    public float lastVoltage = 0f;

    private void Awake() => Instance = this;

    public void NotifyConnectionChanged()
    {
        Debug.Log("[CircuitManager] Connection changed — rebuilding...");
        RebuildAndSimulate();
    }

    void RebuildAndSimulate()
    {
        var wires = Object.FindObjectsByType<Wire>(FindObjectsSortMode.None);
        var nodes = new Dictionary<CircuitComponentBase, List<CircuitComponentBase>>();

        foreach (var w in wires)
        {
            if (!w.IsComplete) continue;
            var (a, b) = w.GetConnectionPair();
            if (a == null || b == null) continue;

            if (!nodes.ContainsKey(a)) nodes[a] = new();
            if (!nodes.ContainsKey(b)) nodes[b] = new();

            nodes[a].Add(b);
            nodes[b].Add(a);
        }

        if (nodes.Count < 2)
        {
            Debug.LogWarning("[CircuitManager] Need at least 2 connected components.");
            lastVoltage = 0;
            return;
        }

        // Build Spice circuit
        Circuit ckt = new Circuit();
        var componentList = new List<CircuitComponentBase>(nodes.Keys);

        string prevNode = "0"; // start with ground
        string nextNode = "N1";

        foreach (var comp in componentList)
        {
            comp.AddToSpice(ckt, prevNode, nextNode);
            prevNode = nextNode;
            nextNode = "N" + (Random.Range(2, 99));
        }

        RunSpice(ckt);
    }

    void RunSpice(Circuit ckt)
    {
        try
        {
            var op = new OP("DC");
            var export = new RealVoltageExport(op, "N1", "0");
            op.Run(ckt);

            lastVoltage = (float)export.Value;
            Debug.Log($"[Circuit] Result voltage: {lastVoltage}V");

            // ---- Update all LEDs in the scene ----
            var leds = Object.FindObjectsByType<LED_Component>(FindObjectsSortMode.None);
            foreach (var led in leds)
            {
                float appliedV = lastVoltage;   // simple: use main node voltage
                led.UpdateLEDState(appliedV);
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[Circuit] SPICE failed: {ex}");
        }
    }
}

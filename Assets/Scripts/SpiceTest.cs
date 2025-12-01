using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;

public class SpiceTest : MonoBehaviour
{
    void Start()
    {
        RunDCSweep();
    }

    void RunDCSweep()
    {
        Debug.Log("[SPICE] Starting DC Sweep Test...");

        // Circuit: V1 -> R1 -> R2 -> ground
        var ckt = new Circuit(
            new VoltageSource("V1", "in", "0", 0.0),
            new Resistor("R1", "in", "out", 1000.0),
            new Resistor("R2", "out", "0", 2000.0)
        );

        // Sweep V1 from 0 to 5 volts in 0.1 increments
        double start = 0.0;
        double stop = 5.0;
        double step = 0.1;

        var dc = new DC("dc", "V1", start, stop, step);

        double currentSweepValue = start;

        try
        {
            foreach (var _ in dc.Run(ckt))
            {
                double vOut = dc.GetVoltage("out");

                Debug.Log($"[SPICE DC] V1={currentSweepValue:F3}V  V(out)={vOut:F4}V");

                currentSweepValue += step;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SPICE ERROR] {ex.Message}");
        }

        Debug.Log("[SPICE] DC Sweep Complete.");
    }
}

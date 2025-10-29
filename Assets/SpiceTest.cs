using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class SpiceTest : MonoBehaviour
{
    void Start()
    {
        try
        {
            var ckt = new Circuit(
                new Resistor("R1", "in", "out", 1000)
            );

            // Count entities via enumeration to avoid API differences
            int count = 0;
            foreach (var _ in ckt) count++;

            Debug.Log($"✅ SpiceSharp is working! Circuit has {count} item(s).");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("❌ SpiceSharp test failed: " + ex.Message);
        }
    }
}

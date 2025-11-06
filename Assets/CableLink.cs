// CableLink.cs
using UnityEngine;
using System;

public class CableLink : MonoBehaviour
{
    [Header("Optional visual wire")]
    public KinematicCableTube tubeWire;
    public Transform endA;   // DC tip
    public Transform endB;   // Resistor tip

    [Header("Spice")]
    public Spicerunner spiceRunner;   // assign in Inspector
    public bool useSpice = true;

    // Live endpoints
    private Direct_Current dc;
    private Ohms resistor;

    // NEW: telemetry event
    // Args: voltage (V), current (A), resistance (Ohms)
    public event Action<float, float, float> OnTelemetry;

    public void SetDCEnd(Direct_Current dcSource, Transform dcTip)
    {
        dc = dcSource;
        endA = dcTip;
        UpdateWire();
        TryTransmit();
    }

    public void SetResistorEnd(Ohms r, Transform rTip)
    {
        resistor = r;
        endB = rTip;
        UpdateWire();
        TryTransmit();
    }

    public void ClearDCEnd() { dc = null; endA = null; UpdateWire(); Emit(0f, 0f, 0f); }
    public void ClearResistorEnd() { resistor = null; endB = null; UpdateWire(); Emit(0f, 0f, 0f); }

    private void UpdateWire()
    {
        if (tubeWire == null) return;
        if (endA != null && endB != null)
        {
            tubeWire.endA = endA;
            tubeWire.endB = endB;
            if (!tubeWire.gameObject.activeSelf) tubeWire.gameObject.SetActive(true);
        }
        else
        {
            if (tubeWire.gameObject.activeSelf) tubeWire.gameObject.SetActive(false);
        }
    }

    private void Emit(float v, float i, float r)
    {
        if (OnTelemetry != null) OnTelemetry.Invoke(v, i, r);
    }

    // Call when voltage or resistance changes, or both ends connect
    private void TryTransmit()
    {
        if (dc == null || resistor == null) { Emit(0f, 0f, 0f); return; }

        float v = dc.voltage;
        float r = resistor.resistance;

        if (r <= 0f)
        {
            Debug.LogWarning("[Cable] Invalid resistance (<=0). Skipping Spice call.");
            Emit(v, 0f, r);
            return;
        }

        // Show immediate UI feedback (analytic), then optionally call Spice for logs
        float i = v / r;
        Emit(v, i, r);

        if (useSpice && spiceRunner != null)
        {
            // Keep your existing Spicerunner logging
            spiceRunner.RunSpiceOp(v, r);
        }
    }
}

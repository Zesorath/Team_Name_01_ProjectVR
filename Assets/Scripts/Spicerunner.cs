using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

using SpiceSharp;
using SpiceSharp.Components;
using SpiceSharp.Simulations;

public class Spicerunner : MonoBehaviour
{
    public XRSocketInteractor socket;
    public Direct_Current source;  // your MonoBehaviour with "public float voltage;"
    public bool runTestOnStart = false;

    void Awake()
    {
        if (socket == null) socket = GetComponent<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
            Debug.Log("[Spice] Spicerunner: subscribed to socket events.");
        }
        if (source == null) source = GetComponentInParent<Direct_Current>();
    }

    void Start()
    {
        if (runTestOnStart) RunSpiceOp(5f, 1000f);
    }

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (source == null) { Debug.LogWarning("[Spice] No DC component found."); return; }

        Ohms resistor = null; // or Resitor if that's your script name
        if (args.interactableObject is UnityEngine.Component comp)
            resistor = comp.GetComponentInParent<Ohms>();

        if (resistor == null)
        {
            Debug.Log("[Spice] Connected object is not a resistor component.");
            return;
        }

        Debug.Log("[Spice] Running DC with Vdc=" + source.voltage + " V, R=" + resistor.resistance + " Ohms.");
        RunSpiceOp(source.voltage, resistor.resistance);
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        Debug.Log("[Spice] Disconnected.");
    }

    public void RunSpiceOp(float vdc, float r)
    {
        try
        {
            // Build the same circuit (kept for future SpiceSharp use or if exports work on your build)
            var ckt = new SpiceSharp.Circuit(
                new SpiceSharp.Components.VoltageSource("V1", "IN", "0", vdc),
                new SpiceSharp.Components.Resistor("RLOAD", "IN", "0", r)
            );

            // Try an operating point
            var op = new SpiceSharp.Simulations.OP("op");

            // Set up exports BEFORE running (works on many builds)
            var vInExport = new SpiceSharp.Simulations.RealVoltageExport(op, "IN");
            var iV1Export = new SpiceSharp.Simulations.RealPropertyExport(op, "V1", "i");
            var iRExport = new SpiceSharp.Simulations.RealPropertyExport(op, "RLOAD", "i");

            // Run once
            op.Run(ckt);

            double vin = vInExport.Value;
            double iV1 = iV1Export.Value;
            double iR = iRExport.Value;

            // If your SpiceSharp build doesn't populate exports here, fall back to analytic values
            bool looksZero = (System.Math.Abs(vin) < 1e-9) && (System.Math.Abs(iR) < 1e-12) && (System.Math.Abs(iV1) < 1e-12);
            if (looksZero)
            {
                double i = (r > 0.0) ? (vdc / r) : 0.0;
                vin = vdc;
                iR = i;
                iV1 = -i;
                Debug.Log("[Spice] OP exports not available on this build; using analytic DC solution.");
            }

            Debug.Log(
                "[Spice] Result - " +
                "V(IN)=" + vin.ToString("F4") + " V, " +
                "I(RLOAD)=" + (iR * 1000.0).ToString("F4") + " mA, " +
                "I(V1)=" + (iV1 * 1000.0).ToString("F4") + " mA, " +
                "R=" + r + " Ohms, " +
                "Vdc=" + vdc + " V"
            );
        }
        catch (System.Exception ex)
        {
            // As a last resort, compute analytically if SpiceSharp throws
            double i = (r > 0.0) ? (vdc / r) : 0.0;
            Debug.LogError("[Spice] Simulation error: " + ex.Message + "\n" + ex.StackTrace);
            Debug.Log(
                "[Spice] Fallback - " +
                "V(IN)=" + vdc.ToString("F4") + " V, " +
                "I(RLOAD)=" + (i * 1000.0).ToString("F4") + " mA, " +
                "I(V1)=" + (-i * 1000.0).ToString("F4") + " mA, " +
                "R=" + r + " Ohms, " +
                "Vdc=" + vdc + " V"
            );
        }
    }
}

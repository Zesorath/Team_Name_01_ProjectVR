using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;

public class DCSource : CircuitComponentBase
{
    [Header("DC Source")]
    public float voltage = 5f;

    [Header("UI Display")]
    public TMPro.TextMeshProUGUI voltageLabel;

    [Header("Runtime Adjustment")]
    public float voltageStep = 0.5f;
    public float minVoltage = 0f;
    public float maxVoltage = 30f;
    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        string vName = $"V_{componentId}";
        ckt.Add(new SpiceSharp.Components.VoltageSource(vName, nodeA, nodeB, voltage));
        Debug.Log($"[DCSource] Adding Vsource: {vName} => {nodeA} to {nodeB}, {voltage}V");
    }
    public void IncrementVoltage()
    {
        voltage = Mathf.Clamp(voltage + voltageStep, minVoltage, maxVoltage);
        if (voltageLabel != null)
            voltageLabel.text = $"{voltage:F1}V";
        CircuitManager.Instance.NotifyConnectionChanged();
    }

    public void DecrementVoltage()
    {
        voltage = Mathf.Clamp(voltage - voltageStep, minVoltage, maxVoltage);
        if (voltageLabel != null)
            voltageLabel.text = $"{voltage:F1}V";
        CircuitManager.Instance.NotifyConnectionChanged();
    }
}

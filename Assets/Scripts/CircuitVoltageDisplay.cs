using UnityEngine;
using TMPro;

public class CircuitVoltageDisplay : MonoBehaviour
{
    public CircuitManager circuitManager;
    public TMP_Text voltageText;
    public string format = "Overall Voltage: {0:0.000} V";

    void Start()
    {
        if (circuitManager == null)
            circuitManager = CircuitManager.Instance;

        if (voltageText == null)
            voltageText = GetComponent<TMP_Text>();
    }

    void Update()
    {
        if (!isActiveAndEnabled)
            return;

        if (voltageText == null || circuitManager == null)
            return;

        voltageText.text = string.Format(format, circuitManager.overallVoltage);
    }
}

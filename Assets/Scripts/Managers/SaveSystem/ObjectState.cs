using System;
using UnityEngine;

[Serializable]
public class ObjectState
{
    public ComponentTypes.Types type;
    public int index;
    public string label;
    public Vector3 position = Vector3.zero;
    public Quaternion rotation = Quaternion.identity;
    public float voltage;
    public float resistance;
    public float ledVoltage;

    public ObjectState(ComponentID cID)
    {
        type = cID.type;
        index = cID.index;
        label = cID.label;
    }

    // For serializing
    public void Capture_ObjectState(ComponentID cID)
    {
        Log($"CAPTURING component {cID.id} state");
        if (cID == null) 
        {
            Error($"CAPTURE STATE FAILED--no ComponentID");
            return;
        }
        
        GameObject go = cID.gameObject;

        position = go.transform.position;
        rotation = go.transform.rotation;

        DCSource dc = go.GetComponent<DCSource>();
        if (dc != null) { voltage = dc.voltage; }

        Ohms res = go.GetComponent<Ohms>();
        if (res != null) { resistance = res.resistance; }

        LED_Component led = go.GetComponent<LED_Component>();
        if (led != null) { ledVoltage = led.CurrentVoltage; }

        Success($"{cID.id} state CAPTURED");
    }

    // For deserializing
    public void Apply_ObjectState(ComponentID cID)
    {
        Log($"APPLYING saved state to component {cID.id}");
        if (cID == null) 
        {
            Error($"APPLY STATE FAILED--no ComponentID");
            return;
        }

        GameObject go = cID.gameObject;

        go.transform.position = position;
        go.transform.rotation = rotation;

        DCSource dc = go.GetComponent<DCSource>();
        if (dc != null) { dc.voltage = voltage; }

        Ohms res = go.GetComponent<Ohms>();
        if (res != null) { res.resistance = resistance; }

        LED_Component led = go.GetComponent<LED_Component>();
        if (led != null) { led.CurrentVoltage = ledVoltage; }

        Success($"{cID.id} state APPLIED");
    }

    // Debug output
    string splash = 
        $"{SaveManager.sysSplash}<color=#039BE5>[ObjectState] </color>";

    void Log(string msg) { Debug.Log($"{splash}{msg}"); }
    void Success(string msg) 
        { Debug.Log($"{splash}<color=green>{msg}</color>"); }
    void Warn(string msg) 
        { Debug.LogWarning($"{splash}<color=yellow>{msg}</color>"); }
    void Error(string msg) 
        { Debug.LogError($"{splash}<color=#B71C1C>{msg}</color>"); }
}

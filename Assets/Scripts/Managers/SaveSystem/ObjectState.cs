using System;
using UnityEngine;

[Serializable]
public class ObjectState
{
    [NonSerialized] SaveDebug d;
    SaveDebug D()
    {
        if (d == null)
            d = new SaveDebug("<color=#039BE5>[ObjectState] </color>");
        return d;
    }
    
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
        Capture_ObjectState(cID);
    }

    // For serializing
    public void Capture_ObjectState(ComponentID cID)
    {
        if (cID == null) 
        {
            D().Error($"CAPTURE STATE FAILED--no ComponentID");
            return;
        }

        D().Log($"CAPTURING component {cID.id} state");
        
        GameObject go = cID.gameObject;

        position = go.transform.position;
        rotation = go.transform.rotation;

        DCSource dc = go.GetComponent<DCSource>();
        if (dc != null) { voltage = dc.voltage; }

        Ohms res = go.GetComponent<Ohms>();
        if (res != null) { resistance = res.resistance; }

        LED_Component led = go.GetComponent<LED_Component>();
        if (led != null) { ledVoltage = led.CurrentVoltage; }

        d.Success($"{cID.id} state CAPTURED");
    }

    // For deserializing
    public void Apply_ObjectState(ComponentID cID)
    {
        if (cID == null) 
        {
            D().Error($"APPLY STATE FAILED--no ComponentID");
            return;
        }

        D().Log($"APPLYING saved state to component {cID.id}");

        // Split because only the fields need to be applied on Load()
        Apply_Transform(cID);
        Apply_Fields(cID);

        D().Success($"{cID.id} state APPLIED");
    }

    void Apply_Transform(Component cID)
    {
        GameObject go = cID.gameObject;

        go.transform.position = position;
        go.transform.rotation = rotation;
    }

    public void Apply_Fields(ComponentID cID)
    {
        GameObject go = cID.gameObject;

        DCSource dc = go.GetComponent<DCSource>();
        if (dc != null) { dc.voltage = voltage; }

        Ohms res = go.GetComponent<Ohms>();
        if (res != null) { res.resistance = resistance; }

        LED_Component led = go.GetComponent<LED_Component>();
        if (led != null) { led.CurrentVoltage = ledVoltage; }
    }
}

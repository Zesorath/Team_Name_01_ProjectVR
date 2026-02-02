using System;
using UnityEngine;

// CONSIDER: This might want to be a ISerializationCallbackReceiver

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
        Debug.Log($"[ObjectState]: Caturing component {cID.id} state");
        if (cID == null) 
        {
            Debug.Log($"[ObjectState]: CAPTURE STATE FAILED--no ComponentID");
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

        Debug.Log($"[ObjectState]: {cID.id} state CAPTURED");
    }

    // For deserializing
    public void Apply_ObjectState(ComponentID cID)
    {
        Debug.Log($"[ObjectState]: Applying saved state to component {cID.id}");
        if (cID == null) 
        {
            Debug.Log($"[ObjectState]: APPLY STATE FAILED--no ComponentID");
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

        Debug.Log($"[ObjectState]: {cID.id} state APPLIED");
    }
}

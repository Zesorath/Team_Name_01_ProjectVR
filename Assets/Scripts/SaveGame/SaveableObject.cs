using System;
using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public Guid id;
    public string label;

    void Awake()
    {
        // It's fine if SaveManager doesn't exist yet
        if (SaveManager.Instance == null) return;        
        
        // Only set a new ID if it is empty--e.g., pre-existing level components
        if (id == Guid.Empty)
        {
            ComponentType ct = GetComponent<ComponentType>();
            SaveManager.Type type = SaveManager.Type.OTHER; // Default value

            // ComponentType found
            if (ct != null) type = ct.type;

            id = Guid.NewGuid();
            label = SaveManager.Instance.GenerateLabel(type);
        }

        // Register the object
        SaveManager.Instance.RegisterSaveable(this);
    }

    public ObjectState StoreObjectState()
    {
        ObjectState state = new ObjectState();

        state.id = id;
        state.label = label;
        state.position = transform.position;
        state.rotation = transform.rotation;

        DCSource dc = GetComponent<DCSource>();
        if (dc != null) { state.voltage = dc.voltage; }

        Ohms res = GetComponent<Ohms>();
        if (res != null) { state.resistance = res.resistance; }

        LED_Component led = GetComponent<LED_Component>();
        if (led != null)
        {
            state.ledVoltage = led.CurrentVoltage;
        }

        
        if (GetComponent<GroundNode>() != null)
        {
            state.isGround = true;
        }

        return state;
    }

    public void ApplyObjectState(ObjectState state)
    {
        transform.position = state.position;
        transform.rotation = state.rotation;

        DCSource dc = GetComponent<DCSource>();
        if (dc != null) { dc.voltage = state.voltage; }

        Ohms res = GetComponent<Ohms>();
        if (res != null) { res.resistance = state.resistance; }

        LED_Component led = GetComponent<LED_Component>();
        if (led != null)
        {
            state.ledVoltage = led.CurrentVoltage;
        }        
    }
}

using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public string id;

    void Awake()
    {
        // It's fine if SaveManager doesn't exist yet
        if (SaveManager.Instance == null) return;        
        
        // Only set a new ID if it is empty--e.g., pre-existing level components
        if (string.IsNullOrEmpty(id))
        {
            ComponentType ct = GetComponent<ComponentType>();
            SaveManager.Type type = SaveManager.Type.OTHER; // Default value

            // ComponentType found
            if (ct != null) type = ct.type;

            id = SaveManager.Instance.GenerateID(type);
        }

        // Register the object
        SaveManager.Instance.RegisterSaveable(this);
    }

    public ObjectState StoreObjectState()
    {
        ObjectState state = new ObjectState();

        state.id = id;
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

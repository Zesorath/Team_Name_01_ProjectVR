using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public string id;

    void Awake()
    {
        // Only set a new ID if it is empty--e.g., pre-existing level components
        if (string.IsNullOrEmpty(id))
        {
            ComponentType ct = GetComponent<ComponentType>();
            SaveManager.Type type = SaveManager.Type.OTHER; // Default value

            // ComponentType found
            if (ct != null) type = ct.type;

            // SaveManager instance found
            if (SaveManager.Instance != null)
            {
                id = SaveManager.Instance.GenerateID(type);
            }
            else // Error if SaveManager instance not found
            {
                Debug.LogError(
                    $"No SaveManager instance found. Could not generate ID for {gameObject.name}"
                );
                return;
            }
        }

        // Safety checks passed and ID generated. Register the object
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.RegisterSaveable(this);
        }
        else
        {
            Debug.LogError(
                $"No SaveManager instance found. Could not register {gameObject.name}"
            );
        }
    }

    public ObjectState StoreObjectState()
    {
        ObjectState state = new ObjectState();
        
        state.id = id;
        state.position = transform.position;
        state.rotation = transform.rotation;
        
        Direct_Current dc = GetComponent<Direct_Current>();
        if (dc != null) {state.voltage = dc.voltage;}

        Ohms res = GetComponent<Ohms>();
        if (res != null) {state.resistance = res.resistance;}

        return state;
    }

    public void ApplyObjectState(ObjectState state)
    {
        transform.position = state.position;
        transform.rotation = state.rotation;

        Direct_Current dc = GetComponent<Direct_Current>();
        if (dc != null) {dc.voltage = state.voltage;}

        Ohms res = GetComponent<Ohms>();
        if (res != null) {res.resistance = state.resistance;}
    }
}

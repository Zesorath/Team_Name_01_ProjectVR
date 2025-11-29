using UnityEngine;

public class SaveableObject : MonoBehaviour
{
    public string id;
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

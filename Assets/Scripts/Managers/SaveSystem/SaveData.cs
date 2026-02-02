using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData : ISerializationCallbackReceiver
{
    public Guid saveID = Guid.NewGuid();
    public Dictionary<Guid, ObjectState> objectStates = 
        new Dictionary<Guid, ObjectState>();
    
    // Use for converting objectStates to a serializable form for ToJson()
    [Serializable]
    class Entry
    {
        public string id = "";
        public ObjectState state;
    }

    // Serializable format for objectStates
    [SerializeField] private List<Entry> states_serial = new List<Entry>();

    // SaveData -> serializable
    public void OnBeforeSerialize()
    {
        Debug.Log("[SaveData]: OnBeforeSerialize firing");

        states_serial.Clear();
        foreach (var kvp in objectStates)
        {
            states_serial.Add(
                new Entry { id = kvp.Key.ToString(), state = kvp.Value }
            );
        }

        Debug.Log($"[SaveData]: Serialized {states_serial.Count} objects");
    }

    // TODO: Handle cIDs dictionary
    // Serializable -> SaveData
    public void OnAfterDeserialize()
    {
        Debug.Log("[SaveData]: OnAfterDeserialize firing");

        objectStates.Clear();
        foreach (var s in states_serial)
        {
            objectStates.Add( Guid.Parse(s.id), s.state );
        }

        Debug.Log($"[SaveData]: Deserialized {objectStates.Count} objects");
    }
}

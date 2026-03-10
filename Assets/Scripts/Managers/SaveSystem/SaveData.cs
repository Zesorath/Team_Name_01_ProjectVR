using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData : ISerializationCallbackReceiver
{
    [NonSerialized]
    readonly SaveDebug d = new SaveDebug("<color=#1565C0>[SaveData] </color>");
    
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
        d.Log("OnBeforeSerialize firing");

        states_serial.Clear();
        foreach (var kvp in objectStates)
        {
            states_serial.Add(
                new Entry { id = kvp.Key.ToString(), state = kvp.Value }
            );
        }

        d.Success($"SERIALIZED {states_serial.Count} objects");
    }

    // Serializable -> SaveData
    public void OnAfterDeserialize()
    {
        d.Log("OnAfterDeserialize firing");

        objectStates.Clear();
        foreach (var s in states_serial)
        {
            objectStates.Add( Guid.Parse(s.id), s.state );
        }

        d.Success($"DESERIALIZED {objectStates.Count} objects");
    }
}

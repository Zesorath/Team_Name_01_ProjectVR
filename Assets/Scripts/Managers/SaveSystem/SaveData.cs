using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class SaveData : ISerializationCallbackReceiver
{
    [NonSerialized]
    SaveDebug d;
    SaveDebug D()
    {
        if (d == null)
            d = new SaveDebug("<color=#1565C0>[SaveData] </color>");
        return d;
    }
    
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

    public SaveData() {}

    // Returns a deep copy of other
    public SaveData(SaveData other)
    {
        string other_serial = JsonUtility.ToJson(other);
        JsonUtility.FromJsonOverwrite(other_serial, this);
    }

    // Empty the current SaveData object
    public void Reset()
    {
        objectStates.Clear();
        states_serial.Clear();
    }

    // SaveData -> serializable
    public void OnBeforeSerialize()
    {
        D().Log("OnBeforeSerialize firing");

        states_serial.Clear();
        foreach (var kvp in objectStates)
        {
            states_serial.Add(
                new Entry { id = kvp.Key.ToString(), state = kvp.Value }
            );
        }

        D().Success($"SERIALIZED {states_serial.Count} objects");
    }

    // Serializable -> SaveData
    public void OnAfterDeserialize()
    {
        D().Log("OnAfterDeserialize firing");

        objectStates.Clear();
        foreach (var s in states_serial)
        {
            objectStates.Add( Guid.Parse(s.id), s.state );
        }

        D().Success($"DESERIALIZED {objectStates.Count} objects");
    }
}

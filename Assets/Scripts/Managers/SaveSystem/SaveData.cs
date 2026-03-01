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
        Log("OnBeforeSerialize firing");

        states_serial.Clear();
        foreach (var kvp in objectStates)
        {
            states_serial.Add(
                new Entry { id = kvp.Key.ToString(), state = kvp.Value }
            );
        }

        Success($"SERIALIZED {states_serial.Count} objects");
    }

    // Serializable -> SaveData
    public void OnAfterDeserialize()
    {
        Log("OnAfterDeserialize firing");

        objectStates.Clear();
        foreach (var s in states_serial)
        {
            objectStates.Add( Guid.Parse(s.id), s.state );
        }

        Success($"DESERIALIZED {objectStates.Count} objects");
    }

    // Debug output
    string splash = 
        $"{SaveManager.sysSplash}<color=#1565C0>[SaveData] </color>";

    void Log(string msg) { Debug.Log($"{splash}{msg}"); }
    void Success(string msg) 
        { Debug.Log($"{splash}<color=green>{msg}</color>"); }
    void Warn(string msg) 
        { Debug.LogWarning($"{splash}<color=yellow>{msg}</color>"); }
    void Error(string msg) 
        { Debug.LogError($"{splash}<color=#B71C1C>{msg}</color>"); }
}

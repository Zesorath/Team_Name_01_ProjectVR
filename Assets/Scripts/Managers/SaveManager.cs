using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();
    public readonly ComponentTypes types;
    SaveData saveData;
    class SaveData
    {
        internal string fileName = "saveFile.json";
        internal Guid saveID;
        internal Dictionary<Guid, ObjectState> objectStates;
        
        internal SaveData()
        {
            saveID = Guid.NewGuid();
            objectStates = new Dictionary<Guid, ObjectState>();
        }
    }
    
    SaveManager() 
    { 
        types = new ComponentTypes();
        saveData = new SaveData();
    }

    [Serializable]
    class SaveData_Serialized : ISerializationCallbackReceiver
    {
        public string saveID;
        public List<Entry> objectStates = new List<Entry>();
        
        [Serializable]
        public class Entry
        {
            public string id = "";
            public ObjectState state;
        }

        public void OnBeforeSerialize()
        {
            saveID = Instance.saveData.saveID.ToString();

            objectStates.Clear();
            foreach (var kvp in Instance.saveData.objectStates)
            {
                objectStates.Add(new Entry { id=kvp.Key.ToString(), state=kvp.Value });
            }
            Debug.Log($"Serialized {objectStates.Count} objects");
        }

        public void OnAfterDeserialize()
        {
            Dictionary<Guid,ObjectState> s = Instance.saveData.objectStates;
            if (s == null) 
            {
                s = new Dictionary<Guid, ObjectState>();
                Instance.saveData.objectStates = s; 
            }

            // Use min length just for safety. They should be the same length
            for (int i = 0; i < objectStates.Count; i++)
            {
                // var val = JsonUtility.FromJson<ObjectState>(states[i]);
                // s.Add(Guid.Parse(ids[i]), val);
            }
        }
    }

    public StatusCode Register(ComponentID cID)
    {
        Debug.Log($"[SaveManager]: Attempting to register component {cID.id}");
        // Gatekeeping
        if (cID.id == Guid.Empty) return StatusCode.ERROR_ID_GEN_FAILED;
        if (saveData.objectStates.ContainsKey(cID.id))
            return StatusCode.ERROR_DUPLICATE_ID;
        
        // Register the new component
        ObjectState newObjectState = new ObjectState();
        newObjectState.Build_ObjectState(cID);
        saveData.objectStates.Add(cID.id, newObjectState);
        Debug.Log($"[SaveManager]: Component {cID.id} successfully registered");
        return StatusCode.SUCCESS;
    }

    [Serializable]
    class ObjectState
    {
        public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
        public int index = 0;
        public string label = "NO_LABEL";
        public Vector3 position = Vector3.zero;
        public Quaternion rotation = Quaternion.identity;
        public float voltage = 0;
        public float resistance = 0;
        public float ledVoltage = 0;
        public bool isGround = false;

        internal StatusCode Build_ObjectState(ComponentID cID)
        {
            Debug.Log($"Building component {cID.id}");
            
            if (cID == null) return StatusCode.ERROR_MISSING_COMPONENT;
            
            label = cID.label;
            type = cID.type;
            index = cID.index;
            
            if (cID.gameObject == null)
                return StatusCode.ERROR_MISSING_GAME_OBJECT;
            GameObject go = cID.gameObject;

            position = go.transform.position;
            rotation = go.transform.rotation;

            DCSource dc = go.GetComponent<DCSource>();
            if (dc != null) { voltage = dc.voltage; }

            Ohms res = go.GetComponent<Ohms>();
            if (res != null) { resistance = res.resistance; }

            LED_Component led = go.GetComponent<LED_Component>();
            if (led != null) { ledVoltage = led.CurrentVoltage; }

            if (go.GetComponent<GroundNode>() != null) { isGround = true; }

            return StatusCode.SUCCESS;
        }

        // REWRITE FOR LOAD()
        internal StatusCode Apply_ObjectState() { return StatusCode.SUCCESS; }

        public void OLD_Apply_ObjectState(GameObject go)
        {
            go.transform.position = position;
            go.transform.rotation = rotation;

            DCSource dc = go.GetComponent<DCSource>();
            if (dc != null) { dc.voltage = voltage; }

            Ohms res = go.GetComponent<Ohms>();
            if (res != null) { res.resistance = resistance; }

            LED_Component led = go.GetComponent<LED_Component>();
            if (led != null) { led.CurrentVoltage = ledVoltage; }
        }
    }

    public void Save()
    {       
        string path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            Instance.saveData.fileName
        );

        SaveData_Serialized s = new SaveData_Serialized();

        StreamWriter writer = new StreamWriter(path, append: false);
        writer.WriteLine(JsonUtility.ToJson(s, true));
        writer.Close();
    }

    public void Load() {}
}

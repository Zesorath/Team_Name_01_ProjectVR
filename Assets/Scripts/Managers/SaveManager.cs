using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using UnityEditor;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();
    public readonly ComponentTypes types;
    SaveData saveData;

    // SaveManager methods

    /// <summary>
    /// SaveManager constructor. Initializes type counters/name strings and
    /// empty saveData object.
    /// </summary>
    SaveManager() 
    { 
        types = new ComponentTypes();
        saveData = new SaveData();
    }

    /// <summary>
    /// Used by ComponentID. Each component registers itself with the save 
    /// manager upon spawn.
    /// </summary>
    public StatusCode Register(ComponentID cID)
    {
        Debug.Log($"[SaveManager]: Attempting to register component {cID.id}");
        // Gatekeeping
        if (cID.id == Guid.Empty) return StatusCode.ERROR_ID_GEN_FAILED;
        if (saveData.objectStates.ContainsKey(cID.id))
            return StatusCode.ERROR_DUPLICATE_ID;
        
        // Register the new component
        saveData.objectStates.Add(cID.id, new ObjectState(cID));
        Debug.Log($"[SaveManager]: Component {cID.id} successfully registered");
        return StatusCode.SUCCESS;
    }
    
    /// <summary>
    /// Saves the current scene to a JSON file on the user's desktop.
    /// </summary>
    public void Save()
    {       
        Debug.Log("Save() called");
        string path = Instance.saveData.path;

        // Scan the scene for saveable objects (ComponentIDs)
        foreach (ComponentID cID in 
            UnityEngine.Object.FindObjectsByType<ComponentID>(
                FindObjectsSortMode.None))
        {
            Instance.saveData.objectStates[cID.id].Snapshot_ObjectState(cID);
        }

        // Serialize
        SaveData_Serialized s = new SaveData_Serialized();
        try
        {
            using (StreamWriter sw = new StreamWriter(path, append: false))
            {
                sw.WriteLine(JsonUtility.ToJson(s, prettyPrint: true));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"{Instance.saveData.fileName} not created:");
            Console.WriteLine(e.Message);
        }
    }

    /// <summary>
    /// Loads a saved scene from a JSON file on the user's desktop.
    /// </summary>
    public void Load() {
        Debug.Log("Load() called");
        
        // Scan the scene for saveable objects before loading
        Dictionary<Guid, ComponentID> sceneCIDs = 
            new Dictionary<Guid, ComponentID>();
        foreach (ComponentID cID in 
            UnityEngine.Object.FindObjectsByType<ComponentID>(
                FindObjectsSortMode.None))
        {
            if (sceneCIDs.ContainsKey(cID.id))
            {
                Debug.Log(StatusCode.ERROR_DUPLICATE_ID);
                Debug.Log("Load() failed");
                return;
            }
            sceneCIDs.Add(cID.id, cID);
        }

        // Get the data from the save file
        string path = Instance.saveData.path;
        try
        {
            JsonUtility.FromJson<SaveData_Serialized>(File.ReadAllText(path));
        }
        catch (Exception e)
        {
            Debug.Log($"Could not open {Instance.saveData.fileName}: ");
            Debug.Log(e.Message);
            return;
        }

        // Scene objects will either be spawned, deleted, or updated
        List<Guid> idsToSpawn = new List<Guid>();
        List<Guid> idsToDelete = new List<Guid>();
        List<Guid> idsToUpdate = new List<Guid>();
        foreach (var kvp in sceneCIDs)
        {
            if (Instance.saveData.objectStates.ContainsKey(kvp.Key))
            { idsToUpdate.Add(kvp.Key); }
            else { idsToDelete.Add(kvp.Key); }
        }
        foreach (var kvp in Instance.saveData.objectStates)
        {
            if (!sceneCIDs.ContainsKey(kvp.Key)) { idsToSpawn.Add(kvp.Key); }
        }

        // Delete objects that exist in the current scene but not in the loaded
        // save file
        foreach (Guid id in idsToDelete)
        {
            UnityEngine.Object.Destroy(sceneCIDs[id].gameObject);
        }

        // Update objects present in current scene and loaded data
        foreach (Guid id in idsToUpdate)
        {
            Instance.saveData.objectStates[id].Apply_ObjectState(sceneCIDs[id]);
        }

        // LATER: Spawn objects that exist in loaded save file but not in the
        // current scene. Just log unfinished for now
        int ts = idsToSpawn.Count;
        if (ts > 0) Debug.Log(
            $"Spawn from save not implemented yet. {ts} items not spawned");

        // Get max index for each component type, then restore type counts
        int[] maxTypeIndices = new int[(int)ComponentTypes.Types.TYPES_COUNT];
        foreach (var kvp in Instance.saveData.objectStates)
        {
            int chkType = (int)kvp.Value.type;
            if (kvp.Value.index > maxTypeIndices[chkType])
            {
                maxTypeIndices[chkType] = kvp.Value.index;
            }
        }
        types.RestoreTypeCounters(maxTypeIndices);
    }


    // HELPER CLASSES

    /// <summary>
    /// Contains the name and unique ID of the save file, and a dictionary of
    /// ObjectStates indexed by ComponentID.id (a GUID).
    /// </summary>
    class SaveData
    {
        internal string fileName = "saveFile.json";
        internal string path;
        internal Guid saveID;
        internal Dictionary<Guid, ObjectState> objectStates;
        
        /// <summary>
        /// SaveData constructor
        /// </summary>
        internal SaveData()
        {
            path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                fileName
            );
            saveID = Guid.NewGuid();
            objectStates = new Dictionary<Guid, ObjectState>();
        }
    }

    /// <summary>
    /// saveData formatted for automatic JSON serialization
    /// </summary>
    [Serializable]
    class SaveData_Serialized : ISerializationCallbackReceiver
    {
        public string saveID_serial = Instance.saveData.saveID.ToString();
        public List<Entry> states_serial = new List<Entry>();
        
        /// <summary>
        /// ToJson() doesn't support Dictionaries, but it can handle a list of
        /// instances of a custom class with all serializable fields. Entry acts
        /// as a key, value pair for serialization.
        /// </summary>
        [Serializable]
        public class Entry
        {
            public string id = "";
            public ObjectState state;
        }

        /// <summary>
        /// Convert saveID from GUID to string and saveData.objectStates from 
        /// Dictionary<Guid, ObjectState> to List<Entry>
        /// </summary>
        public void OnBeforeSerialize()
        {
            states_serial.Clear();
            foreach (var kvp in Instance.saveData.objectStates)
            {
                states_serial.Add(
                    new Entry { id=kvp.Key.ToString(), state=kvp.Value });
            }
            Debug.Log($"Serialized {states_serial.Count} objects");
        }

        // TODO: Finish
        /// <summary>
        /// Convert saveID_serial back to GUID and states_serial back to a
        /// dictionary
        /// </summary>
        public void OnAfterDeserialize()
        {
            Debug.Log("[SaveData_Serialized] OnAfterDeserialize fired");

            Instance.saveData.saveID = Guid.Parse(saveID_serial);
            Instance.saveData.objectStates.Clear();

            // Re-build saveData from the file
            foreach (var s in states_serial)
            {
                Instance.saveData.objectStates.Add(Guid.Parse(s.id), s.state);
            }
        }
    }

    /// <summary>
    /// Stores the state of a single component for saving/loading
    /// </summary>
    [Serializable]
    class ObjectState
    {
        public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
        public int index;
        public string label = "";
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

        /// <summary>
        /// Populates ObjectState fields from a ComponentID and its GameObject's
        /// relevant components.
        /// </summary>
        internal StatusCode Snapshot_ObjectState(ComponentID cID)
        {
            Debug.Log($"Building state for component {cID.id}");
            
            if (cID == null) return StatusCode.ERROR_MISSING_COMPONENT;
            
            // No null check needed, because go can't exist without cID
            GameObject go = cID.gameObject;

            position = go.transform.position;
            rotation = go.transform.rotation;

            DCSource dc = go.GetComponent<DCSource>();
            if (dc != null) { voltage = dc.voltage; }

            Ohms res = go.GetComponent<Ohms>();
            if (res != null) { resistance = res.resistance; }

            LED_Component led = go.GetComponent<LED_Component>();
            if (led != null) { ledVoltage = led.CurrentVoltage; }

            return StatusCode.SUCCESS;
        }

        /// <summary>
        /// Repopulates the GameObject's fields from an ObjectState
        /// </summary>
        internal StatusCode Apply_ObjectState(ComponentID cID) {
            Debug.Log($"Applying state to component {cID.id}");

            if (cID == null) return StatusCode.ERROR_MISSING_COMPONENT;

            cID.label = label;
            cID.type = type;
            cID.index = index;
            
            GameObject go = cID.gameObject;

            go.transform.position = position;
            go.transform.rotation = rotation;

            DCSource dc = go.GetComponent<DCSource>();
            if (dc != null) { dc.voltage = voltage; }

            Ohms res = go.GetComponent<Ohms>();
            if (res != null) { res.resistance = resistance; }

            LED_Component led = go.GetComponent<LED_Component>();
            if (led != null) { led.CurrentVoltage = ledVoltage; }

            return StatusCode.SUCCESS;
        }
    }
}

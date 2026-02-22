using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Linq;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();
    public readonly ComponentTypes types;
    public readonly SavePaths paths;
    public SaveManifest man;
    public bool isLoadingOrSaving = false;

    SaveData saveData;
    public Dictionary<Guid, ComponentID> cIDs;
    
    SaveManager()
    {
        types = new ComponentTypes();
        paths = new SavePaths();
        man = new SaveManifest();

        saveData = new SaveData();
        cIDs = new Dictionary<Guid, ComponentID>();
    }

    // Called by ComponentID to register spawned components with save manager
    public void Register(ComponentID cID)
    {
        Log($"REGISTERING component {cID.id}");
        
        // Registration failure
        string errMsg = $"FAILED TO REGISTER {cID.id}--";
        if (cID.id == Guid.Empty) 
            { Error($"{errMsg}ID generation failed"); return; }
        if (cIDs.ContainsKey(cID.id)) 
            { Error($"{errMsg}Duplicate ID"); return; }

        // Register component
        cIDs.Add(cID.id, cID);
        if (!saveData.objectStates.ContainsKey(cID.id))
            saveData.objectStates.Add(cID.id, new ObjectState(cID));
        
        Success($"Component {cID.id} REGISTERED");
    }

    // TODO: Make this accept only the Guid, since we have the cIDs dictionary
    // now. It'll give better error readout.
    public void Unregister(ComponentID cID)
    {
        // ComponentID not found
        if (cID == null) 
            { Error($"UNREGISTER FAILED--NOT FOUND"); return; }
        
        Log($"UNREGISTERING component {cID.id}");

        // Unregister component
        cIDs.Remove(cID.id);
        saveData.objectStates.Remove(cID.id);

        Success($"Component {cID.id} UNREGISTERED");
    }
    
    public void QuickSave() { Save(); }

    // Saves the current scene state to file
    public void Save()
    {       
        Log("Save() CALLED");

        // TODO: FINISH
        // Disable interactions while actively saving/loading (currently stub)
        isLoadingOrSaving = true;
        
        // Capture current state of all registered scene objects
        foreach (ComponentID cID in cIDs.Values)
            { saveData.objectStates[cID.id].Capture_ObjectState(cID); }
        
        // Create the saveFiles folder, if it does not exist
        string sfp = paths.saveFilesPath;
        Log($"Searching for save directory {sfp}");
        if (Directory.Exists(sfp)) 
            { Success($"{sfp} FOUND. Saving"); }
        else
        {
            Warn($"{sfp} NOT FOUND. Creating");
            Directory.CreateDirectory(sfp);
        }

        // Serialize and write to file
        man.lastSave = $"{saveData.saveID}.json";
        string sfPath = Path.Combine(sfp, man.lastSave);
        try
        {
            using (StreamWriter sw = new StreamWriter(sfPath, append: false))
            {
                sw.WriteLine(JsonUtility.ToJson(saveData, prettyPrint: true));
            }
            Success($"SAVED to {sfPath}");
        }
        catch (Exception e)
            { Error($"SAVE FAILED--{e.Message}"); }
        
        // TODO: FINISH
        // Re-enable interactions
        isLoadingOrSaving = false;
    }

    public void QuickLoad() { Load(man.lastSave); }

    // Load a saved scene from the save file. Compares current registered
    // ComponentIDs in cIDs with the de-serialized saveData to determine which
    // objects to update, delete, or spawn.
    public void Load(string saveFileName) 
    {
        Log("Load() CALLED");

        // TODO: FINISH
        // Disable interactions while actively saving/loading (currently stub)
        isLoadingOrSaving = true;

        // Deserialize saveData from the file
        string sfPath = Path.Combine(paths.saveFilesPath, saveFileName);
        try
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(sfPath), saveData);
            Success($"LOADED from {sfPath}");
        }
        catch (Exception e)
            { Error($"LOAD FAILED--{e.Message}"); return; }
        
        // PRUNE: Delete objects in the current scene and not in the save file
        int expectDelCt = Math.Max(0, cIDs.Count - saveData.objectStates.Count);
        Log($"PRUNE expected to delete {expectDelCt}");
        
        int dCt = 0;
        Guid[] liveIDs = cIDs.Keys.ToArray();
        foreach (Guid id in liveIDs)
        {           
            if (!saveData.objectStates.ContainsKey(id))
            {
                Log($"PRUNE deleting {id}");
                cIDs[id].Delete();
                dCt++;
            }
        }

        // UPDATE objects that are in both current scene and save file
        int uCt = 0;
        foreach (Guid id in cIDs.Keys)
            { saveData.objectStates[id].Apply_ObjectState(cIDs[id]); uCt++; }

        // TODO: Finish
        // SPAWN objects in the save file but not the current scene
        int expectSpnCt = Math.Max(0, saveData.objectStates.Count - cIDs.Count);
        Log($"SPAWN expected to spawn {expectSpnCt}");

        int sCt = 0;
        foreach (Guid id in saveData.objectStates.Keys)
        {
            
            if (cIDs.ContainsKey(id)) { continue; }

            Log($"SPAWN creating {id}");

            ObjectState state = saveData.objectStates[id];
            GameObject prefab = ResolvePrefab(state.type);
            if (prefab == null) continue;

            GameObject go = UnityEngine.Object.Instantiate(
                prefab, state.position, state.rotation
            );

            ComponentID cID = go.GetComponent<ComponentID>();
            if (cID == null)
            {
                string msg = $"Prefab {prefab.name} missing ComponentID";
                Error($"SPAWN failed--{msg}");
                UnityEngine.Object.Destroy(go);
                continue;
            }

            // Populate the spawned object. TODO: Rework ComponentID class so
            // this doesn't all have to be done manually
            cID.id = id;
            cID.label = state.label;
            cID.index = state.index;

            state.Apply_Fields(cID);

            Register(cID);
            cID.MarkRegistered();
                
            sCt++;
        }

        // Restore type counts from loaded objects
        types.RestoreTypeCounters(saveData);
        
        // Display successful load stats
        Log($"UPDATED: {uCt} ; DELETED: {dCt} ; SPAWNED: {sCt}");

        // TODO: FINISH
        // Re-enable interactions
        isLoadingOrSaving = false;
    }

    // Get the necessary prefab for spawn-from-file
    GameObject ResolvePrefab(ComponentTypes.Types type)
    {
        string key = type.ToString();
        GameObject prefab = Resources.Load<GameObject>($"Components/{key}");

        if (prefab == null)
        {
            string pfPath = $"Resources/Components/{key}.prefab";
            Error($"No prefab found at {pfPath}");
        }

        return prefab;
    }

    // Debug output
    public static string sysSplash = "<color=#FFFFFF>[SaveSystem]</color>";
    string splash = $"<color=#29B6F6>[SaveManager] </color>";

    void Log(string msg) { Debug.Log($"{sysSplash}{splash}{msg}"); }
    void Success(string msg) 
        { Debug.Log($"{sysSplash}{splash}<color=green>{msg}</color>"); }
    void Warn(string msg) 
        { Debug.LogWarning($"{sysSplash}{splash}<color=yellow>{msg}</color>"); }
    void Error(string msg) 
        { Debug.LogError($"{sysSplash}{splash}<color=#B71C1C>{msg}</color>"); }
}

using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Linq;
using Unity.XR.Management.AndroidManifest.Editor;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

public class SaveManager
{
    readonly SaveDebug d = 
        new SaveDebug("<color=#29B6F6>[SaveManager] </color>");
    
    public static SaveManager Instance { get; } = new SaveManager();
    public SaveManifest man;
    public readonly ComponentTypes types;
    public readonly SavePaths paths;
    public bool isLoadingOrSaving = false;
    bool isInitialized = false;
    
    SaveData saveData;
    public Dictionary<Guid, ComponentID> cIDs;

    SaveManager()
    {
        man = new SaveManifest();
        types = new ComponentTypes();
        paths = new SavePaths();
        saveData = new SaveData();
        cIDs = new Dictionary<Guid, ComponentID>();
    }

    // Load the save manifest into memory
    public void Init()
    {
        if (isInitialized == true) return;
        // Retrieve save manifest JSON from file
        paths.EnsureSaveFolderExists();
        paths.EnsureManifestFileExists();
        string man_serial = ReadJsonFromFile(paths.manFilePath);
        if (man_serial == "")
        {
            d.Error("MANIFEST LOAD FAILED. Terminating program.");
            GameManager.ExitGame();
        }

        // Deserialize to the man object
        JsonUtility.FromJsonOverwrite(man_serial, man);
        
        // Mark as initialized
        isInitialized = true;
    }

    // Called by ComponentID to register spawned components with save manager
    public void Register(ComponentID cID)
    {
        d.Log($"REGISTERING component {cID.id}");
        
        // Registration failure
        string errMsg = $"FAILED TO REGISTER {cID.id}--";
        if (cID.id == Guid.Empty) 
            { d.Error($"{errMsg}ID generation failed"); return; }
        if (cIDs.ContainsKey(cID.id)) 
            { d.Error($"{errMsg}Duplicate ID"); return; }

        // Register component
        cIDs.Add(cID.id, cID);
        if (!saveData.objectStates.ContainsKey(cID.id))
            saveData.objectStates.Add(cID.id, new ObjectState(cID));
        
        d.Success($"Component {cID.id} REGISTERED");
    }

    // TODO: Make this accept only the Guid, since we have the cIDs dictionary
    // now. It'll give better error readout.
    public void Unregister(ComponentID cID)
    {
        // ComponentID not found
        if (cID == null) 
            { d.Error($"UNREGISTER FAILED--NOT FOUND"); return; }
        
        d.Log($"UNREGISTERING component {cID.id}");

        // Unregister component
        cIDs.Remove(cID.id);
        saveData.objectStates.Remove(cID.id);

        d.Success($"Component {cID.id} UNREGISTERED");
    }
    
    public void QuickSave() { Save(); }
    public void SaveToSlot(int slotNo)
    {
        d.Log($"STUB: Saving to slot {slotNo}");
    }

    // Saves the current scene state to file
    public void Save()
    {       
        d.Log("Save() CALLED");

        // TODO: FINISH
        // Disable interactions while actively saving/loading (currently stub)
        isLoadingOrSaving = true;
        
        // Capture current state of all registered scene objects
        foreach (ComponentID cID in cIDs.Values)
            { saveData.objectStates[cID.id].Capture_ObjectState(cID); }

        // Serialize and write to most recent file
        man.lastSave = $"{saveData.saveID}.json";
        string sfPath = Path.Combine(paths.saveFilesPath, man.lastSave);
        string saveData_serial = JsonUtility.ToJson(saveData, prettyPrint:true);
        WriteJsonToFile(sfPath, saveData_serial);
        
        // TODO: FINISH
        // Re-enable interactions
        isLoadingOrSaving = false;
    }

    public void QuickLoad() { Load(man.lastSave); }
    public void LoadFromSlot(int slotNo)
    {
        d.Log($"STUB: Loading from slot {slotNo}");
    }

    // Load a saved scene from the save file. Compares current registered
    // ComponentIDs in cIDs with the de-serialized saveData to determine which
    // objects to update, delete, or spawn.
    public void Load(string saveFileName) 
    {
        d.Log("Load() CALLED");

        // TODO: FINISH
        // Disable interactions while actively saving/loading (currently stub)
        isLoadingOrSaving = true;

        // Retrieve saveData JSON string from the file
        string sfPath = Path.Combine(paths.saveFilesPath, saveFileName);
        string saveData_serial = ReadJsonFromFile(sfPath);
        if (saveData_serial == "") { d.Error("LOAD FAILED"); return; }
        
        // Deserialize to the saveData object
        JsonUtility.FromJsonOverwrite(saveData_serial, saveData);
        
        // PRUNE: Delete objects in the current scene and not in the save file
        int expectDelCt = Math.Max(0, cIDs.Count - saveData.objectStates.Count);
        d.Log($"PRUNE expected to delete {expectDelCt}");
        
        int dCt = 0;
        Guid[] liveIDs = cIDs.Keys.ToArray();
        foreach (Guid id in liveIDs)
        {           
            if (!saveData.objectStates.ContainsKey(id))
            {
                d.Log($"PRUNE deleting {id}");
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
        d.Log($"SPAWN expected to spawn {expectSpnCt}");

        int sCt = 0;
        foreach (Guid id in saveData.objectStates.Keys)
        {
            
            if (cIDs.ContainsKey(id)) { continue; }

            d.Log($"SPAWN creating {id}");

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
                d.Error($"SPAWN failed--{msg}");
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
        d.Log($"UPDATED: {uCt} ; DELETED: {dCt} ; SPAWNED: {sCt}");

        // TODO: FINISH
        // Re-enable interactions
        isLoadingOrSaving = false;
    }

    // HELPERS

    void WriteJsonToFile(string path, string jsonData)
    {
        try
        {
            File.WriteAllText(path, jsonData);
            d.Success($"WROTE to {path}");
        }
        catch (Exception e)
            { d.Error($"WRITE FAILED--{e.Message}"); }
    }

    string ReadJsonFromFile(string path)
    {
        string jsonData = "";
        try
        {
            jsonData = File.ReadAllText(path);
            d.Success($"READ from {path}");
        }
        catch (Exception e)
            { d.Error($"READ FAILED--{e.Message}"); }
        
        return jsonData;
    }

    // Get the necessary prefab for spawn-from-file
    GameObject ResolvePrefab(ComponentTypes.Types type)
    {
        string key = type.ToString();
        GameObject prefab = Resources.Load<GameObject>($"Components/{key}");

        if (prefab == null)
        {
            string pfPath = $"Resources/Components/{key}.prefab";
            d.Error($"No prefab found at {pfPath}");
        }

        return prefab;
    }
}

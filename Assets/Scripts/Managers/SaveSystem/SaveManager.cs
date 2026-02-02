using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using UnityEditor;
using System.Linq;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

public class SaveManager
{
    public static SaveManager Instance { get; } = new SaveManager();
    public readonly ComponentTypes types;
    public readonly SavePaths paths;
    public SaveManifest man;

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
        Debug.Log($"[SaveManager]: Registering component {cID.id}");
        
        // Registration failure
        string errMsg = $"[SaveManager]: FAILED TO REGISTER {cID.id}--";
        if (cID.id == Guid.Empty)
            { Debug.Log(errMsg + "ID generation failed"); return; }
        if (cIDs.ContainsKey(cID.id))
            { Debug.Log(errMsg + "Duplicate ID"); return; }

        // Register component
        cIDs.Add(cID.id, cID);
        saveData.objectStates.Add(cID.id, new ObjectState(cID));
        
        Debug.Log($"[SaveManager]: Component {cID.id} REGISTERED");
    }

    public void Unregister(ComponentID cID)
    {
        // ComponentID not found
        string errMsg = $"[SaveManager]: UNREGISTER FAILED--";
        if (cID == null) 
            { Debug.Log($"{errMsg}NOT FOUND"); return; }
        
        Debug.Log($"[SaveManager]: Unregistering component {cID.id}");

        // Unregister component
        cIDs.Remove(cID.id);
        saveData.objectStates.Remove(cID.id);

        Debug.Log($"[SaveManager]: Component {cID.id} UNREGISTERED");
    }
    
    public void QuickSave() { Save(); }
    // Saves the current scene state to file
    public void Save()
    {       
        Debug.Log("[SaveManager]: Save() CALLED");
        
        // Capture current state of all registered scene objects
        foreach (ComponentID cID in cIDs.Values)
            { saveData.objectStates[cID.id].Capture_ObjectState(cID); }
        
        // Create the saveFiles folder, if it does not exist
        string sfp = paths.saveFilesPath;
        Debug.Log($"[SaveManager]: Searching for save directory {sfp}");
        if (Directory.Exists(sfp)) 
            { Debug.Log($"[SaveManager]: {sfp} FOUND. Saving"); }
        else
        {
            Directory.CreateDirectory(sfp);
            Debug.Log($"[SaveManager]: CREATED {sfp}");
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
            Debug.Log($"[SaveManager]: SAVED to {sfPath}");
        }
        catch (Exception e)
            { Debug.Log($"[SaveManager]: SAVE FAILED--{e.Message}"); }
    }

    public void QuickLoad() { Load(man.lastSave); }

    // Load a saved scene from the save file. Compares current registered
    // ComponentIDs in cIDs with the de-serialized saveData to determine which
    // objects to update, delete, or spawn.
    public void Load(string saveFileName) 
    {
        Debug.Log("[SaveManager]: Load() CALLED");

        // Deserialize saveData from the file
        string sfPath = Path.Combine(paths.saveFilesPath, saveFileName);
        try
        {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(sfPath), saveData);
            Debug.Log($"[SaveManager]: LOADED from {sfPath}");
        }
        catch (Exception e)
            { Debug.Log($"[SaveManager]: LOAD FAILED--{e.Message}"); return; }
        
        // PRUNE: Delete objects in the current scene but not in the save file
        int expectDelCt = cIDs.Count - saveData.objectStates.Count;
        if (expectDelCt < 0) expectDelCt = 0;
        Debug.Log($"[SaveManager]: PRUNE expected to delete {expectDelCt}");
        
        int dCt = 0;
        Guid[] liveIDs = cIDs.Keys.ToArray();
        foreach (Guid id in liveIDs)
        {
            // Delete() is in CircuitComponentBase class
            GameObject go = cIDs[id].gameObject;
            CircuitComponentBase ccb = go.GetComponent<CircuitComponentBase>();
            
            if (!saveData.objectStates.ContainsKey(id))
            {
                Debug.Log($"[SaveManager]: PRUNE deleting {id}");
                ccb.Delete();
                dCt++;
            }
        }

        // UPDATE objects that are in both current scene and save file
        int uCt = 0;
        foreach (Guid id in cIDs.Keys)
            { saveData.objectStates[id].Apply_ObjectState(cIDs[id]); uCt++; }

        // TODO: Finish
        // SPAWN objects in the save file but not the current scene
        int expectSpnCt = saveData.objectStates.Count - cIDs.Count;
        if (expectSpnCt < 0) expectSpnCt = 0;
        Debug.Log($"[SaveManager]: SPAWN expected to spawn {expectSpnCt}");

        int sCt = 0;
        foreach (Guid id in saveData.objectStates.Keys)
        {
            if (!cIDs.ContainsKey(id)) sCt++;
        }

        // Restore type counts from loaded objects
        types.RestoreTypeCounters(saveData);
        
        // Display successful load stats
        string loadStats = $"Objects UPDATED: {uCt} ; ";
        loadStats += $"Objects DELETED: {dCt} ; ";
        loadStats += $"Objects TO SPAWN: {sCt}";
        Debug.Log($"[SaveManager]: {loadStats}");
    }
}

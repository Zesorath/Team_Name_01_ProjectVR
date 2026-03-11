using UnityEngine;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Linq;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEngine.SceneManagement;
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
    
    SaveSlot activeSlot;
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
        cID.MarkRegistered();
        
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
    
    // SAVE

    public void QuickSave()
    { 
        d.Log("QuickSave() CALLED");
        
        // Account for level completion after last save.
        // TODO: Add SaveData reset
        string currentLevel = SceneManager.GetActiveScene().name;
        if (currentLevel != activeSlot.Get_LevelData())
        {
            activeSlot.Set_LevelData(currentLevel);
        }
        
        // Grab the timestamp
        activeSlot.Capture_WhenLastUsed();

        // Re-serialize the manifest
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);

        // Then save
        Save(activeSlot.Get_FilePath());
    }

    public void SaveToSlot(int slotNo)
    {
        d.Log($"CREATING NEW SAVE FILE in slot {slotNo}");
        man.ActivateSaveSlot_empty(slotNo, saveData);

        // Serialize manifest and save to file
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);

        // Grab a reference to the current slot for convenience
        activeSlot = man.GetActiveSlot();

        // Enter level 1, create save file, and populate with level start state
        SceneManager.LoadScene(activeSlot.Get_LevelData());
        Save(activeSlot.Get_FilePath());
    }

    // Saves the current scene state to file
    void Save(string path)
    {       
        d.Log("Save() CALLED");

        // TODO: FINISH
        // Disable interactions while actively saving/loading (currently stub)
        isLoadingOrSaving = true;
        
        // Capture current state of all registered scene objects
        foreach (ComponentID cID in cIDs.Values)
            { saveData.objectStates[cID.id].Capture_ObjectState(cID); }

        // Serialize and write to most recent file
        string saveData_serial = JsonUtility.ToJson(saveData, prettyPrint:true);
        paths.EnsureSaveFolderExists();
        WriteJsonToFile(path, saveData_serial);
        
        // TODO: FINISH
        // Re-enable interactions
        isLoadingOrSaving = false;
    }

    // LOAD

    public void QuickLoad() 
    {
        d.Log("QuickLoad() CALLED");
        Load(activeSlot.Get_FilePath());
    }

    string pendingLoadPath = null;

    public void LoadFromSlot(int slotNo)
    {
        d.Log($"LOADING SAVE FILE from slot {slotNo}");
        man.ActivateSaveSlot_occupied(slotNo);

        // Grab reference to current save slot
        activeSlot = man.GetActiveSlot();
        pendingLoadPath = activeSlot.Get_FilePath();

        // Enter the level; wait for scene to finish loading before loading save
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.LoadScene(activeSlot.Get_LevelData());
    }

    // Listener waits for scene to finish loading
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        Load(pendingLoadPath);
        pendingLoadPath = null;
    }

    struct LoadPlan
    {
        public HashSet<Guid> deleteIDs;
        public HashSet<Guid> updateIDs;
        public HashSet<Guid> spawnIDs;
    }

    void Load(string path)
    {
        d.Log("Load() CALLED");
        isLoadingOrSaving = true;

        // Re-populate saveData from the save file
        string saveData_serial = ReadJsonFromFile(path);
        if (saveData_serial == "") { d.Error("LOAD FAILED"); return; }
        JsonUtility.FromJsonOverwrite(saveData_serial, saveData);

        // Sort by delete/update/spawn
        LoadPlan plan = BuildLoadPlan();
        
        // Perform and count deletes/updates/spawns
        int dCt = Load_delete(plan.deleteIDs);
        int uCt = Load_update(plan.updateIDs);
        int sCt = Load_spawn(plan.spawnIDs);

        // Rebuild type indices from the restored save data
        types.RestoreTypeCounters(saveData);
        isLoadingOrSaving = false;
    }

    // LOAD HELPERS

    // Place IDs into delete/update/spawn buckets
    LoadPlan BuildLoadPlan()
    {
        var liveIDs = cIDs.Keys.ToHashSet();
        var savedIDs = saveData.objectStates.Keys.ToHashSet();

        LoadPlan p = new LoadPlan
        {
            deleteIDs = liveIDs.Except(savedIDs).ToHashSet(),
            updateIDs = liveIDs.Intersect(savedIDs).ToHashSet(),
            spawnIDs = savedIDs.Except(liveIDs).ToHashSet()
        };

        int expD = p.deleteIDs.Count;
        int expU = p.updateIDs.Count;
        int expS = p.spawnIDs.Count;
        d.Log($"EXPECTED: Delete {expD} ; Update {expU} ; Spawn {expS}");

        return p;
    }

    // Delete objects that are in the current scene but not in the save file
    int Load_delete(HashSet<Guid> dIDs)
    {
        int dCt = 0;

        foreach (var id in dIDs)
        {
            d.Log($"DELETING component {id}");
            cIDs[id].Delete();
            dCt++;
        }

        return dCt; 
    }

    // Update objects that are present in both the current scene and save file
    int Load_update(HashSet<Guid> uIDs)
    {
        int uCt = 0;

        foreach (var id in uIDs)
        {
            d.Log($"UPDATING component {id}");
            saveData.objectStates[id].Apply_ObjectState(cIDs[id]);
            uCt++;
        }

        return uCt;
    }

    // Spawn objects present in the save file and not in the current scene
    int Load_spawn(HashSet<Guid> sIDs)
    {
        int sCt = 0;

        foreach (var id in sIDs)
        {
            d.Log($"SPAWNING component {id}");
            if (!SpawnAndRegisterObjectFromState(id, saveData.objectStates[id]))
                continue;
            sCt++;
        }

        return sCt;
    }

    bool SpawnAndRegisterObjectFromState(Guid id, ObjectState state)
    {
        // Grab the correct prefab. Error if not found
        GameObject prefab = ResolvePrefab(state.type);
        if (prefab == null) return false;

        // Instantiate a new GameObject from the prefab, with the saved object's
        // transform
        GameObject go = UnityEngine.Object.Instantiate(
            prefab, state.position, state.rotation
        );

        // If no ComponentID found on the object, something has gone wrong. 
        // Report error and despawn the object.
        ComponentID cID = go.GetComponent<ComponentID>();
        if (cID == null)
        {
            string errMsg = $"Prefab {prefab.name} missing ComponentID";
            d.Error($"SPAWN FAILED--{errMsg}");
            UnityEngine.Object.Destroy(go);
            return false;
        }

        // Restore and register ComponentID identification data from saved state
        cID.id = id;
        cID.label = state.label;
        cID.index = state.index;
        state.Apply_Fields(cID);
        Register(cID);

        return true;
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

    // GENERAL HELPERS

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
}

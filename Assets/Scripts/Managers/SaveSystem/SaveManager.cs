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
    [NonSerialized] SaveDebug d;
    SaveDebug D()
    {
        if (d == null)
            d = new SaveDebug("<color=#29B6F6>[SaveManager] </color>");
        return d;
    }
    
    public static SaveManager Instance { get; } = new SaveManager();
    public SaveManifest man;
    public readonly ComponentTypes types;
    public readonly SavePaths paths;
    public bool isLoadingOrSaving = false;
    bool isInitialized = false;
    
    SaveSlot activeSlot;
    public SaveData saveData;
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
        if (isInitialized) return;
        // Retrieve save manifest JSON from file
        paths.EnsureManifestFileExists();
        string man_serial = ReadJsonFromFile(paths.manFilePath);
        if (man_serial == "")
        {
            D().Error("MANIFEST LOAD FAILED. Terminating program.");
            GameManager.ExitGame();
        }

        // Deserialize to the man object
        JsonUtility.FromJsonOverwrite(man_serial, man);

        // Subscribe to scene change listener
        SceneManager.activeSceneChanged += OnSceneChanged;
        
        // Mark as initialized
        isInitialized = true;
    }

    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        activeSlot.Set_LevelData(newScene.name);
        Reset_sameID();
    }

    // Clear the current SaveManager instance. Re-roll saveID
    public void Reset_newID()
    {
        saveData = new SaveData();
        ClearRuntimeState();
    }

    // Clear the current SaveManager instance. Retain saveID
    public void Reset_sameID()
    {
        saveData.Reset();
        ClearRuntimeState();
    }

    void ClearRuntimeState()
    {
        types.ResetTypeCounters();
        UndoManager.Instance.Reset();
        cIDs.Clear();
    }

    // Refresh the authoritative saveData
    public void CaptureLiveState()
    {
        foreach (var cID in cIDs.Values)
        {
            if (!saveData.objectStates.ContainsKey(cID.id))
                saveData.objectStates.Add(cID.id, new ObjectState(cID));
            else
                saveData.objectStates[cID.id].Capture_ObjectState(cID);
        }
    }

    // Called by ComponentID to register spawned components with save manager
    public void Register(ComponentID cID)
    {
        D().Log($"REGISTERING component {cID.id}");
        
        // Registration failure
        string errMsg = $"FAILED TO REGISTER {cID.id}--";
        if (cID.id == Guid.Empty) 
            { D().Error($"{errMsg}ID generation failed"); return; }
        if (cIDs.ContainsKey(cID.id)) 
            { D().Error($"{errMsg}Duplicate ID"); return; }

        // Register component
        cIDs.Add(cID.id, cID);
        if (!saveData.objectStates.ContainsKey(cID.id))
            saveData.objectStates.Add(cID.id, new ObjectState(cID));
        cID.MarkRegistered();
        
        D().Success($"Component {cID.id} REGISTERED");
    }

    // TODO: Make this accept only the Guid, since we have the cIDs dictionary
    // now. It'll give better error readout.
    public void Unregister(ComponentID cID)
    {
        // ComponentID not found
        if (cID == null) 
            { D().Error($"UNREGISTER FAILED--NOT FOUND"); return; }
        
        D().Log($"UNREGISTERING component {cID.id}");

        // Unregister component
        cIDs.Remove(cID.id);
        saveData.objectStates.Remove(cID.id);

        D().Success($"Component {cID.id} UNREGISTERED");
    }
    
    // SAVE
    string pendingAction = null;

    // Listener waits for scene to finish loading, then performs save/load
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        string path = activeSlot.Get_FilePath();
        if (pendingAction == "save") Save(path);
        else if (pendingAction == "load") LoadFrom_file(path);
        else D().Error($"Invalid pendingAction");

        pendingAction = null;
    }

    public void QuickSave()
    { 
        D().Log("QuickSave() CALLED");
        
        // Grab the timestamp
        activeSlot.Capture_WhenLastUsed();

        // Re-serialize the manifest
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);

        // Then save
        Save(activeSlot.Get_FilePath());
    }

    // Creates a new save file in the specified save slot
    public void SaveToSlot(int slotNo)
    {
        D().Log($"CREATING NEW SAVE FILE in slot {slotNo}");

        // Start brand new save file
        saveData = new SaveData();

        // Initialize slot and set it active, then grab a reference to it
        man.ActivateSaveSlot_empty(slotNo, saveData);
        activeSlot = man.GetActiveSlot();

        // Serialize manifest and save to file
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);
        
        // Set up listener for scene to finish loading.
        pendingAction = "save";
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Enter level 1. Scene-change listener will update the active level and
        // ensure an empty state with the new SaveID. Then, scene-loaded
        // listener will load from the save file
        SceneManager.LoadScene("Lesson 1");
    }

    // Serialize and write to file
    void Save(string path)
    {       
        D().Log("Save() CALLED");

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
        D().Log("QuickLoad() CALLED");
        UndoManager.Instance.Reset();
        LoadFrom_file(activeSlot.Get_FilePath());
    }

    public void LoadFromSlot(int slotNo)
    {
        D().Log($"LOADING SAVE FILE from slot {slotNo}");

        // Activate selected save slot and grab a reference to it
        man.ActivateSaveSlot_occupied(slotNo);
        activeSlot = man.GetActiveSlot();

        // Restore the SaveID
        saveData.saveID = activeSlot.Get_FileName();
        
        // Serialize manifest and save to file
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);

        // Set up listener for scene to finish loading.
        pendingAction = "load";
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Begin loading the scene. Scene-change listener will update the active
        // level and ensure empty state, then Scene-loaded listener will load
        // from the save file
        SceneManager.LoadScene(activeSlot.Get_LevelData());
    }

    struct LoadPlan
    {
        public HashSet<Guid> deleteIDs;
        public HashSet<Guid> updateIDs;
        public HashSet<Guid> spawnIDs;
    }

    // Read from file, deserialize, and apply state
    void LoadFrom_file(string path)
    {
        D().Log("LoadFrom_file() CALLED");
        isLoadingOrSaving = true;

        // Re-populate saveData from the save file
        string saveData_serial = ReadJsonFromFile(path);
        if (saveData_serial == "") { D().Error("LOAD FAILED"); return; }
        JsonUtility.FromJsonOverwrite(saveData_serial, saveData);

        // Restore objects and type counters
        Load_ApplyState();

        isLoadingOrSaving = false;
    }

    // Just for semantic consistency. Applies state from a SaveData object
    public void LoadFrom_object(SaveData sd)
    {
        D().Log("LoadFrom_object() CALLED");
        isLoadingOrSaving = true;

        // Copy argument into authoritative saveData
        saveData = new SaveData(sd);
        Load_ApplyState();

        isLoadingOrSaving = false;
    }

    // LOAD HELPERS
    void Load_ApplyState()
    {
        // Sort by delete/update/spawn
        LoadPlan plan = BuildLoadPlan();
        
        // Perform and count deletes/updates/spawns
        int dCt = Load_delete(plan.deleteIDs);
        int uCt = Load_update(plan.updateIDs);
        int sCt = Load_spawn(plan.spawnIDs);
        D().Log($"LOADED: Deleted {dCt} ; Updated {uCt} ; Spawned {sCt}");

        // Rebuild type indices from the restored save data
        types.RestoreTypeCounters(saveData);
    }
    
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
        D().Log($"EXPECTED: Delete {expD} ; Update {expU} ; Spawn {expS}");

        return p;
    }

    // Delete objects that are in the current scene but not in the save file
    int Load_delete(HashSet<Guid> dIDs)
    {
        int dCt = 0;

        foreach (var id in dIDs)
        {
            D().Log($"DELETING component {id}");
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
            D().Log($"UPDATING component {id}");
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
            D().Log($"SPAWNING component {id}");
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
            D().Error($"SPAWN FAILED--{errMsg}");
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
            D().Error($"No prefab found at {pfPath}");
        }

        return prefab;
    }

    // GENERAL HELPERS

    void WriteJsonToFile(string path, string jsonData)
    {
        try
        {
            File.WriteAllText(path, jsonData);
            D().Success($"WROTE to {path}");
        }
        catch (Exception e)
            { D().Error($"WRITE FAILED--{e.Message}"); }
    }

    string ReadJsonFromFile(string path)
    {
        string jsonData = "";
        try
        {
            jsonData = File.ReadAllText(path);
            D().Success($"READ from {path}");
        }
        catch (Exception e)
            { D().Error($"READ FAILED--{e.Message}"); }
        
        return jsonData;
    }
}

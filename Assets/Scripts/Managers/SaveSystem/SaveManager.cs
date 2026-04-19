using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System;
using System.IO;
using System.Linq;
using Unity.XR.Management.AndroidManifest.Editor;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
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
    Action<string> PendingAction = null;
    
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
        // UndoManager.Instance.Reset();
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
        D().Log($"REGISTERING component {cID.id} ({cID.label})");
        
        // Registration failure
        string errMsg = $"FAILED TO REGISTER {cID.id} ({cID.label})--";
        if (cID.id == Guid.Empty) 
            { D().Error($"{errMsg}ID generation failed"); return; }
        if (cIDs.ContainsKey(cID.id)) 
            { D().Error($"{errMsg}Duplicate ID"); return; }

        // Register component
        cIDs.Add(cID.id, cID);
        if (!saveData.objectStates.ContainsKey(cID.id))
            saveData.objectStates.Add(cID.id, new ObjectState(cID));
        cID.MarkRegistered();
        
        D().Success($"Component {cID.id} ({cID.label}) REGISTERED");
    }

    // TODO: Make this accept only the Guid, since we have the cIDs dictionary
    // now. It'll give better error readout.
    public void Unregister(ComponentID cID)
    {
        // ComponentID not found
        if (cID == null) 
            { D().Error($"UNREGISTER FAILED--NOT FOUND"); return; }
        
        D().Log($"UNREGISTERING component {cID.id} ({cID.label})");

        // Unregister component
        cIDs.Remove(cID.id);
        saveData.objectStates.Remove(cID.id);

        D().Success($"Component {cID.id} ({cID.label}) UNREGISTERED");
    }
    
    // SAVE

    public void QuickSave()
    { 
        isLoadingOrSaving = true;
        // Do nothing if not inside a level
        if (activeSlot == null)
            { D().Warn("QuickSave() FAILED--No active save slot"); return; }
        
        D().Log($"QuickSaving in slot {man.GetLastSlotNo()}");

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
        isLoadingOrSaving = true;
        // Do nothing if already inside a level
        if (activeSlot != null)
        {
            D().Error($"SaveToSlot({slotNo}) FAILED--slot already active");
            return;
        }
        
        // Do nothing if slot already contains a fave file
        if (!man.SlotIsEmpty(slotNo))
        {
            D().Error($"SaveToSlot({slotNo}) FAILED--slot not empty");
            return;
        }

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
        PendingAction = Save;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Enter the slot's level. Scene-change listener will update the active
        // level and ensure an empty state with the new SaveID. Then,
        // scene-loaded listener will load from the save file
        SceneManager.LoadScene($"Lesson {slotNo}");
    }

    // Serialize and write to file
    void Save(string path)
    {       
        D().Log("Save() CALLED");
        
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
        isLoadingOrSaving = true;
        // Do nothing if not inside a level
        if (activeSlot == null)
            { D().Error("QuickLoad() FAILED--No active save slot"); return; }
        
        D().Log($"QuickLoading from slot {man.GetLastSlotNo()}");

        // UndoManager.Instance.Reset();

        // Set up listener for scene to finish loading.
        PendingAction = LoadFrom_file;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Begin loading the scene. Scene-change listener will update the active
        // level and ensure empty state, then Scene-loaded listener will load
        // from the save file
        SceneManager.LoadScene(activeSlot.Get_LevelData());
    }

    // Load from the most reccently used save slot
    public void Continue()
    {
        isLoadingOrSaving = true;
        // Do nothing if already inside a level
        if (activeSlot != null)
        {
            D().Error("Continue() FAILED--a save slot is already active");
            return;
        }

        int slotNo = man.GetLastSlotNo();
        if (slotNo == 0)
        {
            // Enter level 1, or just announce and go to level select?
            // Or some secret third option? Or just nothing?
            D().Warn("Continue() FAILED--No continue file");
        }
        LoadFromSlot(slotNo);
    }

    public void LoadFromSlot(int slotNo)
    {
        isLoadingOrSaving = true;
        // Do nothing if the selected slot is empty
        if (man.SlotIsEmpty(slotNo))
        {
            D().Error($"LoadFromSlot({slotNo}) FAILED--slot empty");
            return;
        }

        // Do nothing if already inside a level
        if (activeSlot != null)
            // Seeing this from Continue() means something has gone wrong
        {
            D().Error($"LoadFromSlot({slotNo}) FAILED--slot already active");
            return;
        }

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
        PendingAction = LoadFrom_file;
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Begin loading the scene. Scene-change listener will update the active
        // level and ensure empty state, then Scene-loaded listener will load
        // from the save file
        SceneManager.LoadScene(activeSlot.Get_LevelData());
    }

    struct LoadPlan
    {
        public List<WireEnd> attachedWireEnds;
        public HashSet<Guid> deleteIDs;
        public HashSet<Guid> updateIDs;
        public HashSet<Guid> spawnIDs;
    }

    // Read from file, deserialize, and apply state
    void LoadFrom_file(string path)
    {
        D().Log($"Loading from {path}");
        isLoadingOrSaving = true;

        // Re-populate saveData from the save file
        string saveData_serial = ReadJsonFromFile(path);
        if (saveData_serial == "") { D().Error("LOAD FAILED"); return; }
        JsonUtility.FromJsonOverwrite(saveData_serial, saveData);

        // Restore objects and type counters
        CoroutineRunner.RunCoroutine(Load_ApplyState());
    }

    // Just for semantic consistency. Applies state from a SaveData object
    public void LoadFrom_object(SaveData sd)
    {
        D().Log("Loading from object");
        isLoadingOrSaving = true;

        // Copy sd into authoritative saveData
        saveData = new SaveData(sd);
        CoroutineRunner.RunCoroutine(Load_ApplyState());
    }

    // LOAD HELPERS
    IEnumerator Load_ApplyState()
    {        
        try {
            SetAllSocketsEnabled(false);
            
            // Sort by delete/update/spawn
            LoadPlan plan = BuildLoadPlan();

            Load_DetatchWires(plan.attachedWireEnds);

            // Wait one frame for detatchment to stabilize
            yield return null;
            
            // Perform and count deletes/updates/spawns
            int dCt = Load_delete(plan.deleteIDs);
            int uCt = Load_update(plan.updateIDs);
            int sCt = Load_spawn(plan.spawnIDs);
            Load_fields();
            Physics.SyncTransforms();

            SetAllSocketsEnabled(true);

            D().Log($"LOADED: Deleted {dCt} ; Updated {uCt} ; Spawned {sCt}");

            // Rebuild type indices from the restored save data
            types.RestoreTypeCounters(saveData);
        }
        finally
        {
        isLoadingOrSaving = false;
        }
    }

    void SetAllSocketsEnabled(bool enabled)
    {
        XRSocketInteractor[] sockets =
            UnityEngine.Object.FindObjectsByType<XRSocketInteractor>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (XRSocketInteractor socket in sockets)
        {
            if (socket == null) continue;
            socket.enabled = enabled;
        }
    }

    // Place IDs into delete/update/spawn buckets
    LoadPlan BuildLoadPlan()
    {
        var liveIDs = cIDs.Keys.ToHashSet();
        var savedIDs = saveData.objectStates.Keys.ToHashSet();
        
        LoadPlan p = new LoadPlan{ attachedWireEnds = new List<WireEnd>() };

        foreach (var id in cIDs.Keys)
        {
            Wire w = cIDs[id].gameObject.GetComponent<Wire>();
            if (!w) continue;

            if (w.compStart != null) p.attachedWireEnds.Add(w.startpoint);
            if (w.compEnd != null) p.attachedWireEnds.Add(w.endpoint);
        }

        p.deleteIDs = liveIDs.Except(savedIDs).ToHashSet();
        p.updateIDs = liveIDs.Intersect(savedIDs).ToHashSet();
        p.spawnIDs = savedIDs.Except(liveIDs).ToHashSet();

        int expD = p.deleteIDs.Count;
        int expU = p.updateIDs.Count;
        int expS = p.spawnIDs.Count;
        D().Log($"EXPECTED: Delete {expD} ; Update {expU} ; Spawn {expS}");

        return p;
    }

    void Load_DetatchWires(List<WireEnd> ends)
    {
        foreach (var e in ends) DetatchWire(e);
    }

    // Force anything currently holding the wire end to let go
    void DetatchWire(WireEnd e)
    {
        if (e == null) return;
        XRGrabInteractable grab = e.GetGrabber();
        if (grab == null) return;

        // Just an extra guard, but it should be attached, because that's how we
        // built the input list
        if (!grab.isSelected) return;

        // So we can have the manager handle detatchment
        XRInteractionManager manager = grab.interactionManager;
        if (manager == null) return;

        // Detatch everything holding on to the wire end
        for (int i = grab.interactorsSelecting.Count - 1; i >= 0; i--)
        {
            IXRSelectInteractor interactor = grab.interactorsSelecting[i];
            if (interactor == null) continue;
            
            // This line does the detatchment
            manager.SelectExit(interactor, grab);
        }
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
            saveData.objectStates[id].Apply_Transform(cIDs[id]);
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

    void Load_fields()
    {
        foreach (var cID in cIDs.Values)
        {
            D().Log($"APPLYING FIELD(S) to component {cID.id} ({cID.label})");
            saveData.objectStates[cID.id].Apply_Fields(cID);
        }
    }

    // Other menu commands

    public void EnterLesson(int lessonNo)
    {
        if (man.SlotIsEmpty(lessonNo)) SaveToSlot(lessonNo);
        else LoadFromSlot(lessonNo);
    }

    public void ResetScene()
    {
        // Clear the scene, but don't save to allow reverting to the last
        // quicksave after reset
        SceneManager.LoadScene(activeSlot.Get_LevelData());
        Reset_sameID();        
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

    // LISTENERS

    // Waits for scene to finish loading, then performs save/load
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        // Call save/load as specified without branching logic
        string path = activeSlot.Get_FilePath();
        PendingAction(path);
        PendingAction = null;
    }

    // On calling LoadScene(), set the current scene's level to the destination
    // scene and clear the authoritative saveData
    void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        if (activeSlot == null) return;
        
        activeSlot.Set_LevelData(newScene.name);

        // Serialize and save new level data
        string man_serial = JsonUtility.ToJson(man, prettyPrint: true);
        WriteJsonToFile(paths.manFilePath, man_serial);

        Reset_sameID();
    }
}

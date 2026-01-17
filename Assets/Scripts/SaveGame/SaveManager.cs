using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System;
[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]

[DefaultExecutionOrder(-100)]
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private SaveData currentSave;

    Dictionary<Type, int> typeCounters;
    public Dictionary<Guid, SaveableObject> saveablesKeyed;

    public enum Type
    {
        WIRE,
        POWER_SOURCE,
        RESISTOR,
        LED,      
        GROUND,    
        OTHER
    }

    void InitTypeCounters()
    {
        typeCounters = new Dictionary<Type, int>
        {
            { Type.WIRE, 0 },
            { Type.POWER_SOURCE, 0 },
            { Type.RESISTOR, 0 },
            { Type.LED, 0 },      
            { Type.GROUND, 0 },   
            { Type.OTHER, 0 }
        };
    }

    // Generate a unique identifier for components spawned in at runtime, using 
    // the individual type counters. Call this in SaveableObject
    public string GenerateLabel(Type type)
    {
        int index = typeCounters[type];
        typeCounters[type]++;

        return $"{type}_{index}";
    }

    // Add new SaveableObject to saveablesKeyed
    public void RegisterSaveable(SaveableObject obj)
    {
        // saveablesKeyed not yet initialized
        if (saveablesKeyed == null)
        {
            saveablesKeyed = new Dictionary<Guid, SaveableObject>();
        }

        // obj has no ID--don't save
        if (obj.id == Guid.Empty)
        {
            Debug.LogWarning(
                $"SaveableObject {obj.gameObject.name} has empty id"
            );
            Debug.LogWarning($"-- not registered");
            return;
        }

        // obj with the current ID already exists
        if (saveablesKeyed.ContainsKey(obj.id))
        {
            Debug.LogWarning(
                $"SaveableObject with id {obj.id} already exists"
            );
            Debug.LogWarning($"-- not registered");
            return;
        }

        // Otherwise, go ahead and add the SaveableObject
        saveablesKeyed.Add(obj.id, obj);
    }
    void CatalogStartingObjects()
    {
        // Find all the SaveableObjects in the scene
        SaveableObject[] saveablesRaw = FindObjectsByType<SaveableObject>(
            FindObjectsSortMode.None
        );
        
        foreach (var s in saveablesRaw)
        {
            RegisterSaveable(s);
        }
    }

    // Initialize in play mode
    void Awake()
    {
        Instance = this;
        InitTypeCounters();
        CatalogStartingObjects();
    }

    // Build SaveData
    public void Save()
    {
        currentSave = new SaveData();
        currentSave.objStates = new List<ObjectState>();

        foreach (var obj in saveablesKeyed)
        {
            SaveableObject saveable = obj.Value;
            ObjectState state = saveable.StoreObjectState();
            currentSave.objStates.Add(state);
        }

        Debug.Log("Saved" + currentSave.objStates.Count + " objects.");
    }

    // Apply SaveData
    public void Load()
    {
        if (currentSave == null)
        {
            Debug.LogWarning("No save data to load");
            return;
        }

        // Build the set of saved IDs
        HashSet<Guid> savedIds = new HashSet<Guid>();
        foreach (var state in currentSave.objStates)
        {
            if (state.id == Guid.Empty) savedIds.Add(state.id);
        }

        // Iterate over a copy of saveablesKeyed, delete objects that have been 
        // spawned since last save
        foreach (var pair in saveablesKeyed.ToList())
        {
            Guid id = pair.Key;
            SaveableObject obj = pair.Value;

            if (!savedIds.Contains(id))
            {
                // Delete the object
                if (obj != null) Destroy(obj.gameObject);
                saveablesKeyed.Remove(id);
            }
        }

        // Apply SaveData
        foreach (var state in currentSave.objStates)
        {
            if (saveablesKeyed.TryGetValue(state.id, out SaveableObject saveable))
            {
                saveable.ApplyObjectState(state);
            }
            else
            {
                Debug.LogWarning($"No SaveableObject with id {state.id}");
            }
        }

        Debug.Log("Loaded " + currentSave.objStates.Count + " objects.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
    }

    // Expose private items for unit tests
        
    // Initialize counters for testing
    internal void TestInit_TypeCounters()
    {
        InitTypeCounters();
    }

    // Initialize empty saveablesKeyed dictionary for testing
    internal void TestInit_SaveablesDict()
    {
        saveablesKeyed = new Dictionary<Guid, SaveableObject>();
    }

}

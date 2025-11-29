using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }
    private SaveData currentSave;

    Dictionary<Type, int> typeCounters;
    public Dictionary<string, SaveableObject> saveablesKeyed;

    public enum Type
    {
        WIRE,
        POWER_SOURCE,
        RESISTOR,
        OTHER
    }

    void InitTypeCounters()
    {
        typeCounters = new Dictionary<Type, int>
        {
            { Type.WIRE, 0 },
            { Type.POWER_SOURCE, 0 },
            { Type.RESISTOR, 0 },
            { Type.OTHER, 0 }
        };

    }
    
    // Generate a unique identifier for components spawned in at runtime, using 
    // the individual type counters. Call this in SaveableObject
    public string GenerateID(Type type)
    {
        int index = typeCounters[type];
        typeCounters[type] += 1;

        return $"{type}_{index}";
    }

    // Add new SaveableObject to saveablesKeyed
    public void RegisterSaveable(SaveableObject obj)
    {
        // saveablesKeyed not yet initialized
        if (saveablesKeyed == null)
        {
            saveablesKeyed = new Dictionary<string, SaveableObject>();
        }

        // obj has no ID--don't save
        if (string.IsNullOrEmpty(obj.id))
        {
            Debug.LogWarning(
                $"SaveableObject {obj.gameObject.name} has empty id--not saved"
            );
            return;
        }

        // obj with the current ID already exists
        if (saveablesKeyed.ContainsKey(obj.id))
        {
            Debug.LogWarning(
                $"SaveableObject with id {obj.id} already exists--not saved"
            );
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
        currentSave.objects = new List<ObjectState>();

        foreach (var obj in saveablesKeyed)
        {
            SaveableObject saveable = obj.Value;
            ObjectState state = saveable.StoreObjectState();
            currentSave.objects.Add(state);
        }

        Debug.Log("Saved" + currentSave.objects.Count + " objects.");
    }

    // Apply SaveData
    public void Load()
    {
        if (currentSave == null)
        {
            Debug.LogWarning("No save data to load");
            return;
        }

        foreach (var state in currentSave.objects)
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

        Debug.Log("Loaded " + currentSave.objects.Count + " objects.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5)) Save();
        if (Input.GetKeyDown(KeyCode.F9)) Load();
    }
}

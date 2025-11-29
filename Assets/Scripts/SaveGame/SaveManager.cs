using UnityEngine;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    private SaveData currentSave;
    Dictionary<string, SaveableObject> saveablesKeyed;

    void Awake()
    {
        // Find all the SaveableObjects in the scene
        SaveableObject[] saveablesRaw = FindObjectsByType<SaveableObject>(
            FindObjectsSortMode.None
        );

        saveablesKeyed = new Dictionary<string, SaveableObject>();
        
        foreach (var s in saveablesRaw)
        {
            // Empty ID field
            if (string.IsNullOrEmpty(s.id))
            {
                Debug.LogWarning(
                    $"SaveableObject on {s.gameObject.name} has no id"
                );
                continue;
            }

            // Duplicate ID
            if (saveablesKeyed.ContainsKey(s.id))
            {
                Debug.LogWarning(
                    $"Duplicate SaveableObject id {s.id} on {s.gameObject.name}"
                );
                continue;
            }

            // Otherwise, save the object
            saveablesKeyed.Add(s.id, s);
        }
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

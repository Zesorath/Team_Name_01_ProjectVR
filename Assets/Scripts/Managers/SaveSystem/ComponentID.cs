using System;
using NUnit.Framework;
using UnityEngine;

public class ComponentID : MonoBehaviour
{
    public Guid id;
    public string label = "";
    public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
    public int index = -1;

    // Runs on object spawn. Generates a unique ID if one doesn't already exist,
    // and calls GenerateLabelSuggestion() to populate the label field
    public void Awake()
    {
        // id will already exist on load
        if (id == Guid.Empty) id = Guid.NewGuid();
        
        // Error states--abort initialization
        string errMsg = "[ComponentID]: Initialization failure--";
        if (SaveManager.Instance == null)
            { Debug.Log($"{errMsg}NO SAVE MANAGER INSTANCE"); return; }

        GenerateLabelSuggestion();
        if (label == "") { Debug.Log($"{errMsg}NO LABEL GENERATED"); return; }

        // Initialization succeeded--register component
        Debug.Log($"[ComponentID]: INITIALIZED--ID = {id}, label = {label}");
        SaveManager.Instance.Register(this);
    }

    public void OnDestroy() { Debug.Log($"[ComponentID]: DESTROYED {id}"); }

    // Generates a label from the component type and the next available index 
    // of that type. Type is set by the component prefab. Populates index field
    // from the type's current count.
    public void GenerateLabelSuggestion()
    {
        string errMsg = "[ComponentID]: Label generation failure--";
        
        // Label generation failure
        if (type == ComponentTypes.Types.DEFAULT)
            { Debug.Log($"{errMsg}COMPONENT TYPE NOT SPECIFIED"); return; }
        
        index = SaveManager.Instance.types.GetNextTypeIndex(type);
        if (index == 0) { Debug.Log($"{errMsg}INDEX NOT FOUND"); return; }

        // Found both parts--generate label
        label = $"{type}_{index}";
        Debug.Log($"[ComponentID]: GENERATED LABEL {label} for component {id}");
    }

    // User can also specify their own label for a component
    public void ChangeLabel(string newLabel) { label = newLabel; }

    // Revert back to the automatically suggested label
    public void RevertLabel() { label = $"{type}_{index}"; }
}

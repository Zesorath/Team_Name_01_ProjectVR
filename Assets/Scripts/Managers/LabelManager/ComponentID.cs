using System;
using NUnit.Framework;
using UnityEngine;

public class ComponentID : MonoBehaviour
{
    public Guid id;
    public string label = "";
    public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
    public int index = -1;

    /// <summary>
    /// Runs on object spawn. Generates a unique ID and populates label field by
    /// calling GenerateLabelSuggestion()
    /// </summary>
    public void Awake()
    {
        if (SaveManager.Instance == null)
        { Debug.Log(StatusCode.ERROR_MISSING_SAVE_MANAGER_INSTANCE); return; }

        if (id == Guid.Empty) id = Guid.NewGuid();
        
        label = GenerateLabelSuggestion();
        if (label == "") 
        { Debug.Log(StatusCode.ERROR_NO_LABEL_GENERATED); return; }

        Debug.Log($"[ComponentID]: Generated ID = {id}, label = {label}");
        Debug.Log($"Register() returned {SaveManager.Instance.Register(this)}");
    }

    /// <summary>
    /// Generates a label from the component type and the next available index 
    /// of that type. Type is set by the component prefab. Populates index field
    /// from the type's current count.
    /// </summary>
    public string GenerateLabelSuggestion()
    {
        Debug.Log($"[ComponentID]: Generating label for component {id}");
        if (type == ComponentTypes.Types.DEFAULT)
            Debug.Log(StatusCode.ERROR_NO_COMPONENT_TYPE);
        
        index = SaveManager.Instance.types.GetNextTypeIndex(type);
        return $"{type}_{index}";
    }

    /// <summary>
    /// User can also specify their own label for a component
    /// </summary>
    public void ChangeLabel(string newLabel)
    {
        label = newLabel;
    }

    /// <summary>
    /// Revert back to the automatically suggested label
    /// </summary>
    public void RevertLabel()
    {
        label = $"{type}_{index}";
    }
}

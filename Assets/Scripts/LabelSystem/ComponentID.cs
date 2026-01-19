using System;
using NUnit.Framework;
using UnityEngine;

public class ComponentID : MonoBehaviour
{
    public Guid id;
    public string label = "";
    public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
    public int index = -1;

    public void Awake()
    {
        if (SaveManager.Instance == null)
        { Debug.Log(StatusCode.ERROR_MISSING_SAVE_MANAGER_INSTANCE); return; }

        id = Guid.NewGuid();
        
        label = GenerateLabelSuggestion();
        if (label == "") 
        { Debug.Log(StatusCode.ERROR_NO_LABEL_GENERATED); return; }

        Debug.Log($"[ComponentID]: Generated ID = {id}, label = {label}");
        Debug.Log($"Register() returned {SaveManager.Instance.Register(this)}");
    }

    public string GenerateLabelSuggestion()
    {
        Debug.Log($"[ComponentID]: Generating label for component {id}");
        if (type == ComponentTypes.Types.DEFAULT)
            Debug.Log(StatusCode.ERROR_NO_COMPONENT_TYPE);
        
        index = SaveManager.Instance.types.GetNextTypeIndex(type);
        return $"{type}_{index}";
    }

    public void ChangeLabel(string newLabel)
    {
        label = newLabel;
    }

    public void RevertLabel()
    {
        label = $"{type}_{index}";
    }
}

using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ComponentID : MonoBehaviour
{
    // ID FIELDS AND FUNCTIONS
    public Guid id;
    public string label = "";
    public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
    public int index = -1;

    // Acts as a constructor. Generates a unique ID if one doesn't already 
    // exist, and calls GenerateLabelSuggestion() to populate the label field
    void Init()
    {
        // id will already exist if building from a save file
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
        registered = true;
    }

    // Generates a label from the component type and the next available index 
    // of that type. Type is set by the component prefab. Populates index field
    // from the type's current count.
    void GenerateLabelSuggestion()
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


    // LIFETIME FIELDS AND FUNCTIONS

    // Used for registering on first release, to avoid a bug in Load()
    bool registered = false;
    XRGrabInteractable grab;
    // Spawner to avoid
    ItemSpawner os; // Origin spawner
    Transform osTransform;
    float osRadius;

    // Runs on object spawn.
    public void Awake()
    {        
        // Grab a reference to the XR component
        grab = GetComponent<XRGrabInteractable>();
        if (grab == null) 
        {
            string msg = $"{gameObject.name} missing XRGrabInteractable";
            Debug.Log($"[ComponentID]: {msg}");
            return;
        }

        // There will be no ItemSpawner if the object is spawned from save file,
        // so go ahead and register the Component
        os = GetComponentInParent<ItemSpawner>();
        if (os == null) { Init(); return; }

        // Otherwise, cache origin spawner's position, rotation, and size
        osTransform = os.transform;
        osRadius = os.Respawn_Radius;
    }

    // Define grab listener
    void OnReleased(SelectExitEventArgs args)
    {
        // Skip if already registered
        if (registered) return;

        // Register first time object is released outside the spawner radius, 
        // plus a tiny epsilon to prevent jitter
        float d = Vector3.Distance(transform.position, osTransform.position);
        if (d > osRadius + 0.01f) Init();
    }

    // Subscribe to grab listener on Awake()
    void OnEnable()
    {
        if (grab != null) grab.selectExited.AddListener(OnReleased);
    }

    // Un-subscribe to grab listener when the object is destroyed
    void OnDisable()
    {
        if (grab != null) grab.selectExited.RemoveListener(OnReleased);
    }

    // Move id/label generation and registration (basically everything in old Awake()) to OnRelease()


    // Just announce when the ComponentID object is destroyed
    public void OnDestroy() { Debug.Log($"[ComponentID]: DESTROYED {id}"); }
}

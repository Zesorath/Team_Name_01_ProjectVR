using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ComponentID : MonoBehaviour
{
    readonly SaveDebug d = 
        new SaveDebug("<color=#64B5F6>[ComponentID] </color>");
    
    // ID FIELDS AND FUNCTIONS
    public Guid id;
    public string label = "";
    public ComponentTypes.Types type = ComponentTypes.Types.DEFAULT;
    public int index = -1;

    // Acts as a constructor. Generates a unique ID if one doesn't already 
    // exist, and calls GenerateLabelSuggestion() to populate the label field
    // TODO: This needs to not call Register(), maybe
    void Init()
    {
        // id will already exist if building from a save file
        if (id == Guid.Empty) id = Guid.NewGuid();

        // Error states--abort initialization
        string errMsg = "Init() FAILURE--";
        if (SaveManager.Instance == null)
            { d.Error($"{errMsg}No SaveManager instance"); return; }

        GenerateLabelSuggestion();
        if (label == "") { d.Error($"{errMsg}No label generated"); return; }

        // Initialization succeeded--register component
        d.Success($"INITIALIZED--ID = {id}, label = {label}");
        SaveManager.Instance.Register(this);
        registered = true;
    }

    public void Delete()
    {
        SaveManager sm = SaveManager.Instance;

        sm.Unregister(this);
        Destroy(gameObject);
    }

    // Generates a label from the component type and the next available index 
    // of that type. Type is set by the component prefab. Populates index field
    // from the type's current count.
    void GenerateLabelSuggestion()
    {
        string errMsg = "LABEL GENERATION FAILURE--";
        
        // Label generation failure
        if (type == ComponentTypes.Types.DEFAULT)
            { d.Error($"{errMsg}Component type not specified"); return; }
        
        index = SaveManager.Instance.types.GetNextTypeIndex(type);
        if (index == 0) { d.Error($"{errMsg}Index not found"); return; }

        // Found both parts--generate label
        label = $"{type}_{index}";
        d.Success($"GENERATED LABEL {label} for component {id}");
    }

    // Used to flip registered bool for Register() when spawning from save file
    public void MarkRegistered() { registered = true; }

    // User can also specify their own label for a component
    public void ChangeLabel(string newLabel) { label = newLabel; }

    // Revert back to the automatically suggested label
    public void RevertLabel() { label = $"{type}_{index}"; }


    // LIFETIME FIELDS AND FUNCTIONS

    // Used for registering on first release
    bool registered = false;
    public bool isDisplay = false;
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
            { d.Error($"{gameObject.name} missing XRGrabInteractable"); return; }

        // There will be no ItemSpawner if the object is spawned from save file.
        // so go ahead and register the Component
        os = GetComponentInParent<ItemSpawner>();
        if (os == null)
        {            
            // Set id/label/index and Register() will happen manually on Load()
            // NOTE: This might change later on
            SaveManager sm = SaveManager.Instance;
            if (sm != null && sm.isLoadingOrSaving) return;
            
            // The other option is that the components already exist in the
            // scene as part of the level
            if (isDisplay == false) Init(); 
            return;
        }

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
        // plus a tiny epsilon to prevent jitter. Un=parent it and mark as no
        // longer display
        float d = Vector3.Distance(transform.position, osTransform.position);
        if (d > osRadius + 0.01f && isDisplay == true) 
            { transform.SetParent(null); isDisplay = false; Init(); }
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

    // Just announce when the ComponentID object is destroyed
    public void OnDestroy() { d.Log($"DESTROYED {id}"); }
}

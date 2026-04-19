using System;
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

        // If this is a wire, explicitly destroy its ends first. This fixes a
        // bug where connected wire ends weren't destroyed automatically by the
        // parent.
        Wire wire = GetComponent<Wire>();
        if (wire)
        {
            Destroy(wire.startpoint.gameObject);
            Destroy(wire.endpoint.gameObject);
        } 

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
    XRGrabInteractable[] grabs = new XRGrabInteractable[2];
    // Spawner to avoid
    ItemSpawner os; // Origin spawner
    Transform osTransform;
    float osRadius;

    // Runs on object spawn.
    public void Awake()
    {        
        // Grab reference(s) to the XR-grab component(s)
        grabs[0] = GetComponent<XRGrabInteractable>();
        if (grabs[0] == null) 
        {
            string msg = "Is it a wire?";
            d.Warn($"{gameObject.name} missing XRGrabInteractable--{msg}");
            
            Wire wire = GetComponent<Wire>();
            if (!wire) { d.Error("No XRGrabInteractable found"); return; }

            if (!wire.startpoint || !wire.endpoint)
                { d.Error("Wire ends not found"); return; }
            
            // Otherwise, set each end's parent cID and grab their grabbers
            wire.startpoint.parentCID = this;
            grabs[0] = wire.startpoint.GetComponent<XRGrabInteractable>();
            
            wire.endpoint.parentCID = this;
            grabs[1] = wire.endpoint.GetComponent<XRGrabInteractable>();
            
            if (!grabs[0] || !grabs[1])
                { d.Error("Wire grabs not found"); return; }
            else d.Success("Wire grabs found");
        }

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
        // Don't do anything if the save system is busy
        if (SaveManager.Instance.isLoadingOrSaving == true) return;
        
        // Skip if already registered
        if (!registered)
        {
            // Register first time object is released outside the spawner
            // radius, plus a tiny epsilon to prevent jitter. Un-parent it and
            // mark as no longer display
            float d = Vector3.Distance(
                transform.position, osTransform.position
            );
            if (d > osRadius + 0.01f && isDisplay == true) 
                { transform.SetParent(null); isDisplay = false; Init(); }
        }

        // Refresh the authoritative saveData
        SaveManager.Instance.CaptureLiveState();

        // Push snapshot to the undo stack
        // UndoManager.Instance.Do();
    }

    // Subscribe to grab listener on Awake()
    void OnEnable()
    {
        if (grabs[0] != null) grabs[0].selectExited.AddListener(OnReleased);
        if (grabs[1] != null) grabs[1].selectExited.AddListener(OnReleased);
    }

    // Un-subscribe to grab listener when the object is destroyed
    void OnDisable()
    {
        if (grabs[0] != null) grabs[0].selectExited.RemoveListener(OnReleased);
        if (grabs[1] != null) grabs[1].selectExited.RemoveListener(OnReleased);
    }

    // Just announce when the ComponentID object is destroyed
    public void OnDestroy() { d.Log($"DESTROYED {id} ({label})"); }
}

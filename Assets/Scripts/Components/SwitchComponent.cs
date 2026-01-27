using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class SwitchComponent : CircuitComponentBase
{
    [Header("State")]
    [SerializeField] private bool isClosed = false;
    public bool IsClosed => isClosed;

    [Header("Handle Interactable (top cylinder)")]
    [SerializeField] private XRBaseInteractable handleInteractable;

    protected override void Awake()
    {
        base.Awake();

        // Auto-find if not assigned (important for spawned prefabs)
        if (!handleInteractable)
        {
            // Prefer an XRSimpleInteractable (your handle)
            handleInteractable = GetComponentInChildren<XRSimpleInteractable>(true);

            // Fallback: any interactable in children
            if (!handleInteractable)
                handleInteractable = GetComponentInChildren<XRBaseInteractable>(true);
            var grab = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            var handleCol = handleInteractable.GetComponent<Collider>();

            if (grab && handleCol)
            {
                // Ensure grab interactable does NOT use the handle collider
                grab.colliders.Remove(handleCol);
                Debug.Log($"[SWITCH] Removed handle collider from grab colliders: {handleCol.name}");
            }
        }

        Debug.Log($"[SWITCH][Awake] {componentId} handleInteractable={(handleInteractable ? handleInteractable.name : "NULL")}");
    }

    private void OnEnable()
    {
        if (!handleInteractable)
        {
            Debug.LogError($"[SWITCH] {componentId}: handleInteractable is not assigned!");
            return;
        }

        handleInteractable.selectEntered.AddListener(OnSelectEntered);
    }

    private void OnDisable()
    {
        if (handleInteractable != null)
            handleInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[SWITCH][SELECT] {componentId} handle selected by {args.interactorObject?.transform.name}");
        Toggle();
    }

    private void Toggle()
    {
        bool old = isClosed;
        isClosed = !isClosed;
        Debug.Log($"[SWITCH][TOGGLE] {componentId}: {old} -> {isClosed}");
        CircuitManager.Instance?.NotifyConnectionChanged();
    }

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        // intentionally empty
    }
}

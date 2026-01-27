using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using SpiceSharp;

public class SwitchComponent : CircuitComponentBase
{
    [Header("State")]
    [SerializeField] private bool isClosed = false;
    public bool IsClosed => isClosed;

    [Header("Handle Interactable (top cylinder)")]
    [SerializeField] private UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable handleInteractable;

    protected override void Awake()
    {
        base.Awake();

        if (!handleInteractable)
            Debug.LogError($"[SWITCH] {componentId}: handleInteractable is not assigned!");
        else
            Debug.Log($"[SWITCH][Awake] {componentId}: handleInteractable={handleInteractable.name}");
    }

    private void OnEnable()
    {
        if (handleInteractable == null) return;

       // handleInteractable.hoverEntered.AddListener(OnHoverEntered);
        handleInteractable.selectEntered.AddListener(OnSelectEntered);
    }


    private void OnDisable()
    {
        if (handleInteractable == null) return;

      //  handleInteractable.hoverEntered.RemoveListener(OnHoverEntered);
        handleInteractable.selectEntered.RemoveListener(OnSelectEntered);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log($"[SWITCH][SELECT] {componentId} handle selected by {args.interactorObject?.transform.name}");
        Toggle();
    }

    

    private void OnActivated(ActivateEventArgs args)
    {
        Debug.Log($"[SWITCH][ACTIVATE] {componentId} handle activated by {args.interactorObject?.transform.name}");
        Toggle(); // <-- now exists
    }

    private void Toggle()
    {
        bool old = isClosed;
        isClosed = !isClosed;

        Debug.Log($"[SWITCH][TOGGLE] {componentId}: {old} -> {isClosed}");

        CircuitManager.Instance?.NotifyConnectionChanged();
    }

    // Connectivity handled in CircuitManager via unions/skips
    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        // intentionally empty
    }
}

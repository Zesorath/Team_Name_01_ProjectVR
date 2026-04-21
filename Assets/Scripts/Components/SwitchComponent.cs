using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SpiceSharp;
using SpiceSharp.Components;

public class SwitchComponent : CircuitComponentBase
{
    [Header("State")]
    [SerializeField] private bool isClosed = false;
    public bool IsClosed => isClosed;

    public Animator animator;

    [Header("Trigger Input Actions")]
    [Tooltip("Drag in the Left Hand / Activate action from your XRI Input Action Asset")]
    [SerializeField] private InputActionReference leftActivateAction;
    [Tooltip("Drag in the Right Hand / Activate action from your XRI Input Action Asset")]
    [SerializeField] private InputActionReference rightActivateAction;

    [Tooltip("Seconds before the switch can be toggled again (prevents double-fire)")]
    [SerializeField] private float toggleCooldown = 0.3f;
    private float lastToggleTime = -999f;

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private bool isHovered = false;

    protected override void Awake()
    {
        base.Awake();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();

        if (!grabInteractable)
            Debug.LogError($"[SWITCH] {componentId}: No XRGrabInteractable found on root object!");
    }

    private void OnEnable()
    {
        if (!grabInteractable) return;

        // Track when a hand is near the switch (hover = hand in range, not grabbed)
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);

        // Trigger while HOLDING the switch (grip held + trigger pressed)
        grabInteractable.activated.AddListener(OnActivated);

        // Subscribe to trigger input actions so we can toggle WITHOUT grabbing first
        if (leftActivateAction?.action != null)
        {
            leftActivateAction.action.Enable();
            leftActivateAction.action.performed += OnTriggerPerformed;
        }
        if (rightActivateAction?.action != null)
        {
            rightActivateAction.action.Enable();
            rightActivateAction.action.performed += OnTriggerPerformed;
        }
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.activated.RemoveListener(OnActivated);
        }

        if (leftActivateAction?.action != null)
            leftActivateAction.action.performed -= OnTriggerPerformed;
        if (rightActivateAction?.action != null)
            rightActivateAction.action.performed -= OnTriggerPerformed;
    }

    // ── Hover tracking ────────────────────────────────────────────────────────

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovered = true;
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Could still have other hands hovering
        isHovered = grabInteractable.interactorsHovering.Count > 0;
    }

    // ── Trigger without grabbing ──────────────────────────────────────────────

    private void OnTriggerPerformed(InputAction.CallbackContext ctx)
    {
        // Only fire if a hand is near the switch AND it isn't currently grabbed.
        // The 'activated' event below handles the grab-then-trigger case.
        if (isHovered && !grabInteractable.isSelected)
            TryToggle();
    }

    // ── Trigger while holding ─────────────────────────────────────────────────

    private void OnActivated(ActivateEventArgs args)
    {
        TryToggle();
    }

    // ── Shared toggle logic ───────────────────────────────────────────────────

    private void TryToggle()
    {
        if (Time.time - lastToggleTime < toggleCooldown) return;
        lastToggleTime = Time.time;
        Toggle();
    }

    private void Toggle()
    {
        bool old = isClosed;
        isClosed = !isClosed;
        Debug.Log($"[SWITCH][TOGGLE] {componentId}: {old} -> {isClosed}");
        CircuitManager.Instance?.NotifyConnectionChanged();
        animator.SetTrigger("Switch");
    }

    public override void AddToSpice(SpiceSharp.Circuit ckt, string nodeA, string nodeB)
    {
        double r = IsClosed ? 1e-3 : 1e12;
        ckt.Add(new Resistor($"R_{componentId}_SW", nodeA, nodeB, r));
    }
}

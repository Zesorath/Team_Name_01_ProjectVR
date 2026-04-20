using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class WireEnd : MonoBehaviour
{
    public enum MoveMode { None, ParentWire, FreeEnd, Locked }
    public Wire parentWire;
    [HideInInspector] public string endLabel = "?";
    public event Action<WireEnd> OnGrabStart;
    public event Action<WireEnd> OnGrabEnd;

    private MoveMode currentMoveMode = MoveMode.None;
    private bool isGrabbed = false;
    private Transform grabTarget;
    private Rigidbody rb;
    private Vector3 prevEndPos; // tracks the actual world position of THIS end, not the grab target

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    // So the parent's ComponentID is still reachable when un-parented from wire on grab
    public ComponentID parentCID;

    // Expose grabber so load/undo/redo can detatch it
    public XRGrabInteractable GetGrabber() { return grabInteractable; }

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        grabTarget = args.interactorObject.GetAttachTransform(args.interactableObject);
        prevEndPos = transform.position; // snapshot where THIS end actually is right now
        GrabStart();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        grabTarget = null;

        // Kill any leftover velocity so the wire end stays where it was dropped
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        GrabEnd();
    }

    public void SetMoveMode(MoveMode mode)
    {
        currentMoveMode = mode;
        Debug.Log($"{name} SetMoveMode: {mode}");
    }

    public override string ToString()
    {
        return parentWire != null ? $"{parentWire.name}_End{endLabel}" : $"OrphanEnd_{endLabel}";
    }

    public void GrabStart() => OnGrabStart?.Invoke(this);
    public void GrabEnd()   => OnGrabEnd?.Invoke(this);

    private void FixedUpdate()
    {
        if (!isGrabbed || grabTarget == null || rb == null) return;

        if (currentMoveMode == MoveMode.ParentWire)
        {
            // Use the ACTUAL movement of this end (post-physics from last step),
            // not the grab target delta. The grab target can rotate with the wrist
            // even when the hand isn't translating, producing spurious deltas that
            // make the other end spin in place.
            Vector3 actualDelta = transform.position - prevEndPos;
            prevEndPos = transform.position;

            WireEnd other = (parentWire.startpoint == this)
                ? parentWire.endpoint
                : parentWire.startpoint;

            if (other != null)
            {
                Rigidbody otherRb = other.GetComponent<Rigidbody>();
                if (otherRb != null)
                    otherRb.linearVelocity = actualDelta / Time.fixedDeltaTime;
            }
        }

        // FreeEnd:  XRI VelocityTracking handles everything — Rigidbody stays
        //           non-kinematic so it collides with table/shelf/walls normally.
        // Locked / None: do nothing.
    }
}

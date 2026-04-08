using UnityEngine;
using System;
using UnityEngine.XR.Interaction.Toolkit;

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
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    // So the parent's ComponentID is still reachable when un-parented from wire
    // on grab
    public ComponentID parentCID;

    private void Awake()
    {
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
        grabTarget = args.interactorObject.transform;
        GrabStart();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        grabTarget = null;
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
    public void GrabStart()
    {
        OnGrabStart?.Invoke(this);
    }
    public void GrabEnd()
    {
        OnGrabEnd?.Invoke(this);
    }
    private void Update()
    {
        if (!isGrabbed || grabTarget == null)
            return;

        switch (currentMoveMode)
        {
            case MoveMode.ParentWire:
                
                parentWire.transform.position = grabTarget.position;
                parentWire.transform.rotation = grabTarget.rotation;
                break;
            case MoveMode.FreeEnd:
                
                transform.position = grabTarget.position;
                transform.rotation = grabTarget.rotation;
                break;
            case MoveMode.Locked:
            case MoveMode.None:
                // Do nothing
                break;
        }
    }

    Vector3 lastPos;
    Quaternion lastRot;
    bool initialized = false;

    void LateUpdate()
    {
        if (!initialized)
        {
            lastPos = transform.position;
            lastRot = transform.rotation;
            initialized = true;
            return;
        }

        if (transform.position != lastPos || transform.rotation != lastRot)
        {
            Debug.Log($"[WireEnd] {name} CHANGED\n" +
                    $"world pos: {lastPos} -> {transform.position}\n" +
                    $"world rot: {lastRot.eulerAngles} -> {transform.rotation.eulerAngles}\n" +
                    $"local pos now: {transform.localPosition}\n" +
                    $"local rot now: {transform.localRotation.eulerAngles}\n" +
                    $"parent: {transform.parent?.name}");
            lastPos = transform.position;
            lastRot = transform.rotation;
        }
    }
}

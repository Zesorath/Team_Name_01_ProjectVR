using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using SpiceSharp;
using SpiceSharp.Components;

public class SwitchComponent : CircuitComponentBase
{
    [Header("State")]
    [SerializeField] private bool isClosed = false;
    public bool IsClosed => isClosed;

    [Header("Handle Interactable (top cylinder)")]
    [SerializeField] private XRBaseInteractable handleInteractable;

    private Resistor _spiceResistor;

    protected override void Awake()
    {
        base.Awake();

        if (!handleInteractable)
        {
            handleInteractable = GetComponentInChildren<XRSimpleInteractable>(true);

            if (!handleInteractable)
                handleInteractable = GetComponentInChildren<XRBaseInteractable>(true);

            var grab = GetComponent<XRGrabInteractable>();
            var handleCol = handleInteractable?.GetComponent<Collider>();

            if (grab && handleCol)
            {
                grab.colliders.Remove(handleCol);
                Debug.Log($"[SWITCH] Removed handle collider from grab colliders: {handleCol.name}");
            }
        }

        Debug.Log($"[SWITCH][Awake] {componentId}");
    }

    private void OnEnable()
    {
        if (!handleInteractable)
        {
            Debug.LogError($"[SWITCH] {componentId}: handleInteractable not assigned!");
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
        Debug.Log($"[SWITCH][SELECT] {componentId}");
        Toggle();
    }

    private void Toggle()
    {
        isClosed = !isClosed;

        CircuitManager.Instance?.QueueSpiceMutation(() =>
        {
            if (_spiceResistor != null)
            {
                _spiceResistor.Parameters.Resistance =
                    isClosed ? 0.1 : 1e9;

                Debug.Log($"[SWITCH] {(isClosed ? "CLOSED" : "OPEN")} safely applied.");
            }
        });
    }

    public override void AddToSpice(Circuit ckt, string nodeA, string nodeB)
    {
        _spiceResistor = new Resistor(
            $"S_{componentId}",
            nodeA,
            nodeB,
            isClosed ? 1e-3 : 1e15
        );

        ckt.Add(_spiceResistor);

        Debug.Log($"[SWITCH] Resistor switch added: {nodeA} {nodeB}");
    }
}
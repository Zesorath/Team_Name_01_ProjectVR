using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PortSocketBinder : MonoBehaviour
{
    [Tooltip("XR Socket on this port")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    [Tooltip("Which component this port belongs to")]
    public CircuitComponent owner;

    [Tooltip("Index of this terminal in the owner component (0,1,2,...)")]
    public int terminalIndex;

    [HideInInspector] public WireEnd connectedWireEnd;

    void Awake()
    {
        if (socket == null)
            socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();

        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
            Debug.Log($"[PortSocketBinder] Subscribed to XR socket events on {name}.");
        }
        else
        {
            Debug.LogWarning($"[PortSocketBinder] No XRSocketInteractor found on {name}.");
        }
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
        }
    }

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        var wireEnd = args.interactableObject.transform.GetComponentInParent<WireEnd>();
        if (wireEnd == null)
        {
            Debug.Log($"[PortSocketBinder] {name} Selected object is not a WireEnd.");
            return;
        }

        connectedWireEnd = wireEnd;
        wireEnd.pluggedInto = this;
        Debug.Log($"[PortSocketBinder] {name} connected to wire end {wireEnd.name}.");

        CircuitManager.Instance?.OnConnectionChanged();
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (connectedWireEnd != null)
        {
            Debug.Log($"[PortSocketBinder] {name} disconnected from wire end {connectedWireEnd.name}.");
            connectedWireEnd.pluggedInto = null;
            connectedWireEnd = null;

            CircuitManager.Instance?.OnConnectionChanged();
        }
    }

    public bool IsConnected => connectedWireEnd != null;
}

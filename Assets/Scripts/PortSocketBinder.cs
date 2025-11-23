using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class PortSocketBinder : MonoBehaviour
{
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    private CircuitComponentBase owningComponent;
    private WireEnd connectedEnd;

    private void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        owningComponent = GetComponentInParent<CircuitComponentBase>();

        if (owningComponent == null)
        {
            Debug.LogError($"[PortSocketBinder] {name} could not find CircuitComponentBase on parent.");
        }

        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);

        Debug.Log($"[PortSocketBinder] Ready on {owningComponent?.componentId ?? name}");
    }

    private void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var t = args.interactableObject.transform;
        Debug.Log($"[PortSocketBinder] {owningComponent?.componentId} received object: {t.name}");

        // Look for a WireEnd on the grabbed object or its parents
        var end = t.GetComponentInParent<WireEnd>();
        if (end == null || end.parentWire == null)
        {
            Debug.Log($"[PortSocketBinder] {owningComponent?.componentId} got non-wire interactable {t.name}.");
            return;
        }

        connectedEnd = end;
        end.parentWire.NotifyEndConnected(end, owningComponent);

        if (CircuitManager.Instance != null)
        {
            Debug.Log("[PortSocketBinder] Connection changed � notifying CircuitManager.");
            CircuitManager.Instance.NotifyConnectionChanged();
        }
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        if (connectedEnd != null && connectedEnd.parentWire != null && owningComponent != null)
        {
            connectedEnd.parentWire.NotifyEndDisconnected(connectedEnd, owningComponent);
        }

        connectedEnd = null;

        if (CircuitManager.Instance != null)
        {
            Debug.Log("[PortSocketBinder] Disconnection � notifying CircuitManager.");
            CircuitManager.Instance.NotifyConnectionChanged();
        }
    }
}

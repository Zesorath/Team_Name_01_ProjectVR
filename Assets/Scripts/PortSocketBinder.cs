using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PortSocketBinder : MonoBehaviour
{
    public CircuitComponentBase component;
    private UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.selectEntered.AddListener(OnSelectEntered);
        socket.selectExited.AddListener(OnSelectExited);
        Debug.Log($"[PortSocketBinder] Ready on {component?.componentId}");
    }

    private void OnDestroy()
    {
        socket.selectEntered.RemoveListener(OnSelectEntered);
        socket.selectExited.RemoveListener(OnSelectExited);
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        var wire = args.interactableObject.transform.GetComponentInParent<Wire>();
        if (wire == null) return;

        if (wire.endA == null)
            wire.endA = this;
        else
            wire.endB = this;

        Debug.Log($"[PortSocketBinder] {component.componentId} connected");

        CircuitManager.Instance.NotifyConnectionChanged();
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        var wire = args.interactableObject.transform.GetComponentInParent<Wire>();
        if (wire == null) return;

        if (wire.endA == this) wire.endA = null;
        if (wire.endB == this) wire.endB = null;

        Debug.Log($"[PortSocketBinder] {component.componentId} disconnected");

        CircuitManager.Instance.NotifyConnectionChanged();
    }
}

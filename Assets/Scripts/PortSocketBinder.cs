// PortSocketBinder.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class PortSocketBinder : MonoBehaviour
{
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;
    public PortAnchor socketPort;   // assign the PortAnchor on THIS socket object

    PortAnchor _lastTip;            // the PortAnchor that is currently inserted

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        if (!socketPort) socketPort = GetComponent<PortAnchor>(); // fallback
        // subscribe
        socket.selectEntered.AddListener(OnIn);
        socket.selectExited.AddListener(OnOut);
        Debug.Log($"[Binder {name}] Subscribed to XR socket events.");
    }

    void OnDestroy()
    {
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnIn);
            socket.selectExited.RemoveListener(OnOut);
        }
    }

    void OnIn(SelectEnterEventArgs args)
    {
        var go = args.interactableObject.transform.gameObject;

        // We expect a PortAnchor on the INSERTED object (your plug tip)
        if (!go.TryGetComponent<PortAnchor>(out var tipAnchor))
        {
            Debug.LogWarning($"[Binder {name}] selectEntered: inserted object lacks PortAnchor");
            return;
        }
        if (!socketPort)
        {
            Debug.LogWarning($"[Binder {name}] has no socketPort (PortAnchor) assigned.");
            return;
        }

        _lastTip = tipAnchor;
        CircuitManager.Instance.RegisterJunction(socketPort, tipAnchor);
        Debug.Log($"[Binder {name}] Bound socketPort '{socketPort.pinName}' <-> tip '{tipAnchor.pinName}'.");
    }

    void OnOut(SelectExitEventArgs args)
    {
        if (_lastTip && socketPort)
        {
            CircuitManager.Instance.UnregisterJunction(socketPort, _lastTip);
            Debug.Log($"[Binder {name}] Cleared junction for tip '{_lastTip.pinName}'.");
        }
        _lastTip = null;
    }
}

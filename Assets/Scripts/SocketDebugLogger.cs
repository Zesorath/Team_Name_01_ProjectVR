using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor))]
public class SocketDebugLogger : MonoBehaviour
{
    UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor socket;

    void Awake()
    {
        socket = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor>();
        socket.hoverEntered.AddListener(OnHoverEntered);
        socket.hoverExited.AddListener(OnHoverExited);
    }

    void OnDestroy()
    {
        if (socket == null) return;
        socket.hoverEntered.RemoveListener(OnHoverEntered);
        socket.hoverExited.RemoveListener(OnHoverExited);
    }

    void OnHoverEntered(HoverEnterEventArgs args)
    {
        Debug.Log($"[SocketDebug] {name} hover ENTER by {args.interactableObject.transform.name}");
    }

    void OnHoverExited(HoverExitEventArgs args)
    {
        Debug.Log($"[SocketDebug] {name} hover EXIT by {args.interactableObject.transform.name}");
    }
}

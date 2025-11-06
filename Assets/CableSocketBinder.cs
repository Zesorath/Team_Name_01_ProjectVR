using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class CableSocketBinder : MonoBehaviour
{
    public enum SocketRole { DC, Resistor }

    [Header("Config")]
    public SocketRole role;
    public XRSocketInteractor socket;
    public CableLink cable;

    [Header("DC side (if role = DC)")]
    public Direct_Current dcSource;
    public Transform dcTip;   // child with CableTip on the DC probe

    [Header("Resistor side (if role = Resistor)")]
    public Ohms resistor;     // your resistor component (or Resitor)
    public Transform rTip;    // child with CableTip on the resistor probe

    void Awake()
    {
        if (socket == null) socket = GetComponent<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.RemoveListener(OnSelectEntered);
            socket.selectExited.RemoveListener(OnSelectExited);
            socket.selectEntered.AddListener(OnSelectEntered);
            socket.selectExited.AddListener(OnSelectExited);
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

    public void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (cable == null) return;

        // The thing we plugged is usually the wire plug (child collider)
        var comp = args.interactableObject as UnityEngine.Component;
        if (comp == null) return;

        // We only care that a plug is present. Data comes from known scene objects (dcSource / resistor).
        if (role == SocketRole.DC)
        {
            if (dcSource == null || dcTip == null)
            {
                Debug.LogWarning("[Cable] DC binder missing dcSource or dcTip.");
                return;
            }
            cable.SetDCEnd(dcSource, dcTip);  // no resistor required yet
        }
        else // Resistor side
        {
            if (resistor == null || rTip == null)
            {
                // Try to find Ohms/Resitor on the connected object as a fallback
                var ohms = comp.GetComponentInParent<Ohms>() ?? comp.GetComponentInChildren<Ohms>();
                if (ohms != null) resistor = ohms;

                var tip = comp.GetComponentInChildren<CableTip>();
                if (tip != null) rTip = tip.transform;

                if (resistor == null || rTip == null)
                {
                    Debug.Log("[Cable] Resistor binder missing resistor or rTip.");
                    return;
                }
            }
            cable.SetResistorEnd(resistor, rTip);
        }

        // CableLink will show the wire (if assigned) and only compute when both ends are present.
    }

    public void OnSelectExited(SelectExitEventArgs args)
    {
        if (cable == null) return;

        if (role == SocketRole.DC)
            cable.ClearDCEnd();
        else
            cable.ClearResistorEnd();
    }
}

using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class WireEnd : MonoBehaviour
{
    [Tooltip("The parent Wire that owns this end")]
    public Wire parentWire;

    [HideInInspector] public PortSocketBinder pluggedInto;

    void Awake()
    {
        if (parentWire == null)
            parentWire = GetComponentInParent<Wire>();
    }
}

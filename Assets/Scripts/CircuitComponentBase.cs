using UnityEngine;
using SpiceSharp;

public abstract class CircuitComponentBase : MonoBehaviour
{
    public string componentId;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(componentId))
            componentId = gameObject.name;
    }

    public abstract void AddToSpice(Circuit ckt, string nodeA, string nodeB);
}

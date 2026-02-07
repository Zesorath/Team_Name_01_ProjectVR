using UnityEngine;
using SpiceSharp;
using System.Collections.Generic;

public abstract class CircuitComponentBase : MonoBehaviour
{
    public string componentId;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(componentId))
            componentId = gameObject.name;
    }

    public abstract void AddToSpice(Circuit ckt, string nodeA, string nodeB);

    // Auto-discover ports from children instead of manually assigning portA/portB
    public virtual IEnumerable<PortSocketBinder> GetPorts()
    {
        var ports = GetComponentsInChildren<PortSocketBinder>(true);

        if (ports == null || ports.Length == 0)
            Debug.LogError($"{componentId}: No PortSocketBinder found under this component. Check prefab hierarchy.");

        return ports;
    }

    // Do whatever needs to be done for the deletion to go smoothly here
    public void Delete()
    {
        SaveManager sm = SaveManager.Instance;
        ComponentID cID = gameObject.GetComponent<ComponentID>();

        // Unregister object from save manager
        sm.Unregister(cID);

        Destroy(gameObject);
    }
}

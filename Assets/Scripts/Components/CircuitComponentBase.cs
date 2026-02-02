using UnityEngine;
using SpiceSharp;
using System.Collections.Generic;
using System.Linq;
public abstract class CircuitComponentBase : MonoBehaviour
{
    public string componentId;

    protected virtual void Awake()
    {
        if (string.IsNullOrEmpty(componentId))
            componentId = gameObject.name;
    }

    public abstract void AddToSpice(Circuit ckt, string nodeA, string nodeB);
    public PortSocketBinder portA;
    public PortSocketBinder portB;

    public virtual IEnumerable<PortSocketBinder> GetPorts()
    {
        if (portA == null || portB == null)
            Debug.LogError($"{componentId}: portA or portB is not assigned!");

        return new[] { portA, portB };
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

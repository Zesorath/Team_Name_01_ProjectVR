using UnityEngine;

public class Wire : MonoBehaviour
{
    public PortSocketBinder endA;
    public PortSocketBinder endB;

    public bool IsComplete => endA != null && endB != null;

    public (CircuitComponentBase, CircuitComponentBase) GetConnectionPair()
    {
        if (!IsComplete)
            return (null, null);

        return (endA.component, endB.component);
    }

    private void OnDestroy()
    {
        // Cleanup signal for safety
        if (CircuitManager.Instance)
            CircuitManager.Instance.NotifyConnectionChanged();
    }
}

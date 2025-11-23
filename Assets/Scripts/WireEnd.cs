using UnityEngine;

/// <summary>
/// Represents a physical end of a wire that the user can grab/snap.
/// Sockets will interact with this, not with the Wire root directly.
/// </summary>
public class WireEnd : MonoBehaviour
{
    [HideInInspector] public Wire parentWire;
    [HideInInspector] public string endLabel = "?";

    public override string ToString()
    {
        return parentWire != null ? $"{parentWire.name}_End{endLabel}" : $"OrphanEnd_{endLabel}";
    }
}

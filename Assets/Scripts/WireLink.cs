// WireLink.cs
using UnityEngine;

public class WireLink : MonoBehaviour
{
    public PortAnchor endA;
    public PortAnchor endB;

    public bool IsComplete => endA != null && endB != null;
}

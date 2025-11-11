// WireGizmos.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(WireLink))]
public class WireGizmos : MonoBehaviour
{
    public Color colorWhenConnected = new Color(1f, 0.65f, 0f, 1f);
    public Color colorWhenOpen = new Color(1f, 0f, 0f, 1f);
    public float sphereSize = 0.01f;

    void OnDrawGizmos()
    {
        var link = GetComponent<WireLink>();
        if (!link) return;

        var a = link.endA ? link.endA.transform.position : (Vector3?)null;
        var b = link.endB ? link.endB.transform.position : (Vector3?)null;

        if (a.HasValue)
        {
            Gizmos.color = link.IsComplete ? colorWhenConnected : colorWhenOpen;
            Gizmos.DrawSphere(a.Value, sphereSize);
        }
        if (b.HasValue)
        {
            Gizmos.color = link.IsComplete ? colorWhenConnected : colorWhenOpen;
            Gizmos.DrawSphere(b.Value, sphereSize);
        }
        if (a.HasValue && b.HasValue)
        {
            Gizmos.color = colorWhenConnected;
            Gizmos.DrawLine(a.Value, b.Value);
        }
    }
}

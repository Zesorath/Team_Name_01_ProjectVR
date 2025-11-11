// PortAnchorGizmos.cs
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(PortAnchor))]
public class PortAnchorGizmos : MonoBehaviour
{
    public Color color = Color.cyan;
    public float size = 0.015f;

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, size);
#if UNITY_EDITOR
        var port = GetComponent<PortAnchor>();
        if (port)
            UnityEditor.Handles.Label(transform.position + Vector3.up * (size * 1.5f),
                string.IsNullOrEmpty(port.pinName) ? "(port)" : port.pinName);
#endif
    }
}

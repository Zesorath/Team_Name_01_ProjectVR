using UnityEngine;

/// Attach to a GameObject with a LineRenderer. Assign EndA/EndB (the probe tips).
[RequireComponent(typeof(LineRenderer))]
public class KinematicWire : MonoBehaviour
{
    [Header("Ends")]
    public Transform endA;
    public Transform endB;

    [Header("Look/Feel")]
    [Range(2, 128)] public int segments = 24;     // curve resolution
    public float thickness = 0.01f;               // meters
    public float slack = 0.15f;                   // how much the wire sags (scaled by distance)
    public Vector3 sagDirection = Vector3.down;   // usually down

    [Header("Behavior")]
    public bool autoHideIfMissingEnd = true;      // hide when unplugged

    LineRenderer lr;

    void Awake()
    {
        lr = GetComponent<LineRenderer>();
        lr.positionCount = segments + 1;
        lr.useWorldSpace = true;
        lr.widthMultiplier = thickness;
    }

    void LateUpdate()
    {
        if (endA == null || endB == null)
        {
            if (autoHideIfMissingEnd && lr.enabled) lr.enabled = false;
            return;
        }
        if (!lr.enabled) lr.enabled = true;

        Vector3 p0 = endA.position;
        Vector3 p2 = endB.position;

        // Quadratic Bézier control point with “sag”
        float d = Vector3.Distance(p0, p2);
        Vector3 mid = (p0 + p2) * 0.5f;
        Vector3 p1 = mid + sagDirection.normalized * (slack * d);

        // Sample the curve
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            Vector3 a = Vector3.Lerp(p0, p1, t);
            Vector3 b = Vector3.Lerp(p1, p2, t);
            Vector3 point = Vector3.Lerp(a, b, t); // Quadratic Bézier
            lr.SetPosition(i, point);
        }

        // Keep thickness in sync if edited at runtime
        lr.widthMultiplier = thickness;
    }

    // Call this to hook up ends at runtime (e.g., when sockets connect)
    public void SetEnds(Transform a, Transform b)
    {
        endA = a;
        endB = b;
    }
}

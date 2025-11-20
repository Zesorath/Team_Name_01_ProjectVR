using UnityEngine;

public class Wire : MonoBehaviour
{
    [Tooltip("First end of the wire")]
    public WireEnd endA;

    [Tooltip("Second end of the wire")]
    public WireEnd endB;

    [Tooltip("Optional visual rope/line component")]
    public KinematicCableTube tube; 

    void Awake()
    {
        // Try to auto-fill ends if they weren't assigned in the Inspector
        if (endA == null || endB == null)
        {
            var ends = GetComponentsInChildren<WireEnd>();
            if (ends.Length >= 2)
            {
                endA = ends[0];
                endB = ends[1];
            }
        }

        // Try to auto-fill the visual line if not set
        if (tube == null)
        {
            tube = GetComponent<KinematicCableTube>();
        }

        // Now safely hook up the visuals if everything is present
        if (tube != null && endA != null && endB != null)
        {
            tube.endA = endA.transform;
            tube.endB = endB.transform;
        }
        else
        {
            Debug.LogWarning(
                $"[Wire] Missing references on '{name}'. " +
                $"line={tube}, endA={endA}, endB={endB}"
            );
        }
    }
}

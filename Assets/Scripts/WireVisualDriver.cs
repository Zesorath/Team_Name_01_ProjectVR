// WireVisualDriver.cs
using UnityEngine;
public class WireVisualDriver : MonoBehaviour
{
    public WireLink link;
    public KinematicCableTube tube;
    void LateUpdate()
    {
        if (link != null && tube != null && link.IsComplete)
        {
            tube.endA = link.endA.transform;
            tube.endB = link.endB.transform;
            if (!tube.gameObject.activeSelf) tube.gameObject.SetActive(true);
        }
        else if (tube != null && tube.gameObject.activeSelf)
        {
            tube.gameObject.SetActive(false);
        }
    }
}

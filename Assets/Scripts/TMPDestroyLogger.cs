using UnityEngine;
using TMPro;

public class TMPDestroyLogger : MonoBehaviour
{
    void OnDestroy()
    {
        Debug.Log($"[TMPDestroyLogger] {name} was destroyed at time {Time.time}.");
    }
}

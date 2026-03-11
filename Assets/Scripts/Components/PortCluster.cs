using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PortCluster : MonoBehaviour
{
    [Tooltip("Optional label for debugging (ex: A, B, Positive, Negative).")]
    public string terminalId = "A";

    [Tooltip("All sockets that represent the same electrical terminal (same node).")]
    public PortSocketBinder[] sockets;

    private void Awake()
    {
        if (sockets == null || sockets.Length == 0)
            sockets = GetComponentsInChildren<PortSocketBinder>(true);
    }

    public IEnumerable<PortSocketBinder> GetSockets()
    {
        if (sockets == null) yield break;
        foreach (var s in sockets)
            if (s != null) yield return s;
    }

    public PortSocketBinder AnySocket()
    {
        return GetSockets().FirstOrDefault();
    }
}

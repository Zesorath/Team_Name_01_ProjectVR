using UnityEngine;

public class PortCluster : MonoBehaviour
{
    public PortSocketBinder[] sockets;

    private void Awake()
    {
        if (sockets == null || sockets.Length == 0)
            sockets = GetComponentsInChildren<PortSocketBinder>(true);
    }
}

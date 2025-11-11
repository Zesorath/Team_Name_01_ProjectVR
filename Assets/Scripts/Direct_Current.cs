using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;
using System.Collections.Generic;

public class DC : MonoBehaviour, ISpiceDevice
{
    public string spiceId; // will be uniquified
    public float voltage = 5f;
    public PortAnchor positive, negative; // negative can be ground if wired to node "0"

    public string SpiceName => "V";


    public IEnumerable<PortAnchor> GetPorts() { yield return positive; yield return negative; }

    public void Contribute(Circuit ckt, System.Func<PortAnchor, string> NodeByPort)
    {
        var np = NodeByPort(positive);
        var nn = NodeByPort(negative);
        ckt.Add(new VoltageSource(spiceId, np, nn, voltage));
        Debug.Log($"[Circuit DBG] DC source added: id={spiceId} V={voltage} +={np} -={nn}");
    }
}

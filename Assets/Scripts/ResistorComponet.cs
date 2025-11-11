// ResistorDevice.cs
using UnityEngine;
using SpiceSharp;
using SpiceSharp.Components;
using System.Collections.Generic;

public class ResistorComponet : MonoBehaviour, ISpiceDevice
{
    public PortAnchor a, b;
    public float resistance = 1000f;
    public string spiceId;
    public string SpiceName => "R";


    public IEnumerable<PortAnchor> GetPorts() { yield return a; yield return b; }

    public void Contribute(Circuit ckt, System.Func<PortAnchor, string> NodeByPort)
    {
        var na = NodeByPort(a);
        var nb = NodeByPort(b);
        ckt.Add(new Resistor(spiceId, na, nb, resistance));
        Debug.Log($"[Circuit DBG] Resistor added: id={spiceId} R={resistance} a={na} b={nb}");
    }
}

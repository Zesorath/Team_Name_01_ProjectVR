// ISpiceDevice.cs
using SpiceSharp;
using System.Collections.Generic;

public interface ISpiceDevice
{
    // Called after nodes are assigned. Use nodeByPort[myPort] to get the node name string.
    void Contribute(Circuit ckt, System.Func<PortAnchor, string> nodeByPort);
    // Optional: a stable name prefix for exports
    string SpiceName { get; }
    // All ports this device owns (for the net builder to discover its nodes)
    IEnumerable<PortAnchor> GetPorts();
}

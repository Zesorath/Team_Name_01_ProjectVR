using System;
using System.Data;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    readonly SaveDebug d = 
        new SaveDebug("<color=black>[TrashCan] </color>");
    
    private void OnTriggerEnter(Collider other)
    {        
        // Components own their own cID. Just grab it and delete the object.
        ComponentID cID = other.GetComponent<ComponentID>();
        if (cID) { cID.Delete(); return; }

        // If it's a wire, destroy both ends of the wire before deleting and
        // unregistering the parent. This handles a bug where the trash can
        // couldn't delete connected wire ends.
        WireEnd wireEnd = other.GetComponent<WireEnd>();
        if (wireEnd)
        {
            cID = wireEnd.parentCID;
            cID.Delete();
            return;
        }
        
        // Otherwise, the object can't be deleted with the trash can
        d.Error($"No ComponentID found on {other.name}. Cannot delete");
    }
}

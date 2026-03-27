using System;
using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {        
        ComponentID cID = other.GetComponent<ComponentID>();
        if (cID)
        {
            if(other.gameObject.CompareTag("Wire End"))
            {
                GameObject obj = other.gameObject;
                GameObject wireParent = obj.GetComponent<WireEnd>().parentWire.gameObject;
                cID = wireParent.GetComponent<ComponentID>();
                print("!!!wire " + wireParent + ", " + cID);
            }
            else
            {
                print("!!!not wire");    
            }
            cID.Delete();
        }
    }
}

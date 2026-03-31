using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {        
        ComponentID cID = other.GetComponent<ComponentID>();
        if (cID)
        {
            cID.Delete();
        }
    }
}

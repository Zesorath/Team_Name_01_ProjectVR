using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        CircuitComponentBase Component = other.gameObject.GetComponent<CircuitComponentBase>();
        if (Component)
        {
            Component.Delete();
        }
    }
}

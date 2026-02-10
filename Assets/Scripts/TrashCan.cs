using UnityEngine;

public class TrashCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (SaveManager.Instance.isLoadingOrSaving) { return; }
        
        CircuitComponentBase Component = other.gameObject.GetComponent<CircuitComponentBase>();
        if (Component)
        {
            Component.Delete();
        }
    }
}

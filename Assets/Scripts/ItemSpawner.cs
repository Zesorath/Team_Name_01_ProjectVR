using TMPro;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{

    public GameObject Item_To_Spawn;
    public float Respawn_Radius;

    private GameObject LastItem;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Spawn();
        transform.GetChild(0).GetComponent<TextMeshPro>().text = Item_To_Spawn.name;
    }

    // Update is called once per frame
    void Update()
    {
        // Load() could cause LastItem to be destroyed. It that case, re-spawn 
        if (LastItem == null)
        {
            Spawn();
            return;
        }
        
        if ( (LastItem.transform.position-this.transform.position).magnitude > Respawn_Radius )
        {
            Spawn();
        }
    }

    void Spawn()
    {
        GameObject inst = Instantiate(Item_To_Spawn, this.transform);
        LastItem = inst;
    }
}

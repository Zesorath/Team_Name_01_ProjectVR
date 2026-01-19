using UnityEngine;

public class HotKeys : MonoBehaviour
{
    /// <summary>
    /// Register all hotkeys
    /// </summary>
    void Update()
    {
        // Save current scene
        if (Input.GetKeyDown(KeyCode.F5)) 
        {
            Debug.Log($"F5 pressed. Saving");
            SaveManager.Instance.Save();
        }
    }
}

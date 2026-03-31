using UnityEngine;

/// <summary>
/// Listens for hotkey presses
/// </summary>
public class InputManager : MonoBehaviour
{
    /// <summary>
    /// Register all hotkeys
    /// </summary>
    void Update()
    {
        SaveManager sm = SaveManager.Instance;
        string splash = "[InputManager]: ";
        
        // Save current scene to file
        if (Input.GetKeyDown(KeyCode.F5)) 
        {
            Debug.Log($"{splash}F5 pressed. Calling QuickSave()");
            sm.QuickSave();
        }

        // Load from the most recent save file
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log($"{splash}F9 pressed. Calling QuickLoad()");
            sm.QuickLoad();
        }
    }
}

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
        // Save current scene to file
        if (Input.GetKeyDown(KeyCode.F5)) 
        {
            Debug.Log($"F5 pressed. Calling Save()");
            SaveManager.Instance.Save();
        }

        // Will load from the save file
        if (Input.GetKeyDown(KeyCode.F9))
        {
            Debug.Log("F9 pressed. Calling Load()");
            SaveManager.Instance.Load();
        }
    }
}

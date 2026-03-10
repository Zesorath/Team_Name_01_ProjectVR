using UnityEngine;

/// <summary>
/// Listens for hotkey presses
/// </summary>
public class InputManager : MonoBehaviour
{
    private static InputManager instance;

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
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

        if (Input.GetKeyDown(KeyCode.F6)) sm.SaveToSlot(1);
        if (Input.GetKeyDown(KeyCode.F7)) sm.SaveToSlot(2);
        if (Input.GetKeyDown(KeyCode.F8)) sm.SaveToSlot(3);

        if (Input.GetKeyDown(KeyCode.F10)) sm.LoadFromSlot(1);
        if (Input.GetKeyDown(KeyCode.F11)) sm.LoadFromSlot(2);
        if (Input.GetKeyDown(KeyCode.F12)) sm.LoadFromSlot(3);
    }
}

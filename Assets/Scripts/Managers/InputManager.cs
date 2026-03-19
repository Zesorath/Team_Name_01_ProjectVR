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
        UndoManager um = UndoManager.Instance;
        string splash = "[InputManager]: ";

        bool ctrl = Input.GetKey(KeyCode.LeftControl) 
            || Input.GetKey(KeyCode.RightControl);
        
        // Save current scene to the active save slot
        if (Input.GetKeyDown(KeyCode.F5)) 
        {
            Debug.Log($"{splash}F5 pressed. Calling QuickSave()");
            sm.QuickSave();
        }

        // Restore the most recent saved state from the current save slot
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

        if (ctrl && Input.GetKeyDown(KeyCode.F9)) sm.Continue();

        if (ctrl && Input.GetKeyDown(KeyCode.Z)) um.Undo();
        if (ctrl && Input.GetKeyDown(KeyCode.Y)) um.Redo();
    }
}

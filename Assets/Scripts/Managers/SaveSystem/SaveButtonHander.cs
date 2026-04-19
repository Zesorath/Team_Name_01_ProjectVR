// SaveButtonHandler.cs
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Attach this to a GameObject (for example, an "UI Manager" GameObject)
/// and then hook the public methods to your UI Buttons' OnClick events.
/// These methods call the SaveManager singleton which actually performs
/// the save/load work.
/// </summary>
public class SaveButtonHandler : MonoBehaviour
{
    // No need for Awake/Start; this is simply a bridge to SaveManager.
    SaveManager sm = SaveManager.Instance;
    UndoManager um = UndoManager.Instance;
    [NonSerialized] readonly SaveDebug d = 
        new SaveDebug("<color=#1976D2>[SaveButtonManager] </color>");
    [NonSerialized] readonly UndoDebug u = 
        new UndoDebug("<color=#1976D2>[SaveButtonManager] </color>");

    public void Undo()
    {
        u.Error("No more undo");
        // um.Undo();
    }

    public void Redo()
    {
        u.Error("No more redo");
        // um.Redo();
    }

    public void Continue()
    {
        sm.Continue();
    }

    // Calls SaveManager.Save()--DON'T NEED. Don't use the raw Save() function
    public void OnSave()
    {
        d.Error("PLAIN Save() IS NOW PRIVATE.");
    }

    // Save the current scene's state
    public void OnQuickSave()
    {
        sm.QuickSave();
    }

    public void OnSaveToSlot(int slotNo)
    {
        sm.EnterLesson(slotNo);
    }

    // Calls SaveManager.QuickLoad() � loads the last saved file recorded in the manifest
    public void OnQuickLoad()
    {
        sm.QuickLoad();
    }

    public void OnLoadFromSlot(int slotNo)
    {
        SceneManager.LoadScene("ExitScene");
        // sm.LoadFromSlot(slotNo);
    }

    // Calls SaveManager.Load with a filename parameter (e.g. "GUID.json")
    // To use this from a Button in the Inspector, choose the method that
    // accepts a string and type the filename in the inspector field.
    public void OnLoadByFilename(string filename)
    {
        d.Error("PLAIN Load() IS NOW PRIVATE.");
        
        // if (string.IsNullOrEmpty(filename))
        // {
        //     Debug.LogWarning("[SaveButtonHandler] Empty filename supplied to OnLoadByFilename.");
        //     return;
        // }
        // sm.Load(filename);
    }


}

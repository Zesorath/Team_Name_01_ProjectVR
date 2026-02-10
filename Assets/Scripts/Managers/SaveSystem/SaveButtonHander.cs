// SaveButtonHandler.cs
using UnityEngine;

/// <summary>
/// Attach this to a GameObject (for example, an "UI Manager" GameObject)
/// and then hook the public methods to your UI Buttons' OnClick events.
/// These methods call the SaveManager singleton which actually performs
/// the save/load work.
/// </summary>
public class SaveButtonHandler : MonoBehaviour
{
    // No need for Awake/Start; this is simply a bridge to SaveManager.

    // Calls SaveManager.Save()
    public void OnSave()
    {
        SaveManager.Instance.Save();
    }

    // Shortcut that calls SaveManager.QuickSave()
    public void OnQuickSave()
    {
        SaveManager.Instance.QuickSave();
    }

    // Calls SaveManager.Load with a filename parameter (e.g. "GUID.json")
    // To use this from a Button in the Inspector, choose the method that
    // accepts a string and type the filename in the inspector field.
    public void OnLoadByFilename(string filename)
    {
        if (string.IsNullOrEmpty(filename))
        {
            Debug.LogWarning("[SaveButtonHandler] Empty filename supplied to OnLoadByFilename.");
            return;
        }
        SaveManager.Instance.Load(filename);
    }

    // Calls SaveManager.QuickLoad() — loads the last saved file recorded in the manifest
    public void OnQuickLoad()
    {
        SaveManager.Instance.QuickLoad();
    }
}

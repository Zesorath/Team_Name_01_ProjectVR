using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveSlot
{
    [NonSerialized] SaveDebug d;
    SaveDebug D()
    {
        if (d == null)
            d = new SaveDebug("<color=#5C6BC0>[SaveSlot] </color>");
        return d;
    }

    public bool isEmpty = true;
    [SerializeField] string filePath = "";
    [SerializeField] string level = "";
    public string display = "[EMPTY]";
    [SerializeField] string whenLastUsed = "";


    public void MarkNotEmpty() { isEmpty = false; }

    public void MakeEmpty()
    {
        isEmpty = true;
        filePath = "";
        level = "";
        display = "[EMPTY]";
        whenLastUsed = "";
    }


    public void Capture_SaveFilePath(SaveData sd)
    {
        SaveManager sm = SaveManager.Instance;
        filePath = Path.Combine(sm.paths.saveFilesPath, $"{sd.saveID}.json");
    }

    public string Get_FilePath() { return filePath; }


    public void Capture_LevelData()
    {
        level = SceneManager.GetActiveScene().name;
        display = level;
    }

    public void Set_LevelData(string sceneName)
    {
        level = sceneName;
        display = level;
    }

    public string Get_LevelData() { return level; }


    public void Capture_WhenLastUsed()
    {
        whenLastUsed = DateTime.UtcNow.ToString("o");
    }

    // Returns UTC timestamp object
    public DateTime Get_WhenLastUsed_UTC()
    {
        if (string.IsNullOrEmpty(whenLastUsed)) return DateTime.MinValue;
        return DateTime.Parse(
            whenLastUsed,null,System.Globalization.DateTimeStyles.RoundtripKind
        );
    }
    
    // Returns local timestamp object
    public DateTime Get_WhenLastUsed_local()
    {
        DateTime utc = Get_WhenLastUsed_UTC();
        if (utc == DateTime.MinValue) return DateTime.MinValue;
        return utc.ToLocalTime();
    }
    
    // Returns local timestamp as a formatted string
    public string Get_WhenLastUsed_formatted()
    {
        DateTime utc = Get_WhenLastUsed_UTC();
        if (utc == DateTime.MinValue) return "";
        return utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

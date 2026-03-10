using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveSlot
{
    [NonSerialized]
    readonly SaveDebug d = new SaveDebug("<color=#5C6BC0>[SaveSlot] </color>");
    
    public int slotID;
    string display = "[EMPTY]";
    string fileName = "";
    string level = "";
    DateTime whenLastUsed = DateTime.MinValue;

    public void CaptureSlotMetadata(SaveData sd)
    {
        fileName = $"{sd.saveID}.json";
        level = SceneManager.GetActiveScene().name;
        whenLastUsed = DateTime.UtcNow;
    }

    public void CaptureWhenLastUsed() { whenLastUsed = DateTime.UtcNow; }

    // Returns UTC timestamp object
    public DateTime GetWhenLastUsed_UTC() { return whenLastUsed; }
    
    // Returns local timestamp object
    public DateTime GetWhenLastUsed_local() 
        { return whenLastUsed.ToLocalTime(); }
    
    // Returns local timestamp as a formatted string
    public string GetWhenLastUsed_formatted()
    {
        // Show nothing if there is no timestamp yet
        if (whenLastUsed == DateTime.MinValue) return "";
        
        return whenLastUsed.ToLocalTime().ToString("yyyy-MM-dd HH:mm");
    }
}

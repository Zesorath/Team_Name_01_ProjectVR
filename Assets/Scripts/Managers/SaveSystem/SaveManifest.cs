using System;
using System.Collections.Generic;
using UnityEngine;

public class SaveManifest
{
    [NonSerialized]
    readonly SaveDebug d = 
        new SaveDebug("<color=#1E88E5>[SaveManifest] </color>");
    
    public string lastSave;
    public int lastSlotUsed = 0;

    SaveSlot[] slots =
    {
        new SaveSlot { slotID = 1 },
        new SaveSlot { slotID = 2 },
        new SaveSlot { slotID = 3 },
        new SaveSlot { slotID = 4 },
        new SaveSlot { slotID = 5 }
    };

}

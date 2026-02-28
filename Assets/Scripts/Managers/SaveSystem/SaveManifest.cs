using System.Collections.Generic;
using UnityEngine;

public class SaveManifest
{
    public string lastSave;
    public string lastSlotUsed;

    public SaveSlot[] slots =
    {
        new SaveSlot { id = 1 },
        new SaveSlot { id = 2 },
        new SaveSlot { id = 3 },
        new SaveSlot { id = 4 },
        new SaveSlot { id = 5 }
    };
}

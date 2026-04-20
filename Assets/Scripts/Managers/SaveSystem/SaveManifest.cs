using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class SaveManifest
{
    [SerializeField] int lastSlotUsed = 0;
    [SerializeField] SaveSlot[] slots;

    public SaveManifest()
    {
        // Create empty slots with default values
        slots = new SaveSlot[6];
        for (int i = 0; i < slots.Length; i++) slots[i] = new SaveSlot();
    }

    public void ActivateSaveSlot_empty(int slotNo, SaveData sd)
    {
        lastSlotUsed = slotNo;
        SaveSlot curr = GetActiveSlot();
        curr.MarkNotEmpty();
        curr.Capture_SaveFilePath(sd);
        curr.Capture_WhenLastUsed();
    }

    public void ActivateSaveSlot_occupied(int slotNo)
    {
        lastSlotUsed = slotNo;

        SaveSlot curr = GetActiveSlot();
        curr.Capture_WhenLastUsed();
    }

    public void ClearSaveSlot(int slotNo)
    {
        // Grab a reference to the slot
        SaveSlot curr = slots[slotNo-1];

        // Delete the save file
        SavePaths p = SaveManager.Instance.paths;
        p.DeleteSaveFile(curr.Get_FilePath());

        // Clear the slot
        curr.MakeEmpty();
    }

    public void SetActiveSlotNo(int slotNo) { lastSlotUsed = slotNo; }

    public int GetLastSlotNo() { return lastSlotUsed; }

    public SaveSlot GetActiveSlot() { return slots[lastSlotUsed-1]; }

    public bool SlotIsEmpty(int slotNo) { return slots[slotNo-1].isEmpty; }

    public string GetSlotDisplay(int slotNo) {
        return slots[slotNo-1].Get_Display();
    }

    public string GetSlotLastUsed(int slotNo) {
        return slots[slotNo-1].Get_WhenLastUsed_formatted();
    }
}

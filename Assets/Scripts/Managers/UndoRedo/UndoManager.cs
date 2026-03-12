using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UndoManager
{
    readonly UndoDebug d = 
        new UndoDebug("<color=#7E57C2>[UndoManager] </color>");
    
    public static UndoManager Instance { get; } = new UndoManager();
    SaveData currentState;
    Stack<SaveData> undoStack;
    Stack<SaveData> redoStack;
    bool isInitialized = false;

    UndoManager()
    {
        undoStack = new();
        redoStack = new();
    }

    // Call in Bootstrap immediately after SaveManager.Init, to ensure this
    // exists before anything else. Guard against double-initializing
    public void Init()
    {
        if (isInitialized) return;
        isInitialized = true;
    }

    public void Reset()
    {
        currentState = new SaveData(SaveManager.Instance.saveData);
        undoStack.Clear();
        redoStack.Clear();
    }

    public void Do()
    {
        undoStack.Push(currentState);
        currentState = new SaveData(SaveManager.Instance.saveData);
        redoStack.Clear();
    }

    public void Undo()
    {
        if (undoStack.Count == 0)
            { d.Warn("Undo stack empty. No changes restored."); return; }
        
        redoStack.Push(currentState);
        currentState = undoStack.Pop();
        SaveManager.Instance.LoadFrom_object(currentState);
    }

    public void Redo()
    {
        if (redoStack.Count == 0)
            { d.Warn("Redo stack empty. No changes restored."); return; }
        
        undoStack.Push(currentState);
        currentState = redoStack.Pop();
        SaveManager.Instance.LoadFrom_object(currentState);
    }
}

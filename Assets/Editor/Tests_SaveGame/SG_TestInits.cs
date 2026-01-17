using System;
using UnityEngine;

public static class SG_TestInits
{
    public static SaveManager Create_TestSaveManager()
    {
        GameObject go = new GameObject("SaveManager");
        SaveManager sm = go.AddComponent<SaveManager>();
        sm.TestInit_TypeCounters();

        return sm;
    }

    public static SaveableObject Create_TestSaveableObject(
        SaveManager sm, Boolean generateID = true)
    {
        GameObject go = new GameObject("Obj");
        SaveableObject so = go.AddComponent<SaveableObject>();
        if (generateID == true) {so.id = Guid.NewGuid();}

        return so;
    }
}

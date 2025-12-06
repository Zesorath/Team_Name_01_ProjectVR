using UnityEngine;

public static class SG_TestInits
{
    public static SaveManager Create_SaveManager()
    {
        GameObject go = new GameObject("SaveManager");
        SaveManager sm = go.AddComponent<SaveManager>();
        sm.TestInit_TypeCounters();

        return sm;
    }

    public static SaveableObject Create_SaveableObject(SaveManager sm, string n)
    {
        GameObject go = new GameObject("Obj");
        SaveableObject so = go.AddComponent<SaveableObject>();
        so.id = n;

        return so;
    }
}

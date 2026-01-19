using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WTC_S02_SaveGame_RegisterSaveable
{
    // [Test]
    // public void WTC_S02_01_DictionaryInitializesImplicitly()
    // {
    //     SaveManager sm = SG_TestInits.Create_TestSaveManager();
    //     PartID so = SG_TestInits.Create_TestSaveableObject(sm);

    //     sm.RegisterObjectState(so);

    //     // RegisterSaveable() created the empty dictionary
    //     Assert.IsNotNull(sm.saveData);

    //     // SaveableObject was added to the dictionary
    //     Assert.IsTrue(sm.saveData.ContainsKey(so.id));
    // }

    // [Test]
    // public void WTC_S02_02_RejectsEmptyID()
    // {
    //     SaveManager sm = SG_TestInits.Create_TestSaveManager();
    //     PartID so = SG_TestInits.Create_TestSaveableObject(sm, false);

    //     // Expect console warning that the unnamed object could not register
    //     LogAssert.Expect(LogType.Warning, 
    //         "SaveableObject Obj has empty id");
    //     sm.RegisterObjectState(so);
        
    //     // Dictionary initialized, but nothing added
    //     Assert.AreEqual(0, sm.saveData.Count);
    // }

    // [Test]
    // public void WTC_S02_03_RejectsDuplicateID()
    // {
    //     SaveManager sm = SG_TestInits.Create_TestSaveManager();
    //     PartID so1 = SG_TestInits.Create_TestSaveableObject(sm);

    //     // Create duplicate SaveableObject
    //     GameObject go = new GameObject("Duplicate SaveableObject");
    //     PartID so2 = go.AddComponent<PartID>();
    //     so2 = so1;

    //     sm.RegisterObjectState(so1);   // This line will succeed

    //     // Expect this to be the next console output of type Warning
    //     LogAssert.Expect(LogType.Warning, 
    //         $"SaveableObject with id {so1.id} already exists");
    //     sm.RegisterObjectState(so2);   // This line should fail and log failure

    //     // Only the first object registered
    //     Assert.AreEqual(1, sm.saveData.Count);
    // }

    // [Test]
    // public void WTC_S02_04_AcceptsValidID()
    // {
    //     SaveManager sm = SG_TestInits.Create_TestSaveManager();
    //     PartID so = SG_TestInits.Create_TestSaveableObject(sm);

    //     sm.RegisterObjectState(so);

    //     Assert.IsTrue(sm.saveData.ContainsKey(so.id));
    // }
}

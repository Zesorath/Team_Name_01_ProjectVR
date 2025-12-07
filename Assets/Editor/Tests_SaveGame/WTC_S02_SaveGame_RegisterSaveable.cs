using UnityEngine;
using NUnit.Framework;
using UnityEngine.TestTools;

public class WTC_S02_SaveGame_RegisterSaveable
{
    [Test]
    public void WTC_S02_01_DictionaryInitializesImplicitly()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        SaveableObject so = SG_TestInits.Create_SaveableObject(sm, "TEST_0");

        sm.RegisterSaveable(so);

        // RegisterSaveable() created the empty dictionary
        Assert.IsNotNull(sm.saveablesKeyed);

        // SaveableObject was added to the dictionary
        Assert.IsTrue(sm.saveablesKeyed.ContainsKey("TEST_0"));
    }

    [Test]
    public void WTC_S02_02_RejectsEmptyID()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        SaveableObject so = SG_TestInits.Create_SaveableObject(sm, "");

        // Expect console warning that the unnamed object could not register
        LogAssert.Expect(LogType.Warning, 
            "SaveableObject Obj has empty id--not saved");
        sm.RegisterSaveable(so);
        
        // Dictionary initialized, but nothing added
        Assert.AreEqual(0, sm.saveablesKeyed.Count);
    }

    [Test]
    public void WTC_S02_03_RejectsDuplicateID()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        SaveableObject so1 = SG_TestInits.Create_SaveableObject(sm, "LED_0");
        SaveableObject so2 = SG_TestInits.Create_SaveableObject(sm, "LED_0");

        sm.RegisterSaveable(so1);   // This line will succeed

        // Expect this to be the next console output of type Warning
        LogAssert.Expect(LogType.Warning, 
            "SaveableObject with id LED_0 already exists--not saved");
        sm.RegisterSaveable(so2);   // This line should fail and log failure

        // Only the first object registered
        Assert.AreEqual(1, sm.saveablesKeyed.Count);
    }

    [Test]
    public void WTC_S02_04_AcceptsValidID()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        SaveableObject so = SG_TestInits.Create_SaveableObject(sm, "LED_0");

        sm.RegisterSaveable(so);

        Assert.IsTrue(sm.saveablesKeyed.ContainsKey("LED_0"));
    }
}

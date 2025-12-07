using UnityEngine;
using NUnit.Framework;

public class WTC_S01_SaveGame_GenerateID
{
    [Test]
    public void WTC_S01_01_DifferentComponentTypesDontClobberCounters()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        sm.TestInit_SaveablesDict();

        Assert.AreEqual(
            "POWER_SOURCE_0", sm.GenerateID(SaveManager.Type.POWER_SOURCE)
        );
        Assert.AreEqual("RESISTOR_0", sm.GenerateID(SaveManager.Type.RESISTOR));
        Assert.AreEqual("LED_0", sm.GenerateID(SaveManager.Type.LED));
    }

    [Test]
    public void WTC_S01_02_SameComponentTypesIncrementCounters()
    {
        SaveManager sm = SG_TestInits.Create_SaveManager();
        sm.TestInit_SaveablesDict();

        Assert.AreEqual("RESISTOR_0", sm.GenerateID(SaveManager.Type.RESISTOR));
        Assert.AreEqual("RESISTOR_1", sm.GenerateID(SaveManager.Type.RESISTOR));
        Assert.AreEqual("RESISTOR_2", sm.GenerateID(SaveManager.Type.RESISTOR));
    }
}

using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DummyTest
{
    // For testing the framework in GitHub Actions
    [Test]
    public void TestsAreActive()
    {
        Debug.Log("*** EditMode tests active! ***");
        Assert.Pass("Passed!");
    }
}

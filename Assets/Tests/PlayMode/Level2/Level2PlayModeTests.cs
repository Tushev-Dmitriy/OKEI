using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;

public class Level2PlayModeTests
{
    [UnityTest]
    public IEnumerator Level2Scene_LoadsLockControlRuntime()
    {
        yield return SceneTestUtility.LoadScene("Level2");

        UnityEngine.Object system = SceneTestUtility.FindFirstGameplayObject("LockControlSystem");
        UnityEngine.Object inputs = SceneTestUtility.FindFirstGameplayObject("LockInputs");

        Assert.That(system, Is.Not.Null);
        Assert.That(inputs, Is.Not.Null);
        string saveId = (string)system.GetType().GetProperty("SaveId")!.GetValue(system);
        Assert.That(saveId, Is.Not.Null.And.Not.Empty);
    }
}

using NUnit.Framework;

public class BootstrapEditModeTests
{
    private System.Type _levelProgressManagerType;

    [SetUp]
    public void SetUp()
    {
        TestSaveFileUtility.DeleteBootstrapSave();
        _levelProgressManagerType = AssemblyTypeUtility.ResolveGameplayType("LevelProgressManager");
    }

    [TearDown]
    public void TearDown()
    {
        TestSaveFileUtility.DeleteBootstrapSave();
    }

    [Test]
    public void CompleteLevel_StoresCompletionAndUnlocksNextLevel()
    {
        TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "ResetProgress");

        TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "CompleteLevel", 1);

        Assert.That((bool)TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "IsLevelCompleted", 1), Is.True);
        Assert.That((bool)TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "IsLevelUnlocked", 2), Is.True);
        Assert.That((int)TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "GetMaxUnlockedLevel"), Is.EqualTo(2));
        Assert.That((int)TestReflectionUtility.InvokeStaticMethod(_levelProgressManagerType, "GetLastPlayedLevel"), Is.EqualTo(1));
    }
}

using NUnit.Framework;
using UnityEngine;

public class Level2EditModeTests
{
    [Test]
    public void LockInputs_RestoreStateAndInputFlag_UpdateStoredValues()
    {
        GameObject root = new GameObject("LockInputsTest");
        try
        {
            System.Type lockInputsType = AssemblyTypeUtility.ResolveGameplayType("LockInputs");
            Component inputs = root.AddComponent(lockInputsType);
            lockInputsType.GetMethod("RestoreState")?.Invoke(inputs, new object[] { false, false, false, false });

            Assert.That((bool)lockInputsType.GetProperty("PowerEnabled")!.GetValue(inputs), Is.False);
            Assert.That((bool)lockInputsType.GetProperty("CoolingEnabled")!.GetValue(inputs), Is.False);
            Assert.That((bool)lockInputsType.GetProperty("SafeModeEnabled")!.GetValue(inputs), Is.False);
            Assert.That((bool)lockInputsType.GetProperty("InputEnabled")!.GetValue(inputs), Is.False);

            lockInputsType.GetMethod("SetInputEnabled")?.Invoke(inputs, new object[] { true });
            lockInputsType.GetMethod("RestoreState")?.Invoke(inputs, new object[] { true, true, true, true });

            Assert.That((bool)lockInputsType.GetProperty("PowerEnabled")!.GetValue(inputs), Is.True);
            Assert.That((bool)lockInputsType.GetProperty("CoolingEnabled")!.GetValue(inputs), Is.True);
            Assert.That((bool)lockInputsType.GetProperty("SafeModeEnabled")!.GetValue(inputs), Is.True);
            Assert.That((bool)lockInputsType.GetProperty("InputEnabled")!.GetValue(inputs), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}

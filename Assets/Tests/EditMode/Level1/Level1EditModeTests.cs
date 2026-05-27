using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class Level1EditModeTests
{
    [Test]
    public void DoorCondition_UsesHighestClauseSlotToCalculateRequiredSlots()
    {
        GameObject root = new GameObject("DoorConditionTest");
        try
        {
            System.Type doorConditionType = AssemblyTypeUtility.ResolveGameplayType("DoorCondition");
            System.Type clauseType = AssemblyTypeUtility.ResolveGameplayType("DoorConditionClause");
            System.Type expressionType = AssemblyTypeUtility.ResolveGameplayType("DoorConditionExpression");
            System.Type logicalOperatorType = AssemblyTypeUtility.ResolveGameplayType("DoorLogicalOperator");
            System.Type clauseListType = typeof(List<>).MakeGenericType(clauseType);

            Component doorCondition = root.AddComponent(doorConditionType);
            object firstClause = System.Activator.CreateInstance(clauseType);
            object lastClause = System.Activator.CreateInstance(clauseType);
            TestReflectionUtility.SetPrivateField(firstClause, clauseType, "_slotIndex", 0);
            TestReflectionUtility.SetPrivateField(lastClause, clauseType, "_slotIndex", 2);

            object expression = System.Activator.CreateInstance(expressionType);
            object andOperator = System.Enum.Parse(logicalOperatorType, "And");
            object typedClauseList = System.Activator.CreateInstance(clauseListType);
            clauseListType.GetMethod("Add")!.Invoke(typedClauseList, new[] { firstClause });
            clauseListType.GetMethod("Add")!.Invoke(typedClauseList, new[] { lastClause });
            TestReflectionUtility.SetPrivateField(expression, expressionType, "_logic", andOperator);
            TestReflectionUtility.SetPrivateField(
                expression,
                expressionType,
                "_clauses",
                typedClauseList);

            TestReflectionUtility.SetPrivateField(doorCondition, doorConditionType, "_slotCount", 1);
            TestReflectionUtility.SetPrivateField(doorCondition, doorConditionType, "_condition", expression);

            int requiredSlots = (int)TestReflectionUtility.InvokeMethod(
                doorCondition,
                doorConditionType,
                "GetRequiredSlotCountFromCondition");

            Assert.That(requiredSlots, Is.EqualTo(3));
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }
}

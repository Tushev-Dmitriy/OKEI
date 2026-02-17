using System;
using UnityEngine;

[Serializable]
public class DoorConditionClause
{
    [SerializeField] private int _slotIndex;
    [SerializeField] private DoorComparisonOperator _operator = DoorComparisonOperator.Equal;
    [SerializeField] private DoorValueType _valueType = DoorValueType.String;
    [SerializeField] private string _expectedValue = string.Empty;

    public int SlotIndex => _slotIndex;
    public DoorComparisonOperator Operator => _operator;
    public DoorValueType ValueType => _valueType;
    public string ExpectedValue => _expectedValue;
}

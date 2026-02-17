using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DoorConditionExpression
{
    [SerializeField] private DoorLogicalOperator _logic = DoorLogicalOperator.And;
    [SerializeField] private List<DoorConditionClause> _clauses = new List<DoorConditionClause>();

    public DoorLogicalOperator Logic => _logic;
    public List<DoorConditionClause> Clauses => _clauses;
}

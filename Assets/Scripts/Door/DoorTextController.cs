using UnityEngine;
using UnityEngine.UI;
using System.Text;

public class DoorTextController : MonoBehaviour
{
    [SerializeField] Text _doorConditionText;
    [SerializeField] Text _consoleOutText;

    private void Start()
    {
        ClearText();
    }

    public void SetupConditionText(string itemName)
    {
        string _doorText =
        $@"if (<color=#00FF00>value = {itemName}</color>)
    {{
        Door.Open();
    }} else 
    {{
        Output.Error();
    }}";

        _doorConditionText.text = _doorText;
    }

    public void SetupConditionText(DoorConditionExpression expression)
    {
        string conditionText = BuildConditionText(expression);
        string doorText =
        $@"if (<color=#00FF00>{conditionText}</color>)
    {{
        Door.Open();
    }} else 
    {{
        Output.Error();
    }}";

        _doorConditionText.text = doorText;
    }
    
    public void SetupConsoleError()
    {
        _consoleOutText.text = @"<color=#FF0000>Console.WriteLine(""Error"");</color>)";
    }

    public void SetupConsoleSuccess()
    {
        _consoleOutText.text = @"<color=#00FF00>Console.WriteLine(""OK: condition passed"");</color>";
    }

    public void ClearText()
    {
        _consoleOutText.text = null;
    }

    private static string BuildConditionText(DoorConditionExpression expression)
    {
        if (expression == null || expression.Clauses == null || expression.Clauses.Count == 0)
        {
            return "false";
        }

        string joinOperator = expression.Logic == DoorLogicalOperator.And ? "&&" : "||";
        var stringBuilder = new StringBuilder();

        foreach (var clause in expression.Clauses)
        {
            if (clause == null)
            {
                continue;
            }

            if (stringBuilder.Length > 0)
            {
                stringBuilder.Append(" ").Append(joinOperator).Append(" ");
            }

            stringBuilder
                .Append("slot[")
                .Append(clause.SlotIndex)
                .Append("] ")
                .Append(MapOperator(clause.Operator))
                .Append(" ")
                .Append(FormatValue(clause));
        }

        return stringBuilder.Length == 0 ? "false" : stringBuilder.ToString();
    }

    private static string MapOperator(DoorComparisonOperator op)
    {
        switch (op)
        {
            case DoorComparisonOperator.Equal:
                return "==";
            case DoorComparisonOperator.NotEqual:
                return "!=";
            case DoorComparisonOperator.GreaterOrEqual:
                return ">=";
            case DoorComparisonOperator.LessOrEqual:
                return "<=";
            case DoorComparisonOperator.Greater:
                return ">";
            case DoorComparisonOperator.Less:
                return "<";
            default:
                return "==";
        }
    }

    private static string FormatValue(DoorConditionClause clause)
    {
        if (clause.ValueType == DoorValueType.Number)
        {
            return clause.ExpectedValue;
        }

        return "\"" + clause.ExpectedValue + "\"";
    }
}

using UnityEngine;
using UnityEngine.UI;

public class DoorTextController : MonoBehaviour
{
    [SerializeField] Text _doorConditionText;
    [SerializeField] Text _consoleOutText;
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
    
    public void SetupConsoleError()
    {
        _consoleOutText.text = @"<color=#FF0000>Console.WriteLine(""Error"");</color>)";
    }
}

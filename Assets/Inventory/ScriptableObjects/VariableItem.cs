using UnityEngine;

[CreateAssetMenu(menuName = "Items/Variable Item")]
public class VariableItem : ScriptableObject 
{
    public VariableType type;
    public string value;
    public Color displayColor;
}

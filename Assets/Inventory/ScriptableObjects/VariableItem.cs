using UnityEngine;

[CreateAssetMenu(menuName = "Items/Variable Item")]
public class VariableItem : Item
{
    public VariableType type;
    public string value;
    public Color displayColor;
    public GameObject objectPrefab;
}

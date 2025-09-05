using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VariableItemSpawn : MonoBehaviour
{
    public void Spawn(VariableItem variableItem)
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = variableItem.displayColor;
        }

        TextMeshPro objTextData = gameObject.transform.GetChild(1).GetComponent<TextMeshPro>();
        objTextData.color = variableItem.displayColor;
        objTextData.text = $"{(variableItem.type).ToString().ToLower()} name = {variableItem.value}; " +
            $"{variableItem.description}";

        for (int i = 2; i < gameObject.transform.childCount; i++)
        {
            TextMeshPro textMesh = gameObject.transform.GetChild(i).GetComponent<TextMeshPro>();
            textMesh.text = variableItem.value;
        }
    }
}

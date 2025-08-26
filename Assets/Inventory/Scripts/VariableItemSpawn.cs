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

        TextMeshPro textMesh = gameObject.GetComponentInChildren<TextMeshPro>();
        textMesh.text = variableItem.value;
    }
}

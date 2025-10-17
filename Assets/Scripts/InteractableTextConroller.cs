    using NUnit.Framework;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractableTextConroller : MonoBehaviour
{
    [TextArea(5, 10)]
    [SerializeField] private List<string> _conditionText = new();

    private TMP_Text _conditionTextComponent;
    private bool _conditionWork = true;

    private void Awake()
    {
        _conditionTextComponent = GetComponent<TMP_Text>();
        SwapText();
    }

    public void SwapText()
    {
        if (_conditionWork)
        {
            _conditionWork = false;
            _conditionTextComponent.text = _conditionText[0];
        } else
        {
            _conditionWork = true;
            _conditionTextComponent.text = _conditionText[1];
        }
    }
}

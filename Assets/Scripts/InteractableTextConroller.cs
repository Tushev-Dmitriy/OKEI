using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InteractableTextConroller : MonoBehaviour
{
    [TextArea(5, 10)]
    [SerializeField] private List<string> _conditionText = new();

    private TMP_Text _conditionTextComponent;
    private bool _conditionWork = true;

    [Header("For-cycle settings")]
    public bool useCounter;
    [SerializeField] private int _targetCount;
    private int _currentCount = 0;

    private void Awake()
    {
        _conditionTextComponent = GetComponent<TMP_Text>();
        SwapText();
    }

    public void SwapText()
    {
        if (useCounter)
        {
            UpdateCounterText();
            return;
        }

        if (_conditionWork)
        {
            _conditionWork = false;
            _conditionTextComponent.text = _conditionText[0];
        }
        else
        {
            _conditionWork = true;
            _conditionTextComponent.text = _conditionText[1];
        }
    }

    public void IncrementCounter()
    {
        if (!useCounter) return;

        _currentCount++;
        _currentCount = Mathf.Clamp(_currentCount, 0, _targetCount);

        UpdateCounterText();
    }

    private void UpdateCounterText()
    {
        if (_conditionText.Count > 0)
        {
            string template = _conditionText[0];
            _conditionTextComponent.text = string.Format(template, _currentCount, _targetCount);
        }
        else
        {
            _conditionTextComponent.text = $"Нажато {_currentCount}/{_targetCount}";
        }
    }

    public bool IsCompleted => useCounter && _currentCount >= _targetCount;
}

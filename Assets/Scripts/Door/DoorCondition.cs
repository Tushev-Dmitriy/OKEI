using System;
using System.Collections.Generic;
using DevionGames.InventorySystem;
using DG.Tweening;
using UnityEngine;

public class DoorCondition : MonoBehaviour
{
    [SerializeField] private Door _door;
    [SerializeField] private GameObject _doorUI;
    [SerializeField] private DoorTextController _doorTextController;
    [SerializeField] private ItemCollection _doorItemCollection;
    [SerializeField] private List<ItemCollection> _slotCollections = new List<ItemCollection>();
    [SerializeField] private DoorConditionExpression _condition = new DoorConditionExpression();
    [SerializeField] private int _slotCount = 1;
    [SerializeField] private Vector2 _slotOffset = new Vector2(0f, -160f);
    [SerializeField] private bool _ignoreCase = true;

    private GameObject _slotUiObj;
    private GameObject _textUiObj;
    private readonly List<GameObject> _slotUiObjects = new List<GameObject>();
    private bool _isOpen;

    private void Awake()
    {
        _slotUiObj = _doorUI.transform.GetChild(1).gameObject;
        _textUiObj = _doorUI.transform.GetChild(0).gameObject;
        _slotUiObjects.Clear();
        _slotUiObjects.Add(_slotUiObj);
        EnsureSlotCollections();
        ApplySlotVisibility();
    }

    private void OnEnable()
    {
        SyncOpenState();
        SubscribeToSlotEvents();
    }

    private void OnDisable()
    {
        UnsubscribeFromSlotEvents();
    }

    private void EnsureSlotCollections()
    {
        if (_slotCollections.Count > 0)
        {
            return;
        }

        if (_doorItemCollection != null)
        {
            _slotCollections.Add(_doorItemCollection);
            EnsureSlotInstances();
            return;
        }

        var slotCollectionOnUi = _slotUiObj.GetComponent<ItemCollection>();
        if (slotCollectionOnUi != null)
        {
            _slotCollections.Add(slotCollectionOnUi);
            EnsureSlotInstances();
        }
    }

    private void EnsureSlotInstances()
    {
        if (_slotCount <= 1)
        {
            return;
        }

        var baseRect = _slotUiObj.GetComponent<RectTransform>();
        if (baseRect == null)
        {
            return;
        }

        for (int i = _slotCollections.Count; i < _slotCount; i++)
        {
            var clone = Instantiate(_slotUiObj, _slotUiObj.transform.parent);
            clone.name = $"{_slotUiObj.name} ({i})";

            var rect = clone.GetComponent<RectTransform>();
            rect.anchoredPosition = baseRect.anchoredPosition + _slotOffset * i;

            var collection = clone.GetComponent<ItemCollection>();
            if (collection != null)
            {
                _slotCollections.Add(collection);
                _slotUiObjects.Add(clone);
            }
        }
    }

    private int GetRequiredSlotCountFromCondition()
    {
        if (_condition == null || _condition.Clauses == null || _condition.Clauses.Count == 0)
        {
            return 1;
        }

        int maxIndex = 0;
        foreach (var clause in _condition.Clauses)
        {
            if (clause == null)
            {
                continue;
            }

            if (clause.SlotIndex > maxIndex)
            {
                maxIndex = clause.SlotIndex;
            }
        }

        return maxIndex + 1;
    }

    private void ApplySlotVisibility()
    {
        int requiredSlots = GetRequiredSlotCountFromCondition();
        for (int i = 0; i < _slotUiObjects.Count; i++)
        {
            if (_slotUiObjects[i] == null)
            {
                continue;
            }

            _slotUiObjects[i].SetActive(i < requiredSlots);
        }
    }

    private void SubscribeToSlotEvents()
    {
        foreach (var slotCollection in _slotCollections)
        {
            if (slotCollection == null)
            {
                continue;
            }

            slotCollection.onItemAdded.AddListener(OnItemAddedToSlot);
            slotCollection.onItemRemoved.AddListener(OnItemRemovedFromSlot);
        }
    }

    private void UnsubscribeFromSlotEvents()
    {
        foreach (var slotCollection in _slotCollections)
        {
            if (slotCollection == null)
            {
                continue;
            }

            slotCollection.onItemAdded.RemoveListener(OnItemAddedToSlot);
            slotCollection.onItemRemoved.RemoveListener(OnItemRemovedFromSlot);
        }
    }

    public void OnItemAddedToSlot()
    {
        TryOpenDoorFromCondition(true);
    }

    private void OnItemRemovedFromSlot()
    {
        _doorTextController.ClearText();
    }

    private bool EvaluateCondition()
    {
        if (_condition == null || _condition.Clauses == null || _condition.Clauses.Count == 0)
        {
            return false;
        }

        bool result = _condition.Logic == DoorLogicalOperator.And;
        foreach (var clause in _condition.Clauses)
        {
            bool clauseResult = EvaluateClause(clause);
            if (_condition.Logic == DoorLogicalOperator.And)
            {
                result &= clauseResult;
                if (!result)
                {
                    return false;
                }
            }
            else
            {
                result |= clauseResult;
                if (result)
                {
                    return true;
                }
            }
        }

        return result;
    }

    private bool EvaluateClause(DoorConditionClause clause)
    {
        if (clause == null)
        {
            return false;
        }

        if (clause.SlotIndex < 0 || clause.SlotIndex >= _slotCollections.Count)
        {
            return false;
        }

        var slotCollection = _slotCollections[clause.SlotIndex];
        if (slotCollection == null)
        {
            return false;
        }

        var items = slotCollection.GetItemsInCollection();
        if (items == null || items.Count == 0 || items[0] == null)
        {
            return false;
        }

        string actualItemName = ResolveItemValue(items[0]);
        if (clause.ValueType == DoorValueType.String)
        {
            return CompareStrings(actualItemName, clause.Operator, clause.ExpectedValue);
        }

        if (!int.TryParse(actualItemName, out int actualNumber))
        {
            return false;
        }

        if (!int.TryParse(clause.ExpectedValue, out int expectedNumber))
        {
            return false;
        }

        return CompareNumbers(actualNumber, clause.Operator, expectedNumber);
    }

    private static string NormalizeItemName(string itemName)
    {
        return itemName.Replace("(Clone)", string.Empty).Trim();
    }

    private string ResolveItemValue(DevionGames.InventorySystem.Item item)
    {
        if (item == null)
        {
            return string.Empty;
        }

        // Prefer VariableItemSpawn data from prefab/override prefab.
        var prefab = item.OverridePrefab != null ? item.OverridePrefab : item.Prefab;
        if (prefab != null)
        {
            var variable = prefab.GetComponent<VariableItemSpawn>();
            if (variable != null && variable.VariableItemData != null)
            {
                return variable.VariableItemData.value;
            }
        }

        if (!string.IsNullOrWhiteSpace(item.DisplayName))
        {
            return item.DisplayName;
        }

        if (!string.IsNullOrWhiteSpace(item.Name))
        {
            return item.Name;
        }

        return NormalizeItemName(item.name);
    }

    private bool CompareStrings(string actual, DoorComparisonOperator op, string expected)
    {
        actual = NormalizeConditionString(actual);
        expected = NormalizeConditionString(expected);
        var comparison = _ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        switch (op)
        {
            case DoorComparisonOperator.Equal:
                return string.Equals(actual, expected, comparison);
            case DoorComparisonOperator.NotEqual:
                return !string.Equals(actual, expected, comparison);
            default:
                return false;
        }
    }

    private static string NormalizeConditionString(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        value = value.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2);
        }

        return value.Trim();
    }

    private static bool CompareNumbers(int actual, DoorComparisonOperator op, int expected)
    {
        switch (op)
        {
            case DoorComparisonOperator.Equal:
                return actual == expected;
            case DoorComparisonOperator.NotEqual:
                return actual != expected;
            case DoorComparisonOperator.GreaterOrEqual:
                return actual >= expected;
            case DoorComparisonOperator.LessOrEqual:
                return actual <= expected;
            case DoorComparisonOperator.Greater:
                return actual > expected;
            case DoorComparisonOperator.Less:
                return actual < expected;
            default:
                return false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _slotUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        _textUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        ApplySlotVisibility();

        for (int i = 1; i < _slotUiObjects.Count; i++)
        {
            if (_slotUiObjects[i] == null || !_slotUiObjects[i].activeSelf)
            {
                continue;
            }

            _slotUiObjects[i].transform.localScale = Vector3.one;
        }

        _doorTextController.SetupConditionText(_condition);
        _doorTextController.ClearText();
        TryOpenDoorFromCondition(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        _slotUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);
        _textUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);

        for (int i = 1; i < _slotUiObjects.Count; i++)
        {
            if (_slotUiObjects[i] == null)
            {
                continue;
            }

            _slotUiObjects[i].transform.localScale = Vector3.zero;
        }

        _doorTextController.ClearText();
    }

    private void SyncOpenState()
    {
        _isOpen = _door != null && _door.IsOpen;
    }

    private bool TryOpenDoorFromCondition(bool showErrorOnFail)
    {
        SyncOpenState();
        if (_isOpen)
        {
            return true;
        }

        if (!EvaluateCondition())
        {
            if (showErrorOnFail)
            {
                _doorTextController.SetupConsoleError();
            }
            return false;
        }

        _door.SetOpen(true);
        _isOpen = true;
        ConsumeItemsInSlots();
        _doorTextController.SetupConsoleSuccess();
        UnsubscribeFromSlotEvents();
        return true;
    }

    private void ConsumeItemsInSlots()
    {
        foreach (var slotCollection in _slotCollections)
        {
            if (slotCollection == null)
            {
                continue;
            }

            var items = slotCollection.GetItemsInCollection();
            if (items == null || items.Count == 0)
            {
                continue;
            }

            slotCollection.Remove(items[0]);
            var itemSlot = slotCollection.GetComponentInChildren<ItemSlot>();
            if (itemSlot != null)
            {
                itemSlot.ClearSlot();
            }
        }
    }

}

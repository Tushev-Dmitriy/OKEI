using System;
using System.Collections.Generic;
using System.Globalization;
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
    [SerializeField] private Vector2 _slotOffset = new Vector2(170f, 0f);
    [SerializeField] private bool _ignoreCase = true;

    private GameObject _slotTemplate;
    private GameObject _textTemplate;
    private RectTransform _runtimeUiRoot;
    private DoorTextController _runtimeDoorTextController;
    private readonly List<GameObject> _slotUiObjects = new List<GameObject>();
    private readonly List<ItemCollection> _runtimeSlotCollections = new List<ItemCollection>();
    private bool _isOpen;
    private bool _slotEventsSubscribed;

    private void Awake()
    {
        EnsureDoorUiRootReady();
        CacheTemplates();
        HideTemplateUi();
        SyncOpenState();
    }

    private void OnEnable()
    {
        EnsureDoorUiRootReady();
        SyncOpenState();

        if (_isOpen)
        {
            HideRuntimeUi(false);
            UnsubscribeFromSlotEvents();
        }
    }

    private void OnDisable()
    {
        HideRuntimeUi(false);
        UnsubscribeFromSlotEvents();
    }

    private void OnDestroy()
    {
        if (_runtimeUiRoot != null)
        {
            Destroy(_runtimeUiRoot.gameObject);
        }
    }

    private void EnsureDoorUiRootReady()
    {
        if (_doorUI == null)
        {
            return;
        }

        if (!_doorUI.activeSelf)
        {
            _doorUI.SetActive(true);
        }

        if (_doorUI.transform.localScale == Vector3.zero)
        {
            _doorUI.transform.localScale = Vector3.one;
        }
    }

    private void CacheTemplates()
    {
        if (_doorUI == null)
        {
            return;
        }

        if (_textTemplate == null)
        {
            _textTemplate = _doorTextController != null
                ? _doorTextController.gameObject
                : _doorUI.transform.GetChild(0).gameObject;
        }

        if (_slotTemplate == null && _doorUI.transform.childCount > 1)
        {
            _slotTemplate = _doorUI.transform.GetChild(1).gameObject;
        }
    }

    private void HideTemplateUi()
    {
        if (_textTemplate != null)
        {
            _textTemplate.SetActive(false);
            _textTemplate.transform.localScale = Vector3.zero;
        }

        if (_slotTemplate != null)
        {
            _slotTemplate.SetActive(false);
            _slotTemplate.transform.localScale = Vector3.zero;
        }
    }

    private void EnsureRuntimeUi()
    {
        if (_runtimeUiRoot == null)
        {
            var rootObject = new GameObject($"DoorRuntimeUI_{name}", typeof(RectTransform));
            _runtimeUiRoot = rootObject.GetComponent<RectTransform>();
            _runtimeUiRoot.SetParent(_doorUI.transform, false);
            _runtimeUiRoot.anchorMin = Vector2.zero;
            _runtimeUiRoot.anchorMax = Vector2.one;
            _runtimeUiRoot.offsetMin = Vector2.zero;
            _runtimeUiRoot.offsetMax = Vector2.zero;
            _runtimeUiRoot.localScale = Vector3.one;
        }

        if (_runtimeDoorTextController == null && _textTemplate != null)
        {
            var textInstance = Instantiate(_textTemplate, _runtimeUiRoot, false);
            textInstance.name = $"{_textTemplate.name}_{name}";
            textInstance.SetActive(true);
            textInstance.transform.localScale = Vector3.zero;
            _runtimeDoorTextController = textInstance.GetComponent<DoorTextController>();
        }

        EnsureSlotsForCondition();
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

    private void EnsureSlotsForCondition()
    {
        if (_slotTemplate == null || _runtimeUiRoot == null)
        {
            return;
        }

        int requiredSlots = Mathf.Max(1, GetRequiredSlotCountFromCondition());
        _slotCount = Mathf.Max(_slotCount, requiredSlots);

        var templateRect = _slotTemplate.GetComponent<RectTransform>();
        if (templateRect == null)
        {
            return;
        }

        for (int i = _slotUiObjects.Count; i < requiredSlots; i++)
        {
            var slotObject = Instantiate(_slotTemplate, _runtimeUiRoot, false);
            slotObject.name = $"{_slotTemplate.name}_{name}_{i}";
            slotObject.SetActive(true);

            var rect = slotObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchorMin = templateRect.anchorMin;
                rect.anchorMax = templateRect.anchorMax;
                rect.pivot = templateRect.pivot;
                rect.anchoredPosition = templateRect.anchoredPosition + _slotOffset * i;
                rect.sizeDelta = templateRect.sizeDelta;
                rect.localScale = Vector3.zero;
            }

            _slotUiObjects.Add(slotObject);

            var collection = slotObject.GetComponent<ItemCollection>();
            if (collection != null)
            {
                _runtimeSlotCollections.Add(collection);
            }
        }

        ApplySlotVisibility();
    }

    private void ApplySlotVisibility()
    {
        int requiredSlots = Mathf.Max(1, GetRequiredSlotCountFromCondition());
        for (int i = 0; i < _slotUiObjects.Count; i++)
        {
            if (_slotUiObjects[i] == null)
            {
                continue;
            }

            bool shouldBeActive = i < requiredSlots;
            _slotUiObjects[i].SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                _slotUiObjects[i].transform.localScale = Vector3.zero;
            }
        }
    }

    private void SubscribeToSlotEvents()
    {
        if (_slotEventsSubscribed)
        {
            return;
        }

        foreach (var slotCollection in _runtimeSlotCollections)
        {
            if (slotCollection == null)
            {
                continue;
            }

            slotCollection.onItemAdded.AddListener(OnItemAddedToSlot);
            slotCollection.onItemRemoved.AddListener(OnItemRemovedFromSlot);
        }

        _slotEventsSubscribed = true;
    }

    private void UnsubscribeFromSlotEvents()
    {
        if (!_slotEventsSubscribed)
        {
            return;
        }

        foreach (var slotCollection in _runtimeSlotCollections)
        {
            if (slotCollection == null)
            {
                continue;
            }

            slotCollection.onItemAdded.RemoveListener(OnItemAddedToSlot);
            slotCollection.onItemRemoved.RemoveListener(OnItemRemovedFromSlot);
        }

        _slotEventsSubscribed = false;
    }

    public void OnItemAddedToSlot()
    {
        TryOpenDoorFromCondition(true);
    }

    private void OnItemRemovedFromSlot()
    {
        var textController = GetDoorTextController();
        if (textController != null)
        {
            textController.ClearText();
        }
    }

    private bool EvaluateCondition()
    {
        if (_condition == null || _condition.Clauses == null || _condition.Clauses.Count == 0)
        {
            return false;
        }

        if (!AreRequiredSlotsFilled())
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

    private bool AreRequiredSlotsFilled()
    {
        int requiredSlots = GetRequiredSlotCountFromCondition();
        if (requiredSlots <= 0)
        {
            return false;
        }

        for (int i = 0; i < requiredSlots && i < _runtimeSlotCollections.Count; i++)
        {
            var slotCollection = _runtimeSlotCollections[i];
            if (slotCollection == null)
            {
                return false;
            }

            var items = slotCollection.GetItemsInCollection();
            if (items == null || items.Count == 0 || items[0] == null)
            {
                return false;
            }
        }

        return true;
    }

    private bool EvaluateClause(DoorConditionClause clause)
    {
        if (clause == null)
        {
            return false;
        }

        if (clause.SlotIndex < 0 || clause.SlotIndex >= _runtimeSlotCollections.Count)
        {
            return false;
        }

        var slotCollection = _runtimeSlotCollections[clause.SlotIndex];
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

        if (!double.TryParse(actualItemName, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double actualNumber))
        {
            return false;
        }

        if (!double.TryParse(clause.ExpectedValue, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double expectedNumber))
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

    private static bool CompareNumbers(double actual, DoorComparisonOperator op, double expected)
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

        SyncOpenState();
        if (_isOpen)
        {
            HideRuntimeUi(false);
            return;
        }

        EnsureRuntimeUi();
        ApplySlotVisibility();
        SubscribeToSlotEvents();
        ShowRuntimeUi();

        var textController = GetDoorTextController();
        if (textController != null)
        {
            textController.SetupConditionText(_condition);
            textController.ClearText();
        }

        TryOpenDoorFromCondition(false);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        HideRuntimeUi(true);

        var textController = GetDoorTextController();
        if (textController != null)
        {
            textController.ClearText();
        }
    }

    private void ShowRuntimeUi()
    {
        if (_runtimeDoorTextController != null)
        {
            _runtimeDoorTextController.gameObject.SetActive(true);
            _runtimeDoorTextController.transform.DOKill();
            _runtimeDoorTextController.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }

        for (int i = 0; i < _slotUiObjects.Count; i++)
        {
            var slotObject = _slotUiObjects[i];
            if (slotObject == null || !slotObject.activeSelf)
            {
                continue;
            }

            slotObject.transform.DOKill();
            slotObject.transform.DOScale(Vector3.one, 0.35f).SetEase(Ease.OutBack);
        }
    }

    private void HideRuntimeUi(bool animate)
    {
        if (_runtimeDoorTextController != null)
        {
            HideUiObject(_runtimeDoorTextController.gameObject, animate);
        }

        for (int i = 0; i < _slotUiObjects.Count; i++)
        {
            if (_slotUiObjects[i] == null)
            {
                continue;
            }

            HideUiObject(_slotUiObjects[i], animate);
        }
    }

    private static void HideUiObject(GameObject target, bool animate)
    {
        if (target == null)
        {
            return;
        }

        target.transform.DOKill();
        if (animate)
        {
            target.transform.DOScale(Vector3.zero, 0.2f).SetEase(Ease.InBack);
        }
        else
        {
            target.transform.localScale = Vector3.zero;
        }
    }

    private void SyncOpenState()
    {
        _isOpen = _door != null && _door.IsOpen;
    }

    private DoorTextController GetDoorTextController()
    {
        return _runtimeDoorTextController != null ? _runtimeDoorTextController : _doorTextController;
    }

    private bool TryOpenDoorFromCondition(bool showErrorOnFail)
    {
        SyncOpenState();
        if (_isOpen)
        {
            HideRuntimeUi(false);
            return true;
        }

        if (!EvaluateCondition())
        {
            if (showErrorOnFail)
            {
                var textController = GetDoorTextController();
                if (textController != null)
                {
                    textController.SetupConsoleError();
                }
            }
            return false;
        }

        if (_door != null)
        {
            _door.SetOpen(true);
        }

        _isOpen = true;
        ConsumeItemsInSlots();

        var successController = GetDoorTextController();
        if (successController != null)
        {
            successController.SetupConsoleSuccess();
        }

        UnsubscribeFromSlotEvents();
        return true;
    }

    private void ConsumeItemsInSlots()
    {
        foreach (var slotCollection in _runtimeSlotCollections)
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

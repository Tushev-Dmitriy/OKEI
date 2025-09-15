using DevionGames.InventorySystem;
using DG.Tweening;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class DoorCondition : MonoBehaviour
{
    [SerializeField] DoorOpener _doorOpenerScript;
    [SerializeField] GameObject _slotUiObj;
    [SerializeField] ItemDatabase _itemDatabase;
    [SerializeField] ItemCollection _itemCollection;

    DevionGames.InventorySystem.Item _itemForCondition;

    private List<DevionGames.InventorySystem.Item> _variableItems = new List<DevionGames.InventorySystem.Item>();

    private void Awake()
    {
        var _tempItems = _itemDatabase.items;
        foreach (var item in _tempItems)
        {
            if (item.Category == _itemDatabase.categories[0])
            {
                _variableItems.Add(item);
            }
        }
        ConditionCreate();
        _itemCollection.onItemAdded.AddListener(CheckItemInSlot);
    }

    public void CheckItemInSlot()
    {
        Debug.Log(1);
        ItemCollection _doorObjects = _slotUiObj.GetComponent<ItemCollection>();
        Debug.Log(_itemForCondition);
        if (_doorObjects.GetItemsInCollection()[0] == _itemForCondition)
        {
            Debug.Log(1);
        } else
        {
            Debug.Log(2);
        }
    }

    private void ConditionCreate()
    {
        int _rndNum = Random.Range(0, _variableItems.Count + 1);
        _itemForCondition = _variableItems[_rndNum];
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);
        }
    }
}

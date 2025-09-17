using DevionGames.InventorySystem;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class DoorCondition : MonoBehaviour
{
    [SerializeField] DoorOpener _doorOpenerScript;
    [SerializeField] GameObject _doorUI;
    [SerializeField] ItemDatabase _itemDatabase;
    [SerializeField] ItemCollection _doorItemCollection;
    [SerializeField] ItemCollection _mainInventoryCollection;
    [SerializeField] DoorTextController _doorTextController;

    private GameObject _slotUiObj;
    private GameObject _textUiObj;

    private string _itemForCondition;
    private bool _isOpen = false;

    private List<DevionGames.InventorySystem.Item> _variableItems = new List<DevionGames.InventorySystem.Item>();

    private void Awake()
    {
        _slotUiObj = _doorUI.transform.GetChild(1).gameObject;
        _textUiObj = _doorUI.transform.GetChild(0).gameObject;

        var _tempItems = _itemDatabase.items;
        foreach (var item in _tempItems)
        {
            if (item.Category == _itemDatabase.categories[0])
            {
                _variableItems.Add(item);
            }   
        }
        ConditionCreate();
        _doorItemCollection.onItemAdded.AddListener(CheckItemInSlot);
    }

    public void CheckItemInSlot()
    {
        ItemCollection _doorObjects = _slotUiObj.GetComponent<ItemCollection>();
        string itemInSlot = _doorObjects.GetItemsInCollection()[0].name.Replace("(Clone)", "");
        _doorItemCollection.onItemRemoved.AddListener(RemoveItemInSlot);
        if (itemInSlot == _itemForCondition)
        {
            _doorOpenerScript.OpenDoors();
            _doorItemCollection.onItemAdded.RemoveListener(CheckItemInSlot);
            _isOpen = true;
        } else
        {
            _doorTextController.SetupConsoleError();
        }
    }

    private void RemoveItemInSlot()
    {
        _doorTextController.ClearText();
        _doorItemCollection.onItemRemoved.RemoveListener(RemoveItemInSlot);
    }

    private void BackItemToInventory()
    {
        _doorItemCollection.Clear();
    }

    private void ConditionCreate()
    {
        int _rndNum = Random.Range(0, _variableItems.Count + 1);
        _itemForCondition = _variableItems[_rndNum].name;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
            _textUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
            _doorTextController.SetupConditionText(_itemForCondition);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);
            _textUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);
            _doorTextController.ClearText();
            if (_isOpen)
            {
                BackItemToInventory();
            }
        }
    }
}

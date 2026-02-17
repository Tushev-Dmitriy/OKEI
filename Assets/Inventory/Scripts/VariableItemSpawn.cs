using DevionGames.InventorySystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Globalization;

public class VariableItemSpawn : MonoBehaviour, ISceneSaveable
{
    [SerializeField] private VariableItem _variableItem;
    [SerializeField] private string _saveId;
    [SerializeField] private SceneObjectType _objectType = SceneObjectType.VariableItem;

    public VariableItem VariableItemData => _variableItem;
    public string SaveId => _saveId;

    private ItemCollection _itemCollection;
    private Renderer _renderer;
    private Collider _collider;
    private bool _collected;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(_saveId))
        {
            _saveId = BuildRuntimeSaveId();
        }

        _itemCollection = GetComponent<ItemCollection>();
        _renderer = GetComponent<Renderer>();
        _collider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (_itemCollection != null)
        {
            _itemCollection.onChange.AddListener(OnItemCollectionChanged);
        }

        Spawn();
        SyncCollectedFromCollection();
        ApplyCollectedState();
    }

    private void Update()
    {
        SyncCollectedFromCollection();
    }

    private void OnDisable()
    {
        if (_itemCollection != null)
        {
            _itemCollection.onChange.RemoveListener(OnItemCollectionChanged);
        }
    }

    public void Spawn()
    {
        Renderer renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = _variableItem.displayColor;
        }

        TextMeshPro objTextData = gameObject.transform.GetChild(1).GetComponent<TextMeshPro>();
        objTextData.color = _variableItem.displayColor;
        objTextData.text = $"{(_variableItem.type).ToString().ToLower()} name = {_variableItem.value};";

        for (int i = 2; i < gameObject.transform.childCount; i++)
        {
            TextMeshPro textMesh = gameObject.transform.GetChild(i).GetComponent<TextMeshPro>();
            textMesh.text = _variableItem.value;
        }
    }

    private void OnItemCollectionChanged()
    {
        SyncCollectedFromCollection();
    }

    private void ApplyCollectedState()
    {
        bool visible = !_collected;

        if (_renderer != null)
        {
            _renderer.enabled = visible;
        }

        if (_collider != null)
        {
            _collider.enabled = visible;
        }

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            child.SetActive(visible);
        }
    }

    public SceneObjectStateData CaptureState()
    {
        if (_itemCollection != null && _itemCollection.IsEmpty)
        {
            _collected = true;
        }

        return new SceneObjectStateData
        {
            id = _saveId,
            type = _objectType,
            state = _collected ? 1 : 0
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        _collected = data != null && data.state == 1;
        ApplyCollectedState();
    }

    private string BuildRuntimeSaveId()
    {
        Vector3 p = transform.position;
        string px = p.x.ToString("F2", CultureInfo.InvariantCulture);
        string py = p.y.ToString("F2", CultureInfo.InvariantCulture);
        string pz = p.z.ToString("F2", CultureInfo.InvariantCulture);
        string value = _variableItem != null ? _variableItem.value : "null";
        string type = _variableItem != null ? _variableItem.type.ToString() : "null";
        return $"{SceneManager.GetActiveScene().name}::VariableItem::{type}::{value}::{px}:{py}:{pz}";
    }

    private void SaveNow()
    {
        var saver = Object.FindFirstObjectByType<PlayerSaver>();
        if (saver != null)
        {
            saver.SavePlayerData();
        }
    }

    private void SyncCollectedFromCollection()
    {
        if (_collected || _itemCollection == null || !_itemCollection.IsEmpty)
        {
            return;
        }

        _collected = true;
        ApplyCollectedState();
        SaveNow();
    }
}

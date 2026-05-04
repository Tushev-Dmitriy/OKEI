using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Level3Artifact : MonoBehaviour, ISceneSaveable
{
    [Header("Links")]
    [SerializeField] private Level3ArtifactManager _artifactManager;
    [SerializeField] private string _saveId;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private Light _roomDoorLight;
    [SerializeField] private Color _collectedLightColor = Color.green;

    [Header("Pickup")]
    [SerializeField] private GameObject _objectToDisableOnPickup;
    [SerializeField] private bool _disableTriggerAfterPickup = true;

    private Collider _triggerCollider;
    private bool _isCollected;
    private Color _defaultLightColor;

    public bool IsCollected => _isCollected;
    public string SaveId => string.IsNullOrWhiteSpace(_saveId) ? BuildFallbackSaveId() : _saveId;

    private void Awake()
    {
        _triggerCollider = GetComponent<Collider>();
        if (_triggerCollider != null)
        {
            _triggerCollider.isTrigger = true;
        }

        if (_artifactManager == null)
        {
            _artifactManager = FindFirstObjectByType<Level3ArtifactManager>();
        }

        if (_objectToDisableOnPickup == null)
        {
            _objectToDisableOnPickup = gameObject;
        }

        if (_roomDoorLight != null)
        {
            _defaultLightColor = _roomDoorLight.color;
        }
    }

    private void Reset()
    {
        var colliderComponent = GetComponent<Collider>();
        if (colliderComponent != null)
        {
            colliderComponent.isTrigger = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isCollected || !other.CompareTag(_playerTag))
        {
            return;
        }

        if (_artifactManager == null)
        {
            Debug.LogWarning(
                $"{nameof(Level3Artifact)} on {name} has no {nameof(Level3ArtifactManager)} reference.",
                this);
            return;
        }

        MarkAsCollected();
        _artifactManager.NotifyArtifactCollected(this);
        GameplaySaveManager.SaveCurrentGame();
    }

    public void DebugCollect()
    {
        if (_isCollected)
        {
            return;
        }

        MarkAsCollected();
        _artifactManager?.NotifyArtifactCollected(this);
    }

    public SceneObjectStateData CaptureState()
    {
        return new SceneObjectStateData
        {
            id = SaveId,
            type = SceneObjectType.Artifact,
            state = _isCollected ? 1 : 0
        };
    }

    public void RestoreState(SceneObjectStateData data)
    {
        bool collected = data != null && data.state == 1;
        ApplyCollectedState(collected);

        if (collected && _artifactManager != null)
        {
            _artifactManager.NotifyArtifactRestored(this);
        }
    }

    private void MarkAsCollected()
    {
        ApplyCollectedState(true);
    }

    private void ApplyCollectedState(bool collected)
    {
        _isCollected = collected;

        if (_disableTriggerAfterPickup && _triggerCollider != null)
        {
            _triggerCollider.enabled = !collected;
        }

        if (_objectToDisableOnPickup != null)
        {
            _objectToDisableOnPickup.SetActive(!collected);
        }

        if (_roomDoorLight != null)
        {
            _roomDoorLight.color = collected ? _collectedLightColor : _defaultLightColor;
        }
    }

    private string BuildFallbackSaveId()
    {
        string path = name;
        Transform current = transform.parent;
        while (current != null)
        {
            path = current.name + "/" + path;
            current = current.parent;
        }

        return $"{nameof(Level3Artifact)}:{path}";
    }
}

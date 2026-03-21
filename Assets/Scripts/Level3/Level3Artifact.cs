using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class Level3Artifact : MonoBehaviour
{
    [Header("Links")]
    [SerializeField] private Level3ArtifactManager _artifactManager;
    [SerializeField] private string _playerTag = "Player";
    [SerializeField] private Light _roomDoorLight;
    [SerializeField] private Color _collectedLightColor = Color.green;

    [Header("Pickup")]
    [SerializeField] private GameObject _objectToDisableOnPickup;
    [SerializeField] private bool _disableTriggerAfterPickup = true;

    private Collider _triggerCollider;
    private bool _isCollected;
    public bool IsCollected => _isCollected;

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
    }

    private void MarkAsCollected()
    {
        _isCollected = true;

        if (_disableTriggerAfterPickup && _triggerCollider != null)
        {
            _triggerCollider.enabled = false;
        }

        if (_objectToDisableOnPickup != null)
        {
            _objectToDisableOnPickup.SetActive(false);
        }

        if (_roomDoorLight != null)
        {
            _roomDoorLight.color = _collectedLightColor;
        }
    }
}

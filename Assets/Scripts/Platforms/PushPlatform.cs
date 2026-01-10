using UnityEngine;
using StarterAssets;
using Zenject;

public class PushPlatform : MonoBehaviour
{
    [SerializeField] private Vector3 direction = Vector3.forward;
    [SerializeField] private float speed = 2f;

    private ThirdPersonController _player;
    private bool _isPlayerOnPlatform;

    [Inject]
    private void Construct(ThirdPersonController player)
    {
        _player = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out ThirdPersonController player) && player == _player)
        {
            _isPlayerOnPlatform = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out ThirdPersonController player) && player == _player)
        {
            _isPlayerOnPlatform = false;
        }
    }

    private void Update()
    {
        if (_isPlayerOnPlatform && _player.Grounded)
        {
            Vector3 conveyorVelocity = direction.normalized * speed;
            _player.SetExternalVelocity(conveyorVelocity);
        }
    }
}

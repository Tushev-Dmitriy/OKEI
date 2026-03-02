using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float maxDistance = 5f;
    [SerializeField] private LayerMask interactableLayer;

    private MainInputSystem _input;
    private InputAction _interactAction;

    private void Awake()
    {
        _input = new MainInputSystem();
    }

    private void OnEnable()
    {
        _input.Player.Enable();
        _interactAction = _input.Player.Interact;
        _interactAction.performed += OnInteract;
    }

    private void OnDisable()
    {
        _interactAction.performed -= OnInteract;
        _interactAction.Disable();
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, interactableLayer))
        {
            if (hit.collider.TryGetComponent<LockControlInteractable>(out var lockInteractable))
            {
                lockInteractable.Interact();
                return;
            }

            if (hit.collider.TryGetComponent<InteractableAnimator>(out var interactable))
            {
                interactable.Interact();
            }
        }
    }
}

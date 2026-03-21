using DG.Tweening;
using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.UI;

public class TerminalController : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private GameObject _objToShow;
    [SerializeField] private MonoBehaviour _sliderComponent;
    private IChangeSlider _slider;
    private ThirdPersonController _activePlayer;
    private StarterAssetsInputs _activePlayerInputs;
    private Collider _triggerCollider;

    private void Awake()
    {
        RefreshSliderReference();

        _animator = transform.parent.GetComponent<Animator>();
        _triggerCollider = GetComponent<Collider>();
        _animator.SetTrigger("Close");
    }

    public void Configure(GameObject objToShow, MonoBehaviour sliderComponent)
    {
        _objToShow = objToShow;
        _sliderComponent = sliderComponent;
        RefreshSliderReference();
    }

    private void RefreshSliderReference()
    {
        _slider = _sliderComponent as IChangeSlider;
    }

    private void OpenTerminal()
    {
        if (_objToShow == null)
        {
            return;
        }

        SetTerminalInteractionMode(true);
        _animator.SetTrigger("Open");
        _objToShow.transform.DOScale(Vector3.one, 1.25f).SetEase(Ease.OutBack);

        if (_slider != null)
        {
            Slider slider = _objToShow.GetComponentInChildren<Slider>(true);
            if (slider != null)
            {
                float currentValue = _slider.CurrentValue();
                slider.SetValueWithoutNotify(currentValue);

                Transform valueTransform = _objToShow.transform.Find("Value");
                if (valueTransform != null && valueTransform.TryGetComponent(out Text valueText))
                {
                    valueText.text = currentValue.ToString("0.00");
                }
            }
        }
    }

    private void CloseTerminal()
    {
        if (_objToShow == null)
        {
            return;
        }

        SetTerminalInteractionMode(false);
        _animator.SetTrigger("Close");
        _objToShow.transform.DOScale(Vector3.zero, 1.25f).SetEase(Ease.InBack);
    }

    private void SetTerminalInteractionMode(bool isOpened)
    {
        if (_activePlayerInputs != null)
        {
            _activePlayerInputs.cursorLocked = !isOpened;
            _activePlayerInputs.cursorInputForLook = !isOpened;

            if (isOpened)
            {
                _activePlayerInputs.MoveInput(Vector2.zero);
                _activePlayerInputs.LookInput(Vector2.zero);
                _activePlayerInputs.JumpInput(false);
                _activePlayerInputs.SprintInput(false);
            }
        }

        Cursor.lockState = isOpened ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isOpened;
    }

    private void Update()
    {
        if (_activePlayer != null && !IsPlayerInsideTrigger(_activePlayer))
        {
            CloseTerminal();
            _activePlayer = null;
            _activePlayerInputs = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
        {
            return;
        }

        _activePlayer = player;
        _activePlayerInputs = player.GetComponent<StarterAssetsInputs>();
        OpenTerminal();
    }

    private void OnTriggerExit(Collider other)
    {
        ThirdPersonController player = other.GetComponentInParent<ThirdPersonController>();
        if (player == null)
        {
            return;
        }

        StartCoroutine(ValidateExit(player));
    }

    private void OnDisable()
    {
        SetTerminalInteractionMode(false);
        _activePlayerInputs = null;
    }

    private IEnumerator ValidateExit(ThirdPersonController player)
    {
        yield return null;

        if (player == null)
        {
            yield break;
        }

        if (IsPlayerInsideTrigger(player))
        {
            yield break;
        }

        CloseTerminal();
        if (_activePlayer == player)
        {
            _activePlayer = null;
        }
        _activePlayerInputs = null;
    }

    private bool IsPlayerInsideTrigger(ThirdPersonController player)
    {
        if (player == null || _triggerCollider == null || !_triggerCollider.enabled || !_triggerCollider.gameObject.activeInHierarchy)
        {
            return false;
        }

        CharacterController playerController = player.GetComponent<CharacterController>();
        if (playerController == null || !playerController.enabled)
        {
            return false;
        }

        bool overlaps = Physics.ComputePenetration(
            _triggerCollider,
            _triggerCollider.transform.position,
            _triggerCollider.transform.rotation,
            playerController,
            playerController.transform.position,
            playerController.transform.rotation,
            out _,
            out _
        );

        return overlaps || _triggerCollider.bounds.Intersects(playerController.bounds);
    }
}

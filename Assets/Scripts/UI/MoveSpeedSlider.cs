using StarterAssets;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class MoveSpeedSlider : MonoBehaviour, IChangeSlider
{
    private Slider _slider;
    private SignalBus _signalBus;
    private ThirdPersonController _player;

    [Inject]
    private void Construct(SignalBus signalBus, ThirdPersonController player)
    {
        _signalBus = signalBus;
        _player = player;
    }

    private void Awake()
    {
        GetSliderData();
    }

    public void GetSliderData()
    {
        _slider = GetComponent<Slider>();
        _slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        UpdateValueLabel(value);
        if (_signalBus == null)
        {
            return;
        }

        _signalBus.Fire(new PlayerParamChangedSignal
        {
            ParamType = PlayerParamType.MoveSpeed,
            Value = value
        });

        GameplaySaveManager.SaveCurrentGame();
    }

    private void UpdateValueLabel(float value)
    {
        Transform valueTransform = transform.parent != null ? transform.parent.Find("Value") : null;
        if (valueTransform != null && valueTransform.TryGetComponent(out Text valueText))
        {
            valueText.text = value.ToString("0.00");
        }
    }

    float IChangeSlider.CurrentValue()
    {
        if (_player == null)
        {
            _player = FindFirstObjectByType<ThirdPersonController>();
        }

        return _player != null ? _player.MoveSpeed : (_slider != null ? _slider.value : 0f);
    }
}

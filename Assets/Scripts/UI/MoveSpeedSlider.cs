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
        _signalBus.Fire(new PlayerParamChangedSignal
        {
            ParamType = PlayerParamType.MoveSpeed,
            Value = value
        });
    }

    float IChangeSlider.CurrentValue()
    {
        return _player.MoveSpeed;
    }
}

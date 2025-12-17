using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class JumpHeightSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    private SignalBus _signalBus;

    [Inject]
    private void Construct(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    private void Awake()
    {
        _slider.onValueChanged.AddListener(value =>
        {
            _signalBus.Fire(new PlayerParamChangedSignal { Value = value });
        });
    }
}

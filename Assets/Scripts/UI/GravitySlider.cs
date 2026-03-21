using StarterAssets;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GravitySlider : MonoBehaviour, IChangeSlider
{
    [SerializeField] private float minGravity = -40f;
    [SerializeField] private float maxGravity = -2f;

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
        ApplySliderRange();
        _slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        value = ClampGravity(value);
        UpdateValueLabel(value);
        _signalBus.Fire(new PlayerParamChangedSignal
        {
            ParamType = PlayerParamType.Gravity,
            Value = value
        });
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
        return ClampGravity(_player.Gravity);
    }

    private void ApplySliderRange()
    {
        if (_slider == null)
        {
            return;
        }

        float min = Mathf.Min(minGravity, maxGravity);
        float max = Mathf.Max(minGravity, maxGravity);

        _slider.minValue = Mathf.Min(min, -0.01f);
        _slider.maxValue = Mathf.Min(max, -0.01f);
        _slider.wholeNumbers = false;
    }

    private float ClampGravity(float value)
    {
        float min = Mathf.Min(_slider != null ? _slider.minValue : minGravity, _slider != null ? _slider.maxValue : maxGravity);
        float max = Mathf.Max(_slider != null ? _slider.minValue : minGravity, _slider != null ? _slider.maxValue : maxGravity);
        return Mathf.Clamp(value, min, Mathf.Min(max, -0.01f));
    }
}

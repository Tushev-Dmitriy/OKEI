using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TerminalController : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private GameObject _objToShow;
    [SerializeField] private MonoBehaviour _sliderComponent;
    private IChangeSlider _slider;

    private void Awake()
    {
        _slider = _sliderComponent as IChangeSlider;
        if (_slider == null) Debug.LogError("Компонент не реализует IChangeSlider");

        _animator = transform.parent.GetComponent<Animator>();
    }

    private void OpenTerminal()
    {
        _animator.SetTrigger("Open");
        _objToShow.transform.DOScale(Vector3.one, 1.25f).SetEase(Ease.OutBack);
        _objToShow.transform.GetChild(1).GetComponent<Slider>().value = _slider.CurrentValue();
    }

    private void CloseTerminal()
    {
        _animator.SetTrigger("Close");
        _objToShow.transform.DOScale(Vector3.zero, 1.25f).SetEase(Ease.InBack);
    }

    private void OnTriggerEnter(Collider other)
    {
        OpenTerminal();
    }

    private void OnTriggerExit(Collider other)
    {
        CloseTerminal();
    }
}

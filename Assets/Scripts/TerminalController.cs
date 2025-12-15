using DG.Tweening;
using UnityEngine;

public class TerminalController : MonoBehaviour
{
    private Animator _animator;
    [SerializeField] private GameObject _objToShow;

    private void Awake()
    {
        _animator = transform.parent.GetComponent<Animator>();
    }

    private void OpenTerminal()
    {
        _animator.SetTrigger("Open");
        _objToShow.transform.DOScale(Vector3.one, 1).SetEase(Ease.OutBack);
    }

    private void CloseTerminal()
    {
        _animator.SetTrigger("Close");
        _objToShow.transform.DOScale(Vector3.zero, 1).SetEase(Ease.InBack);
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

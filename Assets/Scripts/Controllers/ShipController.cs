using DG.Tweening;
using UnityEngine;

public class ShipController : MonoBehaviour
{
    [SerializeField] private GameObject posToStopObj;
    [SerializeField] private GameObject posToEndObj;

    private Vector3 _posToStop;
    private Vector3 _posToEnd;

    private bool _hasStopped = false;

    private void Awake()
    {
        _posToStop = posToStopObj.transform.position;
        _posToEnd = posToEndObj.transform.position;
    }

    private void Start()
    {
        MoveToStop();
    }

    private void MoveToStop()
    {
        transform.DOMove(_posToStop, 10f).SetEase(Ease.InOutSine).OnComplete(() => ChangeStop());
    }

    private void ChangeStop() => _hasStopped = !_hasStopped;
}

using DG.Tweening;
using UnityEngine;

public class DoorOpener : MonoBehaviour
{
    [SerializeField] private Transform _leftDoor;
    [SerializeField] private Transform _rightDoor;

    [SerializeField] private Vector3 _leftDoorTarget;
    [SerializeField] private Vector3 _rightDoorTarget;
    [SerializeField] private float _duration = 1.0f;

    private Vector3 _leftDoorPosition;
    private Vector3 _rightDoorPosition;

    private void Awake()
    {
        _leftDoorPosition = _leftDoor.transform.position;
        _rightDoorPosition = _rightDoor.transform.position;
    }

    public void OpenDoors()
    {
        _leftDoor.DOLocalMove(_leftDoorTarget, _duration).SetEase(Ease.InOutSine);
        _rightDoor.DOLocalMove(_rightDoorTarget, _duration).SetEase(Ease.InOutSine);
    }

    public void CloseDoors()
    {
        _leftDoor.DOLocalMove(_leftDoorPosition, _duration).SetEase(Ease.InOutSine);
        _rightDoor.DOLocalMove(_rightDoorPosition, _duration).SetEase(Ease.InOutSine);
    }
}

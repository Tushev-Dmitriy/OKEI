using DG.Tweening;
using UnityEngine;

public class DoorCondition : MonoBehaviour
{
    [SerializeField] GameObject _slotUiObj;
    private bool _conditionData;

    public void ConditionData(bool condition) => _conditionData = condition; 

    public void CheckItemInSlot()
    {
        Debug.Log(1);
    }

    private void ConditionChecker()
    {

    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _slotUiObj.transform.DOScale(Vector3.zero, 1f).SetEase(Ease.OutBack);
        }
    }
}

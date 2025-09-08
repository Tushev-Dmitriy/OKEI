using DG.Tweening;
using UnityEngine;

public class TooltipTrigger : MonoBehaviour
{
    [SerializeField] private TooltipUI _tooltipUI;

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            _tooltipUI.Show();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            _tooltipUI.Hide();
        }
    }
}

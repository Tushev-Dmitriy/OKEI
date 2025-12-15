using DG.Tweening;
using System.Collections;
using UnityEngine;

public class PreviewJumpController : MonoBehaviour
{
    private Vector3 _basePos;
    private Animator _animator;

    private void Awake()
    {
        _basePos = transform.localPosition;
        _animator = GetComponent<Animator>();

        StartCoroutine(ShowJump(2.5f));
    }

    public IEnumerator ShowJump(float height)
    {
        _animator.SetBool("Jump", true);

        float target = _basePos.y + height;

        Sequence s = DOTween.Sequence();
        s.Append(transform.DOLocalMoveY(target, 0.5f).SetEase(Ease.OutQuad));
        s.Append(transform.DOLocalMoveY(_basePos.y, 0.5f).SetEase(Ease.InQuad));

        _animator.SetBool("Grounded", true);

        yield return new WaitForSeconds(0.25f);

        _animator.SetBool("Jump", false);
    }
}

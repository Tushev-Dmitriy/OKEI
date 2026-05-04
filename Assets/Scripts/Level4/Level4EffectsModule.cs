using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class Level4EffectsModule : MonoBehaviour
{
    private readonly Dictionary<EnemyUnit, Vector3> _enemyBaseScales = new();
    private readonly Dictionary<EnemyUnit, Coroutine> _enemyScaleCoroutines = new();

    public void ClearEnemyRespawnCache()
    {
        _enemyBaseScales.Clear();
    }

    public void RememberEnemyBaseScale(EnemyUnit enemy)
    {
        if (enemy == null)
            return;

        _enemyBaseScales[enemy] = enemy.transform.localScale;
    }

    public void PlayEnemyRespawnScale(Level4FlowController flow, EnemyUnit enemy)
    {
        if (flow == null || enemy == null)
            return;

        Transform tr = enemy.transform;
        if (!_enemyBaseScales.TryGetValue(enemy, out Vector3 targetScale))
        {
            targetScale = tr.localScale;
            _enemyBaseScales[enemy] = targetScale;
        }

        if (_enemyScaleCoroutines.TryGetValue(enemy, out Coroutine running) && running != null)
        {
            StopCoroutine(running);
        }

        float startFactor = Mathf.Clamp(flow.EnemyRespawnStartScaleFactor, 0.05f, 1f);
        tr.localScale = targetScale * startFactor;

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            tr.localScale = targetScale;
            _enemyBaseScales[enemy] = targetScale;
            _enemyScaleCoroutines.Remove(enemy);
            return;
        }

        Coroutine routine = StartCoroutine(AnimateEnemyScaleRoutine(flow, enemy, targetScale));
        _enemyScaleCoroutines[enemy] = routine;
    }

    public IEnumerator AnimateEnemyScaleRoutine(Level4FlowController flow, EnemyUnit enemy, Vector3 targetScale)
    {
        if (flow == null || enemy == null)
            yield break;

        Transform tr = enemy.transform;
        Vector3 from = tr.localScale;
        float duration = Mathf.Max(0.05f, flow.EnemyRespawnScaleDuration);
        float t = 0f;

        while (t < duration && enemy != null && tr != null)
        {
            t += Time.deltaTime;
            float lerp = Mathf.Clamp01(t / duration);
            tr.localScale = Vector3.LerpUnclamped(from, targetScale, lerp);
            yield return null;
        }

        if (enemy != null && tr != null)
        {
            tr.localScale = targetScale;
            _enemyBaseScales[enemy] = targetScale;
        }

        if (enemy != null)
            _enemyScaleCoroutines.Remove(enemy);
    }

    public void StopAllEnemyRespawnScaleCoroutines()
    {
        foreach (var pair in _enemyScaleCoroutines)
        {
            if (pair.Value != null)
                StopCoroutine(pair.Value);
        }

        _enemyScaleCoroutines.Clear();
    }
}

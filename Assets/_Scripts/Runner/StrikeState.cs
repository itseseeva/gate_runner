using UnityEngine;

/// <summary>
/// Состояние "рывок и удар".
/// Воин бежит к цели, бьёт один раз, ищет следующую жертву в радиусе.
/// Если врагов в радиусе нет → переход в Drift (плавный возврат в строй).
/// </summary>
public class StrikeState : IUnitState
{
    private readonly MeleeUnitController _ctrl;
    private Enemy _target;

    public StrikeState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    /// <summary>Устанавливает цель ПЕРЕД входом в состояние (вызывается из FollowState).</summary>
    public void SetTarget(Enemy target)
    {
        _target = target;
    }

    public void Enter() { }

    public void Exit()
    {
        // Освобождаем текущую цель если ещё держим (например при выходе в Drift)
        if (_target != null)
        {
            _ctrl.ReleaseTarget(_target);
            _target = null;
        }
    }

    public void Tick()
    {
        // Цель умерла во время рывка? → Освобождаем и ищем новую
        if (_target == null || !_target.gameObject.activeSelf)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;

            FindAndSetNewTargetOrDrift();
            return;
        }

        // Бежим к цели
        Vector3 toEnemy  = _target.transform.position - _ctrl.transform.position;
        float   distance = toEnemy.magnitude;

        if (distance <= _ctrl.AttackRange)
        {
            // Подбежали — бьём
            DiagLogger.RecordHit(_ctrl.gameObject.GetInstanceID(), _target.GetInstanceID());
            _target.TakeDamage(_ctrl.Damage);
            Debug.Log($"[Strike] {_ctrl.gameObject.name} ударил {_target.name}! Урон: {_ctrl.Damage}", _ctrl);

            // Освобождаем побитую цель → доступна для других воинов / умрёт от добивания
            _ctrl.ReleaseTarget(_target);
            _target = null;

            // Сразу ищем следующего
            FindAndSetNewTargetOrDrift();
            return;
        }

        // Двигаемся к врагу
        Vector3 dir = toEnemy.normalized;
        _ctrl.transform.position += dir * _ctrl.ChaseSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Ищет следующего врага в радиусе. Найден → продолжаем рывок к нему.
    /// Не найден → переходим в Drift (плавный возврат в строй).
    /// </summary>
    private void FindAndSetNewTargetOrDrift()
    {
        Enemy next = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange);
        if (next != null)
        {
            _ctrl.ClaimTarget(next);
            _target = next;
        }
        else
        {
            _ctrl.ChangeState(_ctrl.DriftState);
        }
    }
}

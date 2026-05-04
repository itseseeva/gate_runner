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
            // Используем компонент атаки вместо прямого вызова TakeDamage
            if (_ctrl.AutoAttack == null) return;
            if (!_ctrl.AutoAttack.IsReady) return; // ждём cooldown

            DiagLogger.RecordHit(_ctrl.gameObject.GetInstanceID(), _target.GetInstanceID());
            HitResult result = _ctrl.AutoAttack.Hit(_target);

            if (result.WasCritical)
            {
                Debug.Log($"[Strike] {_ctrl.gameObject.name} КРИТ по {_target.name}! Урон: {result.DamageDealt}", _ctrl);
            }

            if (result.Killed)
            {
                _ctrl.ReleaseTarget(_target);
                _target = null;
                FindAndSetNewTargetOrDrift();
            }
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

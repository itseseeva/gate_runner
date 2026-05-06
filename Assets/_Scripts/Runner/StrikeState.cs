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
    private Enemy _lastHitEnemy; // враг которого только что ударили
    public float LastHitZ { get; private set; }    // Z позиция последнего удара

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
            if (_ctrl.AutoAttack == null) return;
            if (!_ctrl.AutoAttack.IsReady) return;

            DiagLogger.RecordHit(_ctrl.gameObject.GetInstanceID(), _target.GetInstanceID());
            HitResult result = _ctrl.AutoAttack.Hit(_target);

            if (result.WasCritical)
                Debug.Log($"[Strike] {_ctrl.gameObject.name} КРИТ по {_target.name}! Урон: {result.DamageDealt}", _ctrl);

            // Один удар — сразу уходим, не ждём смерти врага
            LastHitZ = _target.transform.position.z;
            _lastHitEnemy = _target;
            _ctrl.ReleaseTarget(_target);
            _target = null;
            FindAndSetNewTargetOrDrift();
            return;
        }

        // Двигаемся к врагу
        Vector3 dir = toEnemy.normalized;
        _ctrl.transform.position += dir * _ctrl.ChaseSpeed * Time.deltaTime;
    }

    private void FindAndSetNewTargetOrDrift()
    {
        // Временно бронируем последнего побитого чтобы не выбрать его снова
        if (_lastHitEnemy != null && _lastHitEnemy.gameObject.activeSelf)
            _ctrl.ClaimTarget(_lastHitEnemy);

        // Ищем врага ТОЛЬКО впереди по Z (следующая волна)
        Enemy next = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange, minZ: LastHitZ + 1f);

        // Освобождаем последнего побитого
        if (_lastHitEnemy != null)
        {
            _ctrl.ReleaseTarget(_lastHitEnemy);
            _lastHitEnemy = null;
        }

        Debug.Log($"[Strike] FindNext: {(next != null ? next.name : "NULL → Drift")}", _ctrl);

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

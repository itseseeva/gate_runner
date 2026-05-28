using UnityEngine;

/// <summary>
/// Состояние "рывок и удар". Воин бежит к цели, бьёт один раз,
/// ищет следующую цель ВПЕРЕДИ по Z. Если нет — возврат в Follow.
/// </summary>
public class StrikeState : IUnitState
{
    private readonly MeleeUnitController _ctrl;
    private Enemy _target;
    private Enemy _lastHitEnemy;

    public float LastHitZ { get; private set; }

    public StrikeState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    public void SetTarget(Enemy target) => _target = target;

    public void Enter()
    {
        _ctrl.PlayAttackRun();
    }

    public void Exit()
    {
        if (_target != null)
        {
            _ctrl.ReleaseTarget(_target);
            _target = null;
        }
    }

    public void Tick()
    {
        // Цель умерла во время рывка
        if (_target == null || !_target.gameObject.activeSelf)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;
            FindNextOrReturn();
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
                Debug.Log($"[Strike] {_ctrl.gameObject.name} КРИТ! Урон: {result.DamageDealt}", _ctrl);

            // Один удар — запоминаем Z и ищем следующего
            LastHitZ = _target.transform.position.z;
            _lastHitEnemy = _target;
            _ctrl.ReleaseTarget(_target);
            _target = null;
            FindNextOrReturn();
            return;
        }

        // Двигаемся к врагу
        Vector3 dir = toEnemy.normalized;
        _ctrl.transform.position += dir * _ctrl.ChaseSpeed * Time.deltaTime;
    }

    /// <summary>
    /// Ищет следующего врага ВПЕРЕДИ (Z больше LastHitZ).
    /// Найден → продолжаем Strike. Нет → возврат в Follow с плавным Lerp.
    /// </summary>
    private void FindNextOrReturn()
    {
        // Бронируем последнего побитого чтобы не выбрать его снова
        if (_lastHitEnemy != null && _lastHitEnemy.gameObject.activeSelf)
            _ctrl.ClaimTarget(_lastHitEnemy);

        Debug.Log($"[Strike] LastHitZ={LastHitZ:F1}, ищем minZ={LastHitZ + 3f:F1}");
        Enemy next = _ctrl.FindRandomEnemyInRange(_ctrl.DetectionRange, minZ: LastHitZ + 3f);

        if (_lastHitEnemy != null)
        {
            _ctrl.ReleaseTarget(_lastHitEnemy);
            _lastHitEnemy = null;
        }

        if (next != null)
        {
            Debug.Log($"[Strike] Найден next на Z={next.transform.position.z:F1}");
            _ctrl.ClaimTarget(next);
            _target = next;
            return;
        }

        // Нет следующих — запускаем плавный возврат и переходим в Follow
        _ctrl.StartRejoin();
        _ctrl.ChangeState(_ctrl.FollowState);
    }
}

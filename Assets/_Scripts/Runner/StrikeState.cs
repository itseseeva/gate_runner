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

    private bool _waitingAfterHit = false;

    public float LastHitZ { get; private set; }

    public StrikeState(MeleeUnitController controller)
    {
        _ctrl = controller;
    }

    public void SetTarget(Enemy target) => _target = target;

    public void Enter()
    {
        _waitingAfterHit = false;
        _ctrl.PlayAttackRun();
    }

    public void Exit()
    {
        _waitingAfterHit = false;
        if (_target != null)
        {
            _ctrl.ReleaseTarget(_target);
            _target = null;
        }
    }

    public void Tick()
    {
        // Ждём после удара чтобы анимация доиграла
        if (_waitingAfterHit)
        {
            // Ждём пока анимация AttackRun доиграет
            AnimatorStateInfo state = _ctrl.GetAnimatorState();
            bool attackDone = state.IsName("AttackRun") && state.normalizedTime >= 0.9f;
            bool notAttack = !state.IsName("AttackRun");
            
            if (attackDone || notAttack)
            {
                _waitingAfterHit = false;
                FindNextOrReturn();
            }
            return;
        }

        // Цель умерла во время рывка
        if (_target == null || !_target.gameObject.activeSelf)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;
            _waitingAfterHit = true;
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

            // Сбрасываем триггер Attack, чтобы избежать повторного ложного срабатывания анимации удара
            Animator animator = _ctrl.GetComponentInChildren<Animator>();
            if (animator != null)
            {
                animator.ResetTrigger("Attack");
            }

            if (result.WasCritical)
                Debug.Log($"[Strike] {_ctrl.gameObject.name} КРИТ! Урон: {result.DamageDealt}", _ctrl);

            LastHitZ = _target.transform.position.z;
            _lastHitEnemy = _target;
            _ctrl.ReleaseTarget(_target);
            _target = null;

            // Ждём перед поиском следующего
            _waitingAfterHit = true;
            return;
        }

        // Поводок: убежали от места в строю слишком далеко — бросаем погоню
        Vector3 homePos = _ctrl.Leader.position + _ctrl.FormationOffset;
        if (Vector3.Distance(_ctrl.transform.position, homePos) > _ctrl.MaxChaseDistance)
        {
            if (_target != null) _ctrl.ReleaseTarget(_target);
            _target = null;
            _ctrl.StartRejoin();
            _ctrl.ChangeState(_ctrl.FollowState);
            return;
        }

        // Двигаемся к врагу
        Vector3 dir = toEnemy.normalized;
        _ctrl.transform.position += dir * _ctrl.ChaseSpeed * Time.deltaTime;
    }

    private void FindNextOrReturn()
    {
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

        _ctrl.StartRejoin();
        _ctrl.ChangeState(_ctrl.FollowState);
    }
}

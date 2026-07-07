using UnityEngine;

/// <summary>
/// Стейт "враг бьёт". Не двигается, играет Attack анимацию,
/// урон наносится через Animation Event OnEnemyAttackHit().
/// Если цель ушла из радиуса или умерла — возврат в Approach.
/// </summary>
public class EnemyAttackState : EnemyStateBase
{
    private Unit  _target;
    private float _lastAttackTime;
    private bool  _attackTriggered;
    private Vector3 _enterPos;
    private int     _tickCounter;

    public Unit  Target      => _target;
    public bool  IsCoolingDown => Time.time - _lastAttackTime < (1f / _ctrl.AttackSpeed);

    public EnemyAttackState(EnemyController ctrl, Unit initialTarget) : base(ctrl)
    {
        _target = initialTarget;
    }

    public override void Enter()
    {
        if (_ctrl.Scroller != null)
            _ctrl.Scroller.enabled = false;

        _enterPos    = _ctrl.transform.position;
        _tickCounter = 0;

        _lastAttackTime = Time.time - (1f / _ctrl.AttackSpeed) + 0.3f;
        _attackTriggered = false;

        FaceTarget();

        Debug.Log($"[EnemyAttack ENTER] {_ctrl.name}: pos={_enterPos:F3}, " +
                  $"AttackRange={_ctrl.AttackRange}, AttackSpeed={_ctrl.AttackSpeed}", _ctrl);
    }

    public override void Tick()
    {
        _tickCounter++;
        if (_tickCounter % 20 == 0)
        {
            Vector3 cur = _ctrl.transform.position;
            Vector3 drift = cur - _enterPos;
            bool scrollerEnabled = _ctrl.Scroller != null && _ctrl.Scroller.enabled;
            Debug.Log($"[EnemyAttack TICK] {_ctrl.name}: drift={drift:F3}, " +
                      $"scroller.enabled={scrollerEnabled}", _ctrl);
        }

        // 1. Цель мертва или пропала? Ищем новую.
        if (_target == null || _target.IsDead)
        {
            // Освобождаем старую цель (она умерла — но регистрация на трупе висела)
            if (_target != null)
                EnemyTargetRegistry.Unregister(_target);

            // Выбираем новую через реестр
            SquadController squad = Object.FindAnyObjectByType<SquadController>();
            _target = EnemyTargetRegistry.GetLeastAttacked(_ctrl.transform.position, squad);

            if (_target == null)
            {
                _ctrl.SwitchTo(new EnemyApproachState(_ctrl));
                return;
            }

            // Регистрируем новую
            EnemyTargetRegistry.Register(_target);
        }

        // 2. Цель ушла из радиуса? Возврат в Approach.
        float distSqr = SqrDistanceXZ(_ctrl.transform.position, _target.transform.position);
        float rangeSqr = _ctrl.AttackRange * _ctrl.AttackRange;
        if (distSqr > rangeSqr)
        {
            _ctrl.SwitchTo(new EnemyApproachState(_ctrl));
            return;
        }

        // 3. Держим лицо к цели.
        FaceTarget();

        // 4. Cooldown? Ждём.
        if (IsCoolingDown) return;

        // 5. Триггерим анимацию удара. Урон нанесётся через Animation Event.
        if (!_attackTriggered && _ctrl.Animator != null)
        {
            _ctrl.Animator.SetTrigger("Attack");
            _attackTriggered = true;
        }
    }

    public override void Exit()
    {
        Vector3 exitPos = _ctrl.transform.position;
        Vector3 totalDrift = exitPos - _enterPos;
        Debug.Log($"[EnemyAttack EXIT] {_ctrl.name}: totalDrift={totalDrift:F3}", _ctrl);

        // Освобождаем цель — атака закончилась (враг умер или цель ушла).
        // Если Approach снова начнётся — он выберет цель заново.
        if (_target != null)
        {
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
    }

    /// <summary>
    /// Вызывается через Animation Event в момент взмаха.
    /// Наносит урон и запускает cooldown.
    /// </summary>
    public void OnAnimationHit()
    {
        Debug.Log($"[EnemyAttack HIT] {_ctrl.name}: target={_target?.name}, " +
                  $"targetDead={_target?.IsDead}, damage={_ctrl.Damage}", _ctrl);

        _attackTriggered = false;
        _lastAttackTime = Time.time;

        if (_target == null || _target.IsDead) return;

        // Ещё раз проверяем — цель могла отойти между триггером и хитом.
        float distSqr = SqrDistanceXZ(_ctrl.transform.position, _target.transform.position);
        float rangeSqr = _ctrl.AttackRange * _ctrl.AttackRange;
        if (distSqr > rangeSqr) return;

        bool killed = _target.TakeDamage(_ctrl.Damage);
        if (killed)
        {
            SquadController squad = Object.FindAnyObjectByType<SquadController>();
            squad?.OnUnitDied(_target);
        }
    }

    private void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.transform.position - _ctrl.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;
        // Модель Skeleton_110 повёрнута на -190° внутри prefab-а,
        // поэтому разворачиваем корень В ОБРАТНУЮ сторону — тогда модель смотрит на цель.
        _ctrl.transform.rotation = Quaternion.LookRotation(-dir);
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}

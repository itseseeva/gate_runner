using UnityEngine;

/// <summary>
/// Стейт "вечная погоня после первой атаки".
/// Враг преследует цель со скоростью меньше отряда — создаёт ощущение погони.
/// Если догнал (цель в AttackRange) → переход обратно в Attack.
/// Если цель умерла → выбираем новую через реестр, продолжаем гнать.
/// </summary>
public class EnemyChaseState : EnemyStateBase
{
    // TODO: вынести в EnemyDefinitionSO при балансировке разных типов врагов
    private const float ChaseSpeedMultiplier = 0.7f; // 70% от WorldSpeed
    private const float AfterHitCooldown     = 1f;   // 1 сек не можем снова атаковать после удара

    private SquadController _squad;
    private Unit  _target;
    private float _enterTime;

    public EnemyChaseState(EnemyController ctrl, Unit initialTarget) : base(ctrl)
    {
        _target = initialTarget;
    }

    public override void Enter()
    {
        _enterTime = Time.time;

        Debug.Log($"[ChaseDiag ENTER] {_ctrl.name}: pos={_ctrl.transform.position:F2}, " +
                  $"target={_target?.name}, targetPos={_target?.transform.position:F2}", _ctrl);

        _squad = Object.FindAnyObjectByType<SquadController>();

        // Играем Run пока преследуем
        if (_ctrl.Animator != null)
            _ctrl.Animator.Play("Run");

        // WorldScroller выключен — Chase движется сам, а не от мира
        if (_ctrl.Scroller != null)
            _ctrl.Scroller.enabled = false;

        // Регистрируем цель в реестре — мы её "занимаем" пока преследуем
        if (_target != null)
            EnemyTargetRegistry.Register(_target);
    }

    public override void Exit()
    {
        // Освобождаем цель — следующий стейт её перерегистрирует если нужно
        if (_target != null)
        {
            EnemyTargetRegistry.Unregister(_target);
            _target = null;
        }
    }

    public override void Tick()
    {
        if (Time.frameCount % 30 == 0)
        {
            Debug.Log($"[ChaseDiag TICK] {_ctrl.name}: myPos={_ctrl.transform.position:F2}, " +
                      $"targetPos={_target?.transform.position:F2}, " +
                      $"scrollerEnabled={_ctrl.Scroller?.enabled}", _ctrl);
        }

        // 1. Цель мертва? Ищем новую.
        if (_target == null || _target.IsDead)
        {
            if (_target != null) EnemyTargetRegistry.Unregister(_target);

            _target = EnemyTargetRegistry.GetLeastAttacked(_ctrl.transform.position, _squad);

            if (_target == null)
            {
                // Никого нет — возврат в Approach, там разберётся
                _ctrl.SwitchTo(new EnemyApproachState(_ctrl));
                return;
            }

            EnemyTargetRegistry.Register(_target);
        }

        // 2. Догнал? → в Attack.
        // Cooldown после удара — нельзя сразу возвращаться в Attack.
        // Иначе Attack→Chase→Attack бесконечно, потому что враг ещё рядом с целью.
        if (Time.time - _enterTime < AfterHitCooldown) return;

        float distSqr = SqrDistanceXZ(_ctrl.transform.position, _target.transform.position);
        float rangeSqr = _ctrl.AttackRange * _ctrl.AttackRange;
        if (distSqr <= rangeSqr)
        {
            _ctrl.SwitchTo(new EnemyAttackState(_ctrl, _target));
            return;
        }

        // 3. Двигаемся к цели по XZ со скоростью 70% от WorldSpeed
        FaceTarget();
        MoveTowardsTarget();
    }

    private void MoveTowardsTarget()
    {
        Vector3 myPos = _ctrl.transform.position;
        Vector3 targetPos = _target.transform.position;

        Vector3 diff = targetPos - myPos;
        diff.y = 0;

        float distXZ = diff.magnitude;
        if (distXZ < 0.01f) return;

        Vector3 dir = diff / distXZ;
        float speed = WorldScroller.WorldSpeed * ChaseSpeedMultiplier;

        Vector3 delta = dir * speed * Time.deltaTime;

        Vector3 newPos = myPos + delta;
        newPos.y = myPos.y; // Y не трогаем — не прыгаем
        _ctrl.transform.position = newPos;
    }

    private void FaceTarget()
    {
        if (_target == null) return;
        Vector3 dir = _target.transform.position - _ctrl.transform.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.0001f) return;
        // Модель повёрнута на -190° внутри prefab-а — компенсируем как в AttackState
        _ctrl.transform.rotation = Quaternion.LookRotation(-dir);
    }

    private static float SqrDistanceXZ(Vector3 a, Vector3 b)
    {
        float dx = a.x - b.x;
        float dz = a.z - b.z;
        return dx * dx + dz * dz;
    }
}

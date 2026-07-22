using UnityEngine;

/// <summary>
/// Маг стреляет по кулдауну из EnemyDefinitionSO. Между выстрелами
/// проигрывает Run (IsAttacking = false), в момент выстрела — триггерит анимацию атаки.
/// Едет с отрядом, стреляет пока есть цель.
/// </summary>
public class EnemyRangedAttackState : EnemyState
{
    private float _nextShotTime;

    public EnemyRangedAttackState(EnemyCombatBase ctrl) : base(ctrl) { }

    public override void Enter()
    {
        Ctrl.SetScroller(true);           // едет с отрядом
        Ctrl.SetAnimatorAttacking(false); // между выстрелами — Run
        _nextShotTime = Time.time;        // первый выстрел сразу
    }

    public override void Tick()
    {
        if (Ctrl.Target == null || Ctrl.Target.IsDead) return;

        Ctrl.FaceTarget();

        if (Time.time >= _nextShotTime)
        {
            _nextShotTime = Time.time + Ctrl.AttackCooldown;

            // Стреляем только если цель в досягаемости — иначе снаряд не долетит.
            float range = Ctrl.AttackRange;
            if (Ctrl.DistToTargetPointSqr() <= range * range)
            {
                Ctrl.TriggerAttackAnim();   // разовая анимация атаки
                Ctrl.FireProjectile();
            }
        }
    }
}

using UnityEngine;

/// <summary>
/// Враг дальнего боя. Держит дистанцию и стреляет снарядами по кулдауну.
/// Выстрел идёт из EnemyRangedAttackState по таймеру, а не по Animation Event.
/// </summary>
public class EnemyRangedCombat : EnemyCombatBase
{
    // Запас дальности снаряда сверх дистанции стрельбы — чтобы точно долетел.
    private const float RangeMargin = 1.3f;

    private int Damage => Data != null ? Data.AttackDamage : 10;

    // Рейнджер не прибивается к Z цели и заходит в своё состояние атаки.
    public override bool SticksToTargetZ => false;
    public override EnemyState StartState => RangedAttackState;
    public override EnemyState AttackStateFor => RangedAttackState;

    public override void FireProjectile()
    {
        if (Data == null || Target == null) return;

        float fireRange = AttackRange;
        if (DistToTargetPointSqr() > fireRange * fireRange) return;

        GameObject prefab = Data.ProjectilePrefab;
        if (prefab == null || ProjectilePool.Instance == null) return;

        Vector3 spawnPos = transform.position;

        Vector3 targetPos = Target.transform.position;   // цель — на уровне героя
        Vector3 dir = targetPos - spawnPos;
        Quaternion rot = dir.sqrMagnitude > 0.0001f
            ? Quaternion.LookRotation(dir)
            : transform.rotation;

        Projectile p = ProjectilePool.Instance.Get(prefab, spawnPos, rot);
        if (p == null) return;

        p.Launch(Damage, AttackRange * 1.5f, ElementType.None);
    }
}

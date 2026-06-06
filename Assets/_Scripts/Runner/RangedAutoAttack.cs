using UnityEngine;

/// <summary>
/// Дальнобойная атака. Выпускает снаряд вперёд по Z.
/// Наследует MeleeAutoAttackBase для совместимости с IUnitAttack.
/// </summary>
public class RangedAutoAttack : MeleeAutoAttackBase
{
    [Header("Дальнобойная атака")]
    [Tooltip("Высота спавна снаряда относительно юнита")]
    [SerializeField] private float _spawnHeightOffset = 0.5f;

    private Enemy _pendingTarget; // цель, в которую полетит снаряд при OnShoot

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage * powerMultiplier,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }

    /// <summary>
    /// Hit только запускает анимацию стрельбы.
    /// Реальный спавн снаряда — в OnShoot(), вызывается Animation Event-ом.
    /// </summary>
    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        _pendingTarget = target;

        if (Animator != null)
            Animator.SetTrigger("Shoot");

        UpdateCooldown();

        return new HitResult { Hit = true };
    }

    /// <summary>
    /// Вызывается через Animation Event в момент выстрела (натянул-отпустил тетиву).
    /// Спавнит снаряд нужной стихии из пула.
    /// </summary>
    public void OnShoot()
    {
        int multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        ElementType element = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;

        Vector3 spawnPos = transform.position + Vector3.up * _spawnHeightOffset;
        Projectile p = ProjectilePool.Instance.Get(element, spawnPos, Quaternion.identity);
        if (p == null) return;

        p.Launch(calc.FinalDamage, _range, element);
    }
}

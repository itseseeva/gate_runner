using UnityEngine;

/// <summary>
/// Атака танка щитом — АОЕ урон + отталкивание всех врагов в радиусе.
/// Весь урон и эффекты наносятся только через АОЕ.
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    [Header("АОЕ удар щитом")]
    [Tooltip("Радиус АОЕ в метрах")]
    [SerializeField] private float _bashRadius = 3f;

    [Tooltip("Сила отталкивания от 0 до 1")]
    [Range(0f, 1f)]
    [SerializeField] private float _knockbackForce = 0.4f;

    [Tooltip("Скорость анимации атаки")]
    [SerializeField] private float _attackAnimationSpeed = 1f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;
    [SerializeField] private HeroType _heroType = HeroType.Warrior;

    public override HitResult Hit(Enemy target)
    {
        // НЕ вызываем base.Hit — урон только через АОЕ
        if (!IsReady) return HitResult.Miss();

        // Анимация и cooldown
        if (Animator != null)
        {
            Animator.SetTrigger("Attack");
            Animator.SetFloat("AttackSpeed", _attackAnimationSpeed);
        }
        UpdateCooldown();

        ElementType element    = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier         = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        Enemy[] allEnemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        int count = 0;
        foreach (Enemy enemy in allEnemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > _bashRadius) continue;

            StatusController status = enemy.GetComponent<StatusController>();
            int finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
            bool killed = enemy.TakeDamage(finalDamage);

            if (!killed && element != ElementType.None && status != null)
            {
                StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
                status.ApplyStatus(statusToApply, finalDamage);
            }

            KnockbackReceiver knockback = enemy.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector3 dir = (enemy.transform.position - transform.position).normalized;
                dir = (dir + Vector3.forward).normalized;
                knockback.ApplyKnockback(dir, _knockbackForce, killed);
            }
            count++;
        }

        HitResult result = count > 0 
            ? new HitResult { Hit = true, Killed = false, DamageDealt = calc.FinalDamage }
            : HitResult.Miss();

        if (result.Hit)
        {
            Debug.Log($"[VFX] _vfxConfig={_vfxConfig}, heroType={_heroType}", this);
            if (_vfxConfig != null)
            {
                GameObject vfxPrefab = _heroType switch
                {
                    HeroType.Warrior => _vfxConfig.WarriorHitVfx,
                    HeroType.Tank    => _vfxConfig.TankHitVfx,
                    HeroType.Mage    => _vfxConfig.MageHitVfx,
                    HeroType.Archer  => _vfxConfig.ArcherHitVfx,
                    HeroType.Assassin => _vfxConfig.AssassinHitVfx,
                    _ => null
                };

                Debug.Log($"[VFX] vfxPrefab={vfxPrefab}, WarriorHitVfx={_vfxConfig.WarriorHitVfx}", this);

                if (vfxPrefab != null)
                {
                    Vector3 spawnPos = target.transform.position + Vector3.up * 0.5f;
                    GameObject.Instantiate(vfxPrefab, spawnPos, Quaternion.identity);
                }
            }
        }

        return result;
    }

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        return new DamageCalculation
        {
            FinalDamage     = _baseDamage * powerMultiplier,
            WasCritical     = false,
            LifestealAmount = 0,
        };
    }
}

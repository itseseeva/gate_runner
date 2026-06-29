using UnityEngine;

/// <summary>
/// Атака воина — АОЕ слеш в радиусе меча.
/// Два удара за одну анимацию через Animation Events.
/// </summary>
public class WarriorMeleeAttack : MeleeAutoAttackBase
{
    [Header("АОЕ слеш")]
    [Tooltip("Радиус АОЕ урона от слеша меча")]
    [Range(0.5f, 5f)]
    [SerializeField] private float _slashRadius = 1.5f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;

    [Tooltip("Смещение слеш-эффекта вперёд от героя")]
    [SerializeField] private float _slashForward = 1f;

    [Tooltip("Высота спавна слеш-эффекта")]
    [SerializeField] private float _slashHeight = 0.8f;


    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        // Только запускаем cooldown — урон через Animation Events
        UpdateCooldown();

        return new HitResult { Hit = true };
    }

    /// <summary>
    /// Вызывается через Animation Event в момент удара.
    /// Поставь два события на анимацию — для двух ударов меча.
    /// </summary>
    public void OnAttackHit()
    {

        ElementType element = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        Enemy[] allEnemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > _slashRadius) continue;

            StatusController status = enemy.GetComponent<StatusController>();
            int finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
            bool killed = enemy.TakeDamage(finalDamage);

            if (!killed && element != ElementType.None && status != null)
            {
                StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
                status.ApplyStatus(statusToApply, finalDamage);
            }
        }

        // VFX — базовый слеш только если нет стихийного
        if (VfxPool.Instance != null && _vfxConfig != null)
        {
            GameObject slashPrefab = _vfxConfig.GetWarriorSlash(element);
            Vector3 spawnPos = transform.position
                             + transform.forward * _slashForward
                             + Vector3.up * _slashHeight;
            Debug.Log($"[Slash] heroPos={transform.position}, forward={transform.forward}, spawnPos={spawnPos}");
            if (slashPrefab != null)
                VfxPool.Instance.Spawn(spawnPos, slashPrefab.transform.rotation, slashPrefab);
            else if (_vfxConfig.WarriorHitVfx != null)
                VfxPool.Instance.Spawn(spawnPos, Quaternion.identity, _vfxConfig.WarriorHitVfx);
        }
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

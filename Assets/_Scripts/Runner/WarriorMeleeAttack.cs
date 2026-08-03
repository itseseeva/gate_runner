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


    private bool _hasDealtDamage = false;

    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        UpdateCooldown(); // кулдаун стартует в момент, когда StrikeState принял удар
        _hasDealtDamage = false; // сбрасываем флаг для нового удара

        return new HitResult { Hit = true };
    }

    /// <summary>
    /// Вызывается через Animation Event в момент удара.
    /// У воина их 2 в анимации, но мы засчитываем только первый.
    /// </summary>
    public void OnAttackHit()
    {
        if (_hasDealtDamage) return; // Защита от двойного урона за один взмах
        _hasDealtDamage = true;

        // Кулдаун теперь стартует в Hit() — здесь только урон и VFX.

        ElementType element = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        var allEnemies = EnemyCombatBase.AllEnemies;

        for (int i = 0; i < allEnemies.Count; i++)
        {
            var combat = allEnemies[i];
            if (combat == null || !combat.gameObject.activeSelf) continue;

            Enemy enemy = combat.GetComponent<Enemy>();
            if (enemy == null) continue;

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
            {}
            if (slashPrefab != null)
                VfxPool.Instance.Spawn(spawnPos, slashPrefab.transform.rotation, slashPrefab);
            else if (_vfxConfig.WarriorHitVfx != null)
                VfxPool.Instance.Spawn(spawnPos, _vfxConfig.WarriorHitVfx.transform.rotation, _vfxConfig.WarriorHitVfx);
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

using UnityEngine;

/// <summary>
/// Автоатака ассасина: один удар с крит-шансом и лайфстилом.
/// Кулдаун атаки регулируется через Attack Cooldown (не Attack Speed).
/// </summary>
public class AssassinAutoAttack : MeleeAutoAttackBase
{
    [Header("Ассасин-специфичное")]
    [Range(0f, 1f)]
    [SerializeField] private float _critChance = 0.25f;

    [SerializeField] private float _critMultiplier = 2f;

    [Range(0f, 1f)]
    [SerializeField] private float _lifestealRatio = 0.20f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;

    [Tooltip("Смещение слеш-эффекта вперёд от героя")]
    [SerializeField] private float _slashForward = 0.1f;

    [Tooltip("Высота спавна слеш-эффекта")]
    [SerializeField] private float _slashHeight = 0.2f;

    [Header("Кулдаун атаки")]
    [Tooltip("Пауза между атаками в секундах")]
    [SerializeField] private float _attackCooldown = 4f;
    private float _lastAttackTime = -999f;

    public bool IsAttackReady => Time.time - _lastAttackTime >= _attackCooldown;

    public override HitResult Hit(Enemy target)
    {
        if (!IsAttackReady) return HitResult.Miss();
        Debug.Log($"[Assassin] Hit() target={target.name}, frame={Time.frameCount}", this);
        _lastAttackTime = Time.time;
        UpdateCooldown();
        return new HitResult { Hit = true };
    }

    /// <summary>Наносит один удар по цели + слеш-эффект.</summary>
    public void DoSingleSlash(Enemy target)
    {
        if (target == null) return;

        ElementType element  = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier       = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        StatusController status = target.GetComponent<StatusController>();
        int finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
        bool killed = target.TakeDamage(finalDamage);

        if (!killed && element != ElementType.None && status != null)
        {
            StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
            status.ApplyStatus(statusToApply, finalDamage);
        }

        // VFX — стихийный слеш если есть, иначе базовый AssassinHitVfx
        Debug.Log($"[Assassin VFX] element={element}, slashPrefab={_vfxConfig?.GetAssassinSlash(element)?.name ?? "NULL"}, hitVfx={_vfxConfig?.AssassinHitVfx?.name ?? "NULL"}, pos={transform.position}", this);
        if (VfxPool.Instance != null && _vfxConfig != null)
        {
            GameObject slashPrefab = _vfxConfig.GetAssassinSlash(element);
            Vector3 spawnPos = transform.position
                             + transform.forward * _slashForward
                             + Vector3.up * _slashHeight;

            if (slashPrefab != null)
                VfxPool.Instance.Spawn(spawnPos, slashPrefab.transform.rotation, slashPrefab);
            else if (_vfxConfig.AssassinHitVfx != null)
                VfxPool.Instance.Spawn(spawnPos, Quaternion.identity, _vfxConfig.AssassinHitVfx);
        }
    }

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        bool isCrit = Random.value < _critChance;
        int boostedDamage = _baseDamage * powerMultiplier;
        int finalDamage = isCrit
            ? Mathf.RoundToInt(boostedDamage * _critMultiplier)
            : boostedDamage;
        int lifesteal = Mathf.RoundToInt(finalDamage * _lifestealRatio);

        return new DamageCalculation
        {
            FinalDamage     = finalDamage,
            WasCritical     = isCrit,
            LifestealAmount = lifesteal,
        };
    }
}

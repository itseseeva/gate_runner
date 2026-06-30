using UnityEngine;

/// <summary>
/// Автоатака ассасина: удар через Animation Events.
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
    [SerializeField] private float _slashForward = 0.5f;

    [Tooltip("Высота спавна слеш-эффекта")]
    [SerializeField] private float _slashHeight = 0.5f;



    [Header("Кулдаун серии")]
    [Tooltip("Пауза между сериями ударов в секундах")]
    [SerializeField] private float _seriesCooldown = 2f;
    private float _lastSeriesTime = -999f;

    public bool IsSeriesReady => Time.time - _lastSeriesTime >= _seriesCooldown;
    public void StartSeriesCooldown() => _lastSeriesTime = Time.time;

    public override bool IsReady => base.IsReady && IsSeriesReady;

    private Enemy _currentTarget;
    public Enemy GetCurrentTarget() => _currentTarget;
    public void SetCurrentTarget(Enemy target) => _currentTarget = target;

    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();
        Debug.Log($"[Assassin] Hit() target={target.name}, frame={Time.frameCount}", this);
        return new HitResult { Hit = true };
    }

    // ── Animation Events ──
    public void OnAttackHit()
    {
        Debug.Log($"[Assassin] OnAttackHit triggered, frame={Time.frameCount}, target={(_currentTarget != null ? _currentTarget.name : "NULL")}", this);
        if (_currentTarget != null)
        {
            UpdateCooldown();
            StartSeriesCooldown();
            DoSingleSlash(_currentTarget);
            _currentTarget = null;
        }
    }

    /// <summary>Наносит удар по цели + VFX слеша.</summary>
    public void DoSingleSlash(Enemy target)
    {
        if (target == null) return;
        Debug.Log($"[Assassin] DoSingleSlash EXECUTED on {target.name}, frame={Time.frameCount}", this);

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

        // VFX
        if (VfxPool.Instance != null && _vfxConfig != null)
        {
            GameObject slashPrefab = _vfxConfig.GetAssassinSlash(element);
            Vector3 spawnPos = transform.position
                             + transform.forward * _slashForward
                             + Vector3.up * _slashHeight;

            if (slashPrefab != null)
            {
                // Умножаем ротацию героя на локальную ротацию префаба. 
                // Теперь поворот из префаба работает как отступ относительно того, куда смотрит герой.
                Quaternion spawnRot = transform.rotation * slashPrefab.transform.localRotation;
                VfxPool.Instance.Spawn(spawnPos, spawnRot, slashPrefab);
            }
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

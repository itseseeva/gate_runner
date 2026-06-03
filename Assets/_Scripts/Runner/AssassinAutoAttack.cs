using UnityEngine;

/// <summary>
/// Автоатака ассасина: серия из 3 ударов за одну анимацию по разным врагам.
/// Удары наносятся через Animation Events OnSlash1/2/3 в моменты взмахов.
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

    [Header("Кулдаун серии")]
    [Tooltip("Пауза между сериями ударов в секундах")]
    [SerializeField] private float _seriesCooldown = 2f;
    private float _lastSeriesTime = -999f;

    public bool IsSeriesReady => Time.time - _lastSeriesTime >= _seriesCooldown;
    public void StartSeriesCooldown() => _lastSeriesTime = Time.time;

    // Ссылка на состояние — оно даёт текущую цель для удара
    private AssassinStrikeState _strikeState;
    public void SetStrikeState(AssassinStrikeState state) => _strikeState = state;

    /// <summary>Запускает анимацию серии.</summary>
    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        if (Animator != null)
            Animator.SetTrigger("Attack");

        UpdateCooldown();
        return new HitResult { Hit = true };
    }

    // ── Animation Events — три взмаха ──
    public void OnSlash1() => DoSlashOnCurrentTarget();
    public void OnSlash2() => DoSlashOnCurrentTarget();
    public void OnSlash3() => DoSlashOnCurrentTarget();

    private void DoSlashOnCurrentTarget()
    {
        if (_strikeState == null) return;
        Enemy target = _strikeState.CurrentTarget;
        DoSingleSlash(target);
    }

    /// <summary>Наносит один удар по конкретной цели.</summary>
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

        if (VfxPool.Instance != null && _vfxConfig != null && _vfxConfig.AssassinHitVfx != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 0.5f;
            VfxPool.Instance.Spawn(spawnPos, Quaternion.identity, _vfxConfig.AssassinHitVfx);
        }

        Debug.Log($"[AssassinAutoAttack] Слэш по {target.name}, урон={finalDamage}" +
                  $"{(calc.WasCritical ? " КРИТ!" : "")}", this);
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

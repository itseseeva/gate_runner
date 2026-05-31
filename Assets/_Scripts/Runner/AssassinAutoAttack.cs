using UnityEngine;

/// <summary>
/// Автоатака ассасина: 3 удара за одну анимацию по разным врагам.
/// Крит = урон ×2, лайфстил = % от нанесённого урона.
/// </summary>
public class AssassinAutoAttack : MeleeAutoAttackBase
{
    [Header("Ассасин-специфичное")]
    [Range(0f, 1f)]
    [SerializeField] private float _critChance = 0.25f;

    [SerializeField] private float _critMultiplier = 2f;

    [Range(0f, 1f)]
    [SerializeField] private float _lifestealRatio = 0.20f;

    [Header("Мульти-удар")]
    [Tooltip("Радиус поиска врага для каждого из 3 ударов")]
    [SerializeField] private float _slashRadius = 3f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;

    /// <summary>
    /// Вызывается из MeleeAutoAttackBase — запускает анимацию.
    /// Урон наносится через Animation Events OnSlash1/2/3.
    /// </summary>
    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        if (Animator != null)
            Animator.SetTrigger("Attack");

        UpdateCooldown();
        return new HitResult { Hit = true };
    }

    /// <summary>Удар 1 — вызывается через Animation Event.</summary>
    public void OnSlash1() => DoSlash();

    /// <summary>Удар 2 — вызывается через Animation Event.</summary>
    public void OnSlash2() => DoSlash();

    /// <summary>Удар 3 — вызывается через Animation Event.</summary>
    public void OnSlash3() => DoSlash();

    private void DoSlash()
    {
        // Ищем ближайшего врага в радиусе который ещё жив
        Enemy target = FindNearestEnemy();
        if (target == null) return;

        ElementType element = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        StatusController status = target.GetComponent<StatusController>();
        int finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
        bool killed = target.TakeDamage(finalDamage);

        if (!killed && element != ElementType.None && status != null)
        {
            StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
            status.ApplyStatus(statusToApply, finalDamage);
        }

        // VFX на позиции врага
        if (VfxPool.Instance != null && _vfxConfig != null && _vfxConfig.AssassinHitVfx != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 0.5f;
            VfxPool.Instance.Spawn(spawnPos, Quaternion.identity, _vfxConfig.AssassinHitVfx);
        }

        Debug.Log($"[AssassinAutoAttack] Слэш по {target.name}, урон={finalDamage}" +
                  $"{(calc.WasCritical ? " КРИТ!" : "")}", this);
    }

    /// <summary>
    /// Публичный метод для вызова из AssassinStrikeState.
    /// Наносит один удар по конкретной цели.
    /// </summary>
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

        Debug.Log($"[AssassinAutoAttack] DoSingleSlash по {target.name}, урон={finalDamage}" +
                  $"{(calc.WasCritical ? " КРИТ!" : "")}", this);
    }

    private Enemy FindNearestEnemy()
    {
        Enemy[] all = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        // Собираем всех врагов в радиусе
        System.Collections.Generic.List<Enemy> candidates = new();
        foreach (Enemy e in all)
        {
            if (!e.gameObject.activeSelf) continue;
            float dist = Vector3.Distance(transform.position, e.transform.position);
            if (dist < _slashRadius) candidates.Add(e);
        }

        if (candidates.Count == 0) return null;

        // Возвращаем случайного — каждый удар бьёт другого
        return candidates[Random.Range(0, candidates.Count)];
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

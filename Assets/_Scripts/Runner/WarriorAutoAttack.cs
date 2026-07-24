using UnityEngine;

/// <summary>
/// Атака воина/танка — АОЕ урон + отталкивание + слеш-эффект по стихии.
/// Танк: удар через TankContactAttack (физика) → ApplyTankHit().
/// Воин: урон и слеш через Animation Event (OnAttackHit).
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    [Header("АОЕ удар")]
    [SerializeField] private float _bashRadius = 3f;

    [Range(0f, 1f)]
    [SerializeField] private float _knockbackForce = 0.4f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;
    [SerializeField] private HeroType  _heroType = HeroType.Warrior;

    public VfxConfig GetVfxConfig() => _vfxConfig;

    private Enemy _currentTarget;
    private int   _lastHitFrame = -1;

    // ─────────────────────────────────────────────────────────────────
    // IUnitAttack: используется воинами через AutoAttacker
    // ─────────────────────────────────────────────────────────────────

    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        _currentTarget = target;

        if (Animator != null)
            Animator.SetTrigger("Attack");

        UpdateCooldown();

        return new HitResult { Hit = true };
    }

    // ─────────────────────────────────────────────────────────────────
    // Танк: мгновенный удар при физическом касании (TankContactAttack)
    // ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Вызывается из TankContactAttack. Наносит AOE урон, knockback и VFX
    /// мгновенно. Animation Event (OnAttackHit) для танка игнорируется.
    /// </summary>
    public void ApplyTankHit(Enemy primaryTarget)
    {
        _currentTarget = primaryTarget;

        ElementType       element    = OwnerUnit  != null ? OwnerUnit.Element         : ElementType.None;
        int               multiplier = OwnerUnit  != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc       = CalculateDamage(multiplier);

        // ── Урон только по одной цели (без АОЕ) ──
        if (primaryTarget != null)
        {
            StatusController status      = primaryTarget.GetComponent<StatusController>();
            int              finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
            bool             killed      = primaryTarget.TakeDamage(finalDamage);

            if (!killed && element != ElementType.None && status != null)
            {
                StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
                status.ApplyStatus(statusToApply, finalDamage);
            }

            KnockbackReceiver knockback = primaryTarget.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                // Отбрасываем исходя из текущего угла поворота танка
                // на случайное расстояние от 3 до 5 метров
                float distance = Random.Range(3f, 5f);
                
                // Берём направление, куда смотрит танк, но сильно "режем" боковой отлёт (по X),
                // чтобы враги летели красиво НАЗАД по трассе (+Z), а не улетали за край экрана.
                Vector3 pushDir = transform.forward;
                pushDir.y = 0f;
                pushDir.x *= 0.25f; 
                
                knockback.ApplyKnockback(pushDir.normalized, distance, killed);
            }
        }

        // ── Слеш-эффект по стихии ──
        if (_vfxConfig != null && VfxPool.Instance != null)
        {
            GameObject slashPrefab = _vfxConfig.GetTankSlash(element);
            if (slashPrefab != null)
                VfxPool.Instance.Spawn(transform.position, transform.rotation, slashPrefab);
        }

        // ── HitVfx спереди танка (только без элемента — со стихией слеш перекрывает) ──
        bool hasElement = element != ElementType.None;
        if (!hasElement && VfxPool.Instance != null && _vfxConfig != null && _vfxConfig.TankHitVfx != null)
            VfxPool.Instance.Spawn(transform.position + Vector3.up * 0.5f, Quaternion.identity, _vfxConfig.TankHitVfx);
    }

    // ─────────────────────────────────────────────────────────────────
    // Animation Event — только для воина, танк игнорирует
    // ─────────────────────────────────────────────────────────────────

    public void OnAttackHit()
    {
        // Танк бьёт через ApplyTankHit — Animation Event не нужен
        if (_heroType == HeroType.Tank) return;

        if (_lastHitFrame == Time.frameCount) return;
        _lastHitFrame = Time.frameCount;

        ElementType       element    = OwnerUnit != null ? OwnerUnit.Element         : ElementType.None;
        int               multiplier = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc       = CalculateDamage(multiplier);

        // ── Слеш-эффект ──
        if (_vfxConfig != null && VfxPool.Instance != null)
        {
            GameObject slashPrefab = _vfxConfig.GetWarriorSlash(element);
            if (slashPrefab != null)
                VfxPool.Instance.Spawn(transform.position, transform.rotation, slashPrefab);
        }

        // ── AOE урон ──
        Enemy[] allEnemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (Enemy enemy in allEnemies)
        {
            float dist = Vector3.Distance(transform.position, enemy.transform.position);
            if (dist > _bashRadius) continue;

            StatusController status      = enemy.GetComponent<StatusController>();
            int              finalDamage = DamageCalculator.CalculateFinalDamage(calc.FinalDamage, element, status);
            bool             killed      = enemy.TakeDamage(finalDamage);

            if (!killed && element != ElementType.None && status != null)
            {
                StatusEffectType statusToApply = DamageCalculator.GetStatusFromElement(element);
                status.ApplyStatus(statusToApply, finalDamage);
            }

            KnockbackReceiver knockback = enemy.GetComponent<KnockbackReceiver>();
            if (knockback != null)
            {
                Vector3 dir = enemy.transform.position - transform.position;
                dir.y = 0f;
                dir = (dir.normalized + Vector3.forward).normalized;
                knockback.ApplyKnockback(dir, _knockbackForce, killed);
            }
        }

        // ── HitVfx ──
        if (VfxPool.Instance != null && _vfxConfig != null && _vfxConfig.WarriorHitVfx != null)
            VfxPool.Instance.Spawn(transform.position + Vector3.up * 0.5f, Quaternion.identity, _vfxConfig.WarriorHitVfx);
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

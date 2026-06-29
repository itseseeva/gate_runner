using UnityEngine;
using DG.Tweening;

/// <summary>
/// Атака воина/танка — АОЕ урон + отталкивание + слеш-эффект по стихии.
/// Урон и слеш спавнятся через Animation Event (OnAttackHit) в момент удара.
/// </summary>
public class WarriorAutoAttack : MeleeAutoAttackBase
{
    [Header("АОЕ удар щитом")]
    [SerializeField] private float _bashRadius = 3f;

    [Range(0f, 1f)]
    [SerializeField] private float _knockbackForce = 0.4f;

    [SerializeField] private float _attackAnimationSpeed = 1f;

    [Header("Выпад щитом")]
    [Tooltip("На сколько танк делает рывок вперёд при ударе")]
    [SerializeField] private float _lungeDistance = 0.6f;

    [Tooltip("Длительность рывка туда-обратно (сек)")]
    [SerializeField] private float _lungeDuration = 0.2f;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;
    [SerializeField] private HeroType _heroType = HeroType.Warrior;

    public VfxConfig GetVfxConfig() => _vfxConfig;

    // Флаг — разрешён ли урон (выставляется через Animation Event)
    private bool _canDealDamage = false;

    public override HitResult Hit(Enemy target)
    {
        if (!IsReady) return HitResult.Miss();

        if (Animator != null)
            Animator.SetTrigger("Attack");

        UpdateCooldown();
        return new HitResult { Hit = true };
    }

    /// <summary>
    /// Вызывается через Animation Event в момент удара.
    /// Наносит АОЕ урон + спавнит слеш-эффект по текущей стихии.
    /// </summary>
    public void OnAttackHit()
    {
        // Короткий выпад вперёд в момент удара (только для Tank)
        if (_heroType == HeroType.Tank && _lungeDistance > 0f)
        {
            Vector3 lungeTarget = transform.position + Vector3.forward * _lungeDistance;
            transform.DOMove(lungeTarget, _lungeDuration * 0.5f)
                     .SetEase(Ease.OutQuad)
                     .OnComplete(() =>
                         transform.DOMove(
                             transform.position - Vector3.forward * _lungeDistance,
                             _lungeDuration * 0.5f).SetEase(Ease.InQuad));
        }

        ElementType element    = OwnerUnit != null ? OwnerUnit.Element : ElementType.None;
        int multiplier         = OwnerUnit != null ? OwnerUnit.PowerMultiplier : 1;
        DamageCalculation calc = CalculateDamage(multiplier);

        // ── Слеш-эффект на герое по стихии и типу героя ──
        if (_vfxConfig != null && VfxPool.Instance != null)
        {
            GameObject slashPrefab = _heroType == HeroType.Tank
                ? _vfxConfig.GetTankSlash(element)
                : _vfxConfig.GetWarriorSlash(element);

            Debug.Log($"[WarriorAutoAttack] element={element}, heroType={_heroType}, " +
                      $"slashPrefab={( slashPrefab != null ? slashPrefab.name : "NULL")}", this);

            if (slashPrefab != null)
                VfxPool.Instance.Spawn(transform.position, transform.rotation, slashPrefab);
        }
        else
        {
            Debug.LogWarning($"[WarriorAutoAttack] Слеш не спавнится! " +
                             $"_vfxConfig={_vfxConfig}, VfxPool={VfxPool.Instance}", this);
        }

        // ── АОЕ урон по всем врагам в радиусе ──
        Enemy[] allEnemies = FindObjectsByType<Enemy>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);

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
        }

        // ── Эффект попадания на позиции героя (HitVfx) ──
        if (VfxPool.Instance != null && _vfxConfig != null)
        {
            GameObject vfxPrefab = _heroType == HeroType.Tank
                ? _vfxConfig.TankHitVfx
                : _vfxConfig.WarriorHitVfx;

            if (vfxPrefab != null)
                VfxPool.Instance.Spawn(transform.position + Vector3.up * 0.5f, Quaternion.identity, vfxPrefab);
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

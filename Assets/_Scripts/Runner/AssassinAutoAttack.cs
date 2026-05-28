using UnityEngine;

/// <summary>
/// Автоатака ассасина: шанс крита + лайфстил.
/// Крит = урон ×2, лайфстил = % от нанесённого урона восстанавливается.
/// </summary>
public class AssassinAutoAttack : MeleeAutoAttackBase
{
    [Header("Ассасин-специфичное")]
    [Tooltip("Шанс крита (0 = 0%, 1 = 100%)")]
    [Range(0f, 1f)]
    [SerializeField] private float _critChance = 0.25f;

    [Tooltip("Множитель урона при крите")]
    [SerializeField] private float _critMultiplier = 2f;

    [Tooltip("Доля нанесённого урона возвращается как HP (0.2 = 20%)")]
    [Range(0f, 1f)]
    [SerializeField] private float _lifestealRatio = 0.20f;

    public override HitResult Hit(Enemy target)
    {
        HitResult result = base.Hit(target);
        if (result.Hit && VfxPool.Instance != null)
        {
            Vector3 spawnPos = target.transform.position + Vector3.up * 0.5f;
            VfxPool.Instance.Spawn(spawnPos, Quaternion.identity);
        }
        return result;
    }

    protected override DamageCalculation CalculateDamage(int powerMultiplier)
    {
        bool isCrit = Random.value < _critChance;

        // Множитель применяется ДО крита — крит считается от усиленного урона
        int boostedDamage = _baseDamage * powerMultiplier;
        int finalDamage   = isCrit
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

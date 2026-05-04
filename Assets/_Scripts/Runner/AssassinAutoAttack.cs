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

    protected override DamageCalculation CalculateDamage()
    {
        // Проверяем крит
        bool isCrit = Random.value < _critChance;

        // Считаем урон
        int finalDamage = isCrit
            ? Mathf.RoundToInt(_baseDamage * _critMultiplier)
            : _baseDamage;

        // Лайфстил
        int lifesteal = Mathf.RoundToInt(finalDamage * _lifestealRatio);

        return new DamageCalculation
        {
            FinalDamage     = finalDamage,
            WasCritical     = isCrit,
            LifestealAmount = lifesteal,
        };
    }
}

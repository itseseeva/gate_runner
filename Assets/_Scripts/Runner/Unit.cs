using UnityEngine;

/// <summary>
/// Компонент одного юнита в отряде.
/// Знает свой тип, тир, HP и множитель силы.
/// НЕ знает о движении и формации.
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("Состояние")]
    [SerializeField] private int _currentHP;

    private HeroDefinitionSO _data;
    private UnitTier         _tier = UnitTier.T1;
    private int              _powerMultiplier = 1;

    public HeroType         HeroType         => _data != null ? _data.HeroType : HeroType.Warrior;
    public UnitTier         Tier             => _tier;
    public int              CurrentHP        => _currentHP;
    public int              PowerMultiplier  => _powerMultiplier;
    public HeroDefinitionSO Data             => _data;

    /// <summary>
    /// Инициализирует юнита по данным из ScriptableObject + указывает тир.
    /// Вызывается сразу после спавна из пула.
    /// </summary>
    public void Initialize(HeroDefinitionSO data, UnitTier tier = UnitTier.T1)
    {
        _data = data;
        _tier = tier;
        _powerMultiplier = 1;
        _currentHP = data.MaxHP * _powerMultiplier;
    }

    /// <summary>
    /// Увеличивает множитель силы. Используется когда T2 уже есть и приходит ещё одно слияние.
    /// HP при этом не пересчитывается (рост HP — это отдельный вопрос дизайна, обсудим).
    /// </summary>
    public void IncrementPowerMultiplier()
    {
        _powerMultiplier++;
        // HP можем дать рост пропорционально — но это решим в фазе 2.3
    }

    /// <summary>
    /// При смерти T2-юнита с множителем — теряем 1 множитель за удар.
    /// Если множитель упал до 1 (или ниже) — юнит реально умирает.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        _currentHP -= amount;

        if (_currentHP <= 0)
        {
            // Если есть запас множителя — теряем его, а не умираем
            if (_powerMultiplier > 1)
            {
                _powerMultiplier--;
                _currentHP = _data.MaxHP; // восстанавливаем HP до полной шкалы
                return false;
            }

            _currentHP = 0;
            return true;
        }
        return false;
    }
}
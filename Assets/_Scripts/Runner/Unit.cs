using UnityEngine;

/// <summary>
/// Компонент одного юнита в отряде.
/// Знает свой тип, HP и данные из SO.
/// НЕ знает о движении и формации.
/// </summary>
public class Unit : MonoBehaviour
{
    [Header("Состояние")]
    [SerializeField] private int _currentHP;

    private HeroDefinitionSO _data;

    public HeroType          HeroType    => _data != null ? _data.HeroType : HeroType.Warrior;
    public int               CurrentHP   => _currentHP;
    public HeroDefinitionSO  Data        => _data;

    /// <summary>
    /// Инициализирует юнита по данным из ScriptableObject.
    /// Вызывается сразу после спавна из пула.
    /// </summary>
    public void Initialize(HeroDefinitionSO data)
    {
        _data      = data;
        _currentHP = data.MaxHP;
        Debug.Log($"[Unit] Инициализирован: {data.HeroName} | HP: {_currentHP}", this);
    }

    /// <summary>
    /// Получает урон. Возвращает true если юнит погиб.
    /// </summary>
    public bool TakeDamage(int amount)
    {
        _currentHP -= amount;
        Debug.Log($"[Unit] {_data.HeroName} получил {amount} урона. HP: {_currentHP}", this);

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Debug.Log($"[Unit] {_data.HeroName} погиб!", this);
            return true;
        }
        return false;
    }
}
using UnityEngine;

/// <summary>
/// Простой враг. Стоит на месте, имеет HP.
/// Погибает когда HP = 0.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField] private int   _maxHP    = 50;
    [SerializeField] private bool  _isBoss   = false;
    [SerializeField] private float _scale    = 1f;

    private int _currentHP;

    public bool IsBoss => _isBoss;

    private void OnEnable()
    {
        _currentHP = _maxHP;
        transform.localScale = Vector3.one * _scale;
    }

    /// <summary>Получает урон. Возвращает true если погиб.</summary>
    public bool TakeDamage(int amount)
    {
        _currentHP -= amount;
        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Die();
            return true;
        }
        return false;
    }

    private void Die()
    {
        Debug.Log($"[Enemy] {gameObject.name} погиб!", this);
        gameObject.SetActive(false);
    }
}

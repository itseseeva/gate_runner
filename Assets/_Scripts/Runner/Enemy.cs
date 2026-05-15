using UnityEngine;

/// <summary>
/// HP-компонент врага. Хранит здоровье и умирает при HP=0.
/// Атака — отдельным компонентом (EnemyMeleeAttack).
/// </summary>
public class Enemy : MonoBehaviour
{
    public static event System.Action<Enemy> OnAnyEnemyDied;
    [Header("Данные врага")]
    [SerializeField] private EnemyDefinitionSO _data;

    [Header("UI")]
    [SerializeField] private HealthBar _healthBar;

    [Header("Отладка (только для чтения)")]
    [SerializeField] private int _currentHP;

    private bool _isDead = false;
    private int  _maxHP;

    public bool IsBoss => false; // TODO: вынести в EnemyDefinitionSO когда будем делать боссов

    private void OnEnable()
    {
        _isDead    = false;
        _maxHP     = _data != null ? _data.MaxHP : 50;
        _currentHP = _maxHP;

        // Сообщаем бару полное HP — он скроется автоматически
        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);
    }

    /// <summary>
    /// Применяет множитель к Max HP. Вызывается LevelGenerator-ом при спавне.
    /// </summary>
    public void ApplyHealthMultiplier(float multiplier)
    {
        if (_data == null) return;

        int boostedHP = Mathf.RoundToInt(_data.MaxHP * multiplier);
        _maxHP = boostedHP;
        _currentHP = boostedHP;

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);
    }

    /// <summary>Получает урон. Возвращает true если погиб.</summary>
    public bool TakeDamage(int amount)
    {
        if (_isDead) return false;

        _currentHP -= amount;
        
        // Спавн цифры урона над врагом
        if (DamageNumberPool.Instance != null)
        {
            Vector3 spawnPos = transform.position + Vector3.up * 1.5f;
            DamageNumberPool.Instance.Spawn(amount, spawnPos, false);
        }

        Debug.Log($"[Enemy] {gameObject.name} получил {amount} урона. HP={_currentHP}", this);

        // Обновляем бар
        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, _maxHP);

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
        _isDead = true;
        Debug.Log($"[Enemy] {gameObject.name} погиб!", this);

        OnAnyEnemyDied?.Invoke(this);

        gameObject.SetActive(false);
    }
}

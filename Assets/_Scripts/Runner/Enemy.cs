using UnityEngine;

/// <summary>
/// Простой melee-враг. Стоит на месте, бьёт ближайшего юнита в радиусе.
/// Данные читает из EnemyDefinitionSO.
/// </summary>
public class Enemy : MonoBehaviour
{
    [Header("Данные врага")]
    [SerializeField] private EnemyDefinitionSO _data;

    [Header("Отладка (только для чтения)")]
    [SerializeField] private int _currentHP;

    private float _lastAttackTime = -999f;
    private bool  _isDead         = false;

    public bool IsBoss => false; // TODO: вынести в EnemyDefinitionSO когда будем делать боссов

    // ─── Инициализация ───────────────────────────────────────────

    private void OnEnable()
    {
        _isDead = false;

        // Если SO подключён — берём HP оттуда. Если нет — дефолт 50.
        _currentHP = _data != null ? _data.MaxHP : 50;

        Debug.Log($"[Enemy] {gameObject.name} появился. HP={_currentHP}", this);
    }

    // ─── Атака ───────────────────────────────────────────────────

    private void Update()
    {
        if (_isDead) return;
        if (_data == null) return;

        // Проверяем cooldown атаки
        float cooldown = 1f / _data.AttackSpeed;
        if (Time.time - _lastAttackTime < cooldown) return;

        // Ищем ближайшего юнита в радиусе
        Unit target = FindNearestUnit(_data.AttackRange);
        if (target == null) return;

        // Бьём!
        AttackUnit(target);
    }

    /// <summary>Ищет ближайшего живого юнита в радиусе.</summary>
    private Unit FindNearestUnit(float range)
    {
        // TODO: День 7 — заменить на кэшированный список юнитов
        Unit[] allUnits = FindObjectsByType<Unit>(FindObjectsSortMode.None);

        Unit  nearest = null;
        float minDist = range;

        foreach (Unit u in allUnits)
        {
            if (!u.gameObject.activeSelf) continue;

            float dist = Vector3.Distance(transform.position, u.transform.position);
            if (dist < minDist)
            {
                minDist  = dist;
                nearest  = u;
            }
        }

        return nearest;
    }

    /// <summary>Наносит урон юниту. Если юнит умер — сообщаем SquadController.</summary>
    private void AttackUnit(Unit target)
    {
        _lastAttackTime = Time.time;

        bool killed = target.TakeDamage(_data.Damage);

        Debug.Log($"[Enemy] {gameObject.name} ударил {target.gameObject.name} " +
                  $"на {_data.Damage} урона. Убит={killed}", this);

        if (killed)
        {
            Debug.Log($"[Enemy] Юнит {target.gameObject.name} погиб!", this);

            // Сообщаем отряду — он уберёт юнита из формации и вернёт в пул
            SquadController squad = FindAnyObjectByType<SquadController>();
            if (squad != null)
                squad.OnUnitDied(target);
        }
    }

    // ─── Получение урона ─────────────────────────────────────────

    /// <summary>Получает урон. Возвращает true если погиб.</summary>
    public bool TakeDamage(int amount)
    {
        if (_isDead) return false;

        _currentHP -= amount;
        Debug.Log($"[Enemy] {gameObject.name} получил {amount} урона. " +
                  $"HP={_currentHP}/{(_data != null ? _data.MaxHP : 50)}", this);

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
        gameObject.SetActive(false);
    }
}

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

    [Header("UI")]
    [SerializeField] private HealthBar _healthBar;

    [Header("Регенерация")]
    [Tooltip("Через сколько секунд после последнего удара начинается регенерация")]
    [SerializeField] private float _regenDelay = 3f;

    [Tooltip("За сколько секунд восстанавливается до полного HP")]
    [SerializeField] private float _regenTimeToFull = 3f;

    private HeroDefinitionSO _data;
    private UnitTier         _tier = UnitTier.T1;
    private int              _powerMultiplier = 1;
    private float            _regenAccumulator = 0f;
    private float            _lastDamageTime  = -999f;
    private bool             _wasRegenerating = false;

    public HeroType         HeroType         => _data != null ? _data.HeroType : HeroType.Warrior;
    public UnitTier         Tier             => _tier;
    public int              CurrentHP        => _currentHP;
    public int              PowerMultiplier  => _powerMultiplier;
    public HeroDefinitionSO Data             => _data;

    /// <summary>
    /// Инициализирует юнита по данным из ScriptableObject + указывает тир.
    /// Вызывается сразу после спавна из пула.
    /// </summary>
    public void Initialize(HeroDefinitionSO data, UnitTier tier = UnitTier.T1, int multiplier = 1)
    {
        _data = data;
        _tier = tier;
        _powerMultiplier = multiplier;
        _currentHP = data.MaxHP * multiplier;
        _lastDamageTime = -999f;
        _regenAccumulator = 0f;

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, data.MaxHP * multiplier);
    }

    /// <summary>
    /// Увеличивает множитель силы. Используется при +юниты от ворот.
    /// </summary>
    public void IncrementPowerMultiplier()
    {
        _powerMultiplier++;
    }

    /// <summary>
    /// Получает урон. Возвращает true если погиб.
    /// HP — общий пул всего пакета T2 (multiplier × MaxHP_T1).
    /// </summary>
    public bool TakeDamage(int amount)
    {
        int hpBefore = _currentHP;
        _currentHP -= amount;
        _lastDamageTime = Time.time;

        int maxHP = _data.MaxHP * _powerMultiplier;

        Debug.Log($"[Unit] {gameObject.name} получил {amount} урона. " +
                  $"HP: {hpBefore} → {_currentHP} / {maxHP} (multiplier={_powerMultiplier})", this);

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, maxHP);

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            Debug.Log($"[Unit] {gameObject.name} ПОГИБ!", this);
            return true;
        }

        return false;
    }

    private void Update()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying)
            return;

        if (_data == null) return;

        bool canRegen = (Time.time - _lastDamageTime >= _regenDelay);
        int maxHP = _data.MaxHP * _powerMultiplier;
        bool needsRegen = _currentHP < maxHP;

        // Лог когда регенерация только начинается
        if (canRegen && needsRegen && !_wasRegenerating)
        {
            _wasRegenerating = true;
            Debug.Log($"[Unit] {gameObject.name} начал регенерацию. HP: {_currentHP}/{maxHP}", this);
        }

        // Лог когда регенерация закончилась (HP=max или пришёл удар)
        if (_wasRegenerating && (!canRegen || !needsRegen))
        {
            _wasRegenerating = false;
            if (_currentHP >= maxHP)
                Debug.Log($"[Unit] {gameObject.name} полностью восстановился. HP: {maxHP}", this);
        }

        // Не регенимся
        if (!canRegen) return;
        if (!needsRegen) return;

        // ... остальная регенерация без изменений
        float regenPerSecond = maxHP / _regenTimeToFull;
        _regenAccumulator   += regenPerSecond * Time.deltaTime;

        if (_regenAccumulator >= 1f)
        {
            int wholeAmount = Mathf.FloorToInt(_regenAccumulator);
            _regenAccumulator -= wholeAmount;

            _currentHP = Mathf.Min(_currentHP + wholeAmount, maxHP);

            if (_healthBar != null)
                _healthBar.SetHP(_currentHP, maxHP);
        }
    }
}
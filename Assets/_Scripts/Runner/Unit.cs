using UnityEngine;
using DG.Tweening;

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

    [Header("Визуализация (Best Practice)")]
    [Tooltip("Сюда перетаскиваем Renderer куба или SkinnedMeshRenderer гномика")]
    [SerializeField] private Renderer _unitRenderer;

    [Header("Регенерация")]
    [Tooltip("Через сколько секунд после последнего удара начинается регенерация")]
    [SerializeField] private float _regenDelay = 3f;

    [Tooltip("За сколько секунд восстанавливается до полного HP")]
    [SerializeField] private float _regenTimeToFull = 3f;

    private HeroDefinitionSO _data;
    private UnitTier         _tier = UnitTier.T1;
    private int              _powerMultiplier = 1;
    private ElementType _element = ElementType.None;
    public  ElementType Element => _element;

    public bool IsDead { get; private set; } = false;

    [Header("VFX")]
    [SerializeField] private VfxConfig _vfxConfig;

    private Vector3 _initialScale;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        IsDead = false;
    }

    /// <summary>
    /// Меняет стихию юнита. Вызывается ElementGate-ом для всего отряда.
    /// Опционально меняет цвет материала для визуальной индикации.
    /// </summary>
    public void SetElement(ElementType element)
    {
        _element = element;
        UpdateVisualForElement();
    }

    private void UpdateVisualForElement()
    {
        // Никаких GetComponentInChildren! Используем готовую ссылку
        if (_unitRenderer == null) return;

        Color color = _element switch
        {
            ElementType.Fire      => new Color(1f, 0.4f, 0.1f),  
            ElementType.Ice       => new Color(0.4f, 0.8f, 1f),  
            ElementType.Lightning => new Color(1f, 0.95f, 0.3f), 
            _                     => Color.white,                 
        };

        Material mat = _unitRenderer.material;
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        else
            mat.color = color;
    }
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
            IsDead = true;
            
            var warriorAttack = GetComponent<WarriorAutoAttack>();
            VfxConfig vfx = warriorAttack != null ? warriorAttack.GetVfxConfig() : null;
            
            Debug.Log($"[Unit] warriorAttack={warriorAttack}, vfx={vfx}, UnitDeathVfx={vfx?.UnitDeathVfx}, VfxPool={VfxPool.Instance}", this);
            
            if (vfx != null && vfx.UnitDeathVfx != null && VfxPool.Instance != null)
                VfxPool.Instance.Spawn(transform.position, Quaternion.identity, vfx.UnitDeathVfx);
            
            return true;
        }

        return false;
    }

    /// <summary>
    /// Анимация смерти: отлёт + масштабирование через DOTween.
    /// Юнит визуально умирает, потом SquadController вернёт его в пул.
    /// </summary>
    private void PlayDeathAnimation()
    {
        IsDead = true; // ← SquadController больше не двигает этого юнита
        Debug.Log($"[Unit] PlayDeathAnimation START на {gameObject.name}", this);

        // Отключаем компонент движения чтобы SquadController не перезаписывал позицию
        var worldScroller = GetComponent<WorldScroller>();
        if (worldScroller != null) worldScroller.enabled = false;

        var melee = GetComponent<MeleeUnitController>();
        if (melee != null) melee.enabled = false;

        Vector3 flyDir = new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, -1f).normalized;
        Vector3 initialScale = transform.localScale;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(transform.position + flyDir * 2f, 0.5f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InQuad));
        seq.OnComplete(() =>
        {
            Debug.Log($"[Unit] PlayDeathAnimation COMPLETE на {gameObject.name}", this);
            transform.localScale = initialScale;
        });
    }

    /// <summary>
    /// Восстанавливает визуал после смерти — вызывается при возврате в пул.
    /// </summary>
    public void ResetVisual()
    {
        IsDead = false; // ← оживляем для пула
        transform.localScale = _initialScale;

        var worldScroller = GetComponent<WorldScroller>();
        if (worldScroller != null) worldScroller.enabled = true;

        var melee = GetComponent<MeleeUnitController>();
        if (melee != null) melee.enabled = true;

        if (_unitRenderer != null)
        {
            Material mat = _unitRenderer.material;
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = 1f;
                mat.SetColor("_BaseColor", c);
            }
        }
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
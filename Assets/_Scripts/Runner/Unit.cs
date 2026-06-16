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

    [Header("Ауры стихий на герое (префабы)")]
    [Tooltip("Аура огня — включается при Fire")]
    [SerializeField] private GameObject _fireAuraPrefab;

    [Tooltip("Аура льда — включается при Ice")]
    [SerializeField] private GameObject _iceAuraPrefab;

    [Tooltip("Аура молнии — включается при Lightning")]
    [SerializeField] private GameObject _lightningAuraPrefab;

    // Ленивые экземпляры аур (создаются один раз при первой смене на эту стихию)
    private GameObject _fireAura;
    private GameObject _iceAura;
    private GameObject _lightningAura;

    private Vector3 _initialScale;

    private void Awake()
    {
        _initialScale = transform.localScale;
    }

    private void OnEnable()
    {
        IsDead = false;
        _element = ElementType.None;
        UpdateAuras(); // гасим все ауры при выдаче из пула
    }

    /// <summary>
    /// Меняет стихию юнита. Вызывается ElementGate-ом для всего отряда.
    /// Опционально меняет цвет материала для визуальной индикации.
    /// </summary>
    public void SetElement(ElementType element)
    {
        _element = element;
        UpdateAuras();
    }

    /// <summary>
    /// Включает ауру текущей стихии, выключает остальные.
    /// Аура постоянна, пока не сменится стихия (в отличие от временных статусов врага).
    /// </summary>
    private void UpdateAuras()
    {
        SetAura(ref _fireAura,      _fireAuraPrefab,      _element == ElementType.Fire);
        SetAura(ref _iceAura,       _iceAuraPrefab,       _element == ElementType.Ice);
        SetAura(ref _lightningAura, _lightningAuraPrefab, _element == ElementType.Lightning);
    }

    /// <summary>
    /// Вкл/выкл ауру. Создаёт из префаба лениво — только при первом включении.
    /// </summary>
    private void SetAura(ref GameObject instance, GameObject prefab, bool active)
    {
        if (!active && instance == null) return;
        if (prefab == null) return;

        if (instance == null)
        {
            instance = Instantiate(prefab, transform);
            instance.transform.localPosition = prefab.transform.localPosition;
        }

        if (instance.activeSelf != active)
        {
            instance.SetActive(active);
            if (active) PlayDesynced(instance);
        }
    }

    /// <summary>
    /// Запускает партиклы эффекта со случайного кадра,
    /// чтобы у толпы эффекты не были синхронными.
    /// </summary>
    private void PlayDesynced(GameObject effect)
    {
        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            ps.Clear(true);
            float randomOffset = Random.Range(0f, ps.main.duration);
            ps.Simulate(randomOffset, true, true);
            ps.Play(true);
        }
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

        // Динамически вешаем AnimationEventReceiver на всех детей с Animator, если его там нет.
        // Делаем это в Initialize (сразу после Instantiate), чтобы не вызывать ошибок сериализации инспектора в Awake().
        foreach (var anim in GetComponentsInChildren<Animator>(true))
        {
            if (anim.gameObject.GetComponent<AnimationEventReceiver>() == null)
            {
                anim.gameObject.AddComponent<AnimationEventReceiver>();
            }
        }
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

    // Заглушки для Animation Events (звуки шагов)
    public void FootL() { }
    public void FootR() { }
}
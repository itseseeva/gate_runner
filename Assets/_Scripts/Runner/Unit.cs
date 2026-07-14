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
    [SerializeField] private Transform _damageNumberAnchor;
    [Tooltip("На какой высоте спавнить цифру (у врагов по умолчанию 1.8)")]
    [SerializeField] private float _damageNumberHeight = 1.0f;

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
    private int _prefabMaxHP = 0;

    private void Awake()
    {
        _initialScale = transform.localScale;
        // Сохраняем значение из префаба. Если оно > 0, оно будет в приоритете над SO.
        _prefabMaxHP = _currentHP;
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
            // ── ДО INSTANTIATE: что внутри префаба ──
            Debug.Log($"═══════ [Aura Diag] СПАВН AURА для {name} ═══════", this);
            Debug.Log($"[Prefab] {prefab.name}: pos={prefab.transform.position:F3}, " +
                      $"localScale={prefab.transform.localScale:F3}", prefab);

            foreach (Transform child in prefab.transform)
            {
                Debug.Log($"[Prefab Child] {child.name}: " +
                          $"localPos={child.localPosition:F3}, " +
                          $"localScale={child.localScale:F3}", prefab);
            }

            Debug.Log($"[Hero] {name}: worldPos={transform.position:F3}, " +
                      $"localScale={transform.localScale:F3}, " +
                      $"lossyScale={transform.lossyScale:F3}", this);

            // ── INSTANTIATE ──
            instance = Instantiate(prefab, transform);
            instance.transform.localPosition = Vector3.zero;

            // ── ПОСЛЕ INSTANTIATE: что стало с аурой ──
            string parentName = instance.transform.parent != null ? instance.transform.parent.name : "NULL";
            Debug.Log($"[Aura After Spawn] worldPos={instance.transform.position:F3}, " +
                      $"localPos={instance.transform.localPosition:F3}, " +
                      $"parent={parentName}", instance);

            foreach (Transform child in instance.transform)
            {
                Debug.Log($"[Aura Child After] {child.name}: " +
                          $"worldPos={child.position:F3}, " +
                          $"localPos={child.localPosition:F3}, " +
                          $"localScale={child.localScale:F3}", instance);
            }

            DesyncOnce(instance);

            // Проверка через секунду — что-то самостоятельно двигается?
            StartCoroutine(DiagAuraAfterDelay(instance, 1f));
        }

        if (instance.activeSelf != active)
            instance.SetActive(active);
    }

    private System.Collections.IEnumerator DiagAuraAfterDelay(GameObject aura, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (aura == null) yield break;

        Debug.Log($"═══════ [Aura Diag] ЧЕРЕЗ {delay}с ═══════");
        Debug.Log($"[Hero Now] worldPos={transform.position:F3}", this);
        Debug.Log($"[Aura Now] worldPos={aura.transform.position:F3}, " +
                  $"localPos={aura.transform.localPosition:F3}", aura);

        foreach (Transform child in aura.transform)
        {
            Debug.Log($"[Aura Child Now] {child.name}: " +
                      $"worldPos={child.position:F3}, " +
                      $"localPos={child.localPosition:F3}", aura);
        }
    }

    /// <summary>
    /// Задаёт партиклам случайную стартовую фазу ОДИН раз при создании.
    /// Каждая аура с рождения в своей точке цикла → толпа вразнобой.
    /// </summary>
    private void DesyncOnce(GameObject effect)
    {
        ParticleSystem[] systems = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.emitterVelocityMode = ParticleSystemEmitterVelocityMode.Transform;
            var vel = ps.velocityOverLifetime;
            var force = ps.forceOverLifetime;
            var inherit = ps.inheritVelocity;
            var noise = ps.noise;
            var shape = ps.shape;

            Debug.Log($"[Aura Full] {ps.name}: " +
                      $"SimSpace={main.simulationSpace}, " +
                      $"EmitVelMode={main.emitterVelocityMode}, " +
                      $"VelLife={vel.enabled}(x={vel.x.constant:F2},z={vel.z.constant:F2}), " +
                      $"Force={force.enabled}(x={force.x.constant:F2},z={force.z.constant:F2}), " +
                      $"Inherit={inherit.enabled}, " +
                      $"Noise={noise.enabled}(str={noise.strength.constant:F2}), " +
                      $"ShapePos={shape.position}", ps);

            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.randomSeed = (uint)Random.Range(1, 999999);
            float phase = Random.Range(0f, ps.main.duration);
            ps.Simulate(phase, false, true);
            ps.Play(false);
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
        
        int baseMaxHP = GetBaseMaxHP();
        _currentHP = baseMaxHP * multiplier;
        _lastDamageTime = -999f;
        _regenAccumulator = 0f;

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, baseMaxHP * multiplier);

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
    public bool TakeDamage(int amount, bool showDamageNumber = true, DamageNumberType numberType = DamageNumberType.Normal)
    {
        int hpBefore = _currentHP;
        _currentHP -= amount;
        _lastDamageTime = Time.time;

        if (showDamageNumber && DamageNumberPool.Instance != null)
        {
            Transform anchor = _damageNumberAnchor != null ? _damageNumberAnchor : transform;
            DamageNumberPool.Instance.Spawn(amount, anchor, numberType, _damageNumberHeight);
        }

        int maxHP = GetBaseMaxHP() * _powerMultiplier;

        if (_healthBar != null)
            _healthBar.SetHP(_currentHP, maxHP);

        if (_currentHP <= 0)
        {
            _currentHP = 0;
            IsDead = true;
            
            var warriorAttack = GetComponent<WarriorAutoAttack>();
            VfxConfig vfx = warriorAttack != null ? warriorAttack.GetVfxConfig() : null;
            
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

        if (_data == null && _prefabMaxHP <= 0) return;

        bool canRegen = (Time.time - _lastDamageTime >= _regenDelay);
        int maxHP = GetBaseMaxHP() * _powerMultiplier;
        bool needsRegen = _currentHP < maxHP;

        // Лог когда регенерация только начинается
        if (canRegen && needsRegen && !_wasRegenerating)
        {
            _wasRegenerating = true;
        }

        // Лог когда регенерация закончилась (HP=max или пришёл удар)
        if (_wasRegenerating && (!canRegen || !needsRegen))
        {
            _wasRegenerating = false;
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

    private void LateUpdate()
    {
        GameObject activeAura = _fireAura ?? _iceAura ?? _lightningAura;
        if (activeAura == null || !activeAura.activeSelf) return;
        
        if (Time.frameCount % 60 != 0) return; // раз в секунду
        
        Debug.Log($"═══ Frame {Time.frameCount} ═══");
        Debug.Log($"[Hero]  worldPos={transform.position:F3}", this);
        Debug.Log($"[Aura]  worldPos={activeAura.transform.position:F3}, " +
                  $"localPos={activeAura.transform.localPosition:F3}", activeAura);
        
        foreach (Transform child in activeAura.transform)
        {
            Vector3 offsetFromHero = child.position - transform.position;
            Debug.Log($"  [{child.name}] worldPos={child.position:F3}, " +
                      $"localPos={child.localPosition:F3}, " +
                      $"offsetFromHero={offsetFromHero:F3}", child);
        }
        
        // Позиция дочернего меша (SkinnedMeshRenderer)
        var smr = GetComponentInChildren<SkinnedMeshRenderer>();
        if (smr != null)
        {
            Debug.Log($"[Mesh] {smr.name}: " +
                      $"transformWorldPos={smr.transform.position:F3}, " +
                      $"bounds.center={smr.bounds.center:F3}, " +
                      $"offsetFromRoot={smr.bounds.center - transform.position:F3}", smr);
        }
    }

    // Заглушки для Animation Events (звуки шагов)
    public void FootL() { }
    public void FootR() { }

    private int GetBaseMaxHP()
    {
        // Если в префабе (или на сцене) задано HP > 0, оно имеет приоритет над ScriptableObject
        if (_prefabMaxHP > 0)
            return _prefabMaxHP;
            
        return _data != null ? _data.MaxHP : 50;
    }
}
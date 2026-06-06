using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

/// <summary>
/// Пара "тип героя + количество" для настройки стартового отряда.
/// </summary>
[System.Serializable]
public class StartUnitEntry
{
    public HeroType heroType = HeroType.Mage;
    [Min(0)] public int count = 1;
}

/// <summary>
/// Управляет отрядом: хранит список юнитов,
/// расставляет их в шеренгу вокруг SquadLeader.
/// </summary>
public class SquadController : MonoBehaviour
{
    [Header("Настройки формации")]
    [Tooltip("Дефолтное расстояние между юнитами в ряду по X")]
    [SerializeField] private float _spacing     = 0.4f;

    [Header("Настройки толпы")]
    [Tooltip("Расстояние между юнитами внутри зоны")]
    [SerializeField] private float _crowdSpacing    = 0.35f;

    [Tooltip("Расстояние между зонами по Z")]
    [SerializeField] private float _zoneSpacing     = 0.8f;

    [Tooltip("Максимум юнитов в одной строке сетки")]
    [SerializeField] private int   _maxPerRow       = 8;

    [Tooltip("Смещение всего отряда вперёд от лидера по Z")]
    [SerializeField] private float _crowdForwardOffset = 1.5f;

    [SerializeField] private float _followSpeed = 10f;

    [Tooltip("Насколько куча расширяется при росте числа юнитов. Меньше = плотнее.")]
    [SerializeField] private float _densityScale = 0.15f;

    [Header("Передняя дуга танков")]
    [Tooltip("Ширина дуги танков по X")]
    [SerializeField] private float _tankArcWidth = 3f;

    [Tooltip("Насколько дуга изогнута вперёд (горб)")]
    [SerializeField] private float _tankArcCurve = 1f;

    [Header("Стартовый отряд")]
    [SerializeField] private List<StartUnitEntry> _startUnits = new()
    {
        new StartUnitEntry { heroType = HeroType.Tank, count = 1 },
        new StartUnitEntry { heroType = HeroType.Mage, count = 5 },
    };

    // ─── Структура толпы ────────────────────────────────────────────
    // Ключ = (тип, тир). Значение = список юнитов этой категории.
    // ПРАВИЛА:
    // - T1-категория: макс 15 юнитов → слияние в 1 T2
    // - T2-категория: нет лимита по числу, но общий лимит 50 всех моделей
    // - При лимите 50: новое слияние T2 → PowerMultiplier +1 у всех T2 этого типа
    // - При лимите 50 + ворота +N: PowerMultiplier +N у T1 этого типа
    private readonly Dictionary<(HeroType, UnitTier), List<Unit>> _crowd = new();

    // Вспомогательный список для итерации по всем юнитам (обновляется в RebuildFlatList)
    private readonly List<Unit> _allUnits = new();
    private bool _flatListDirty = true;

    public int UnitCount => CountAllUnits();

    private const int MAX_T1_PER_CATEGORY = 15;  // TODO: перенести в RemoteConfig
    private const int MAX_TOTAL_MODELS    = 50;  // TODO: перенести в RemoteConfig

    /// <summary>Считает все модели на сцене (T1 + T2 суммарно).</summary>
    private int CountAllUnits()
    {
        int total = 0;
        foreach (var list in _crowd.Values)
            total += list.Count;
        return total;
    }

    /// <summary>Считает только T2-модели всех типов.</summary>
    private int CountAllT2()
    {
        int total = 0;
        foreach (var kv in _crowd)
            if (kv.Key.Item2 == UnitTier.T2)
                total += kv.Value.Count;
        return total;
    }

    /// <summary>Возвращает количество юнитов конкретного типа (T1 + T2 суммарно).</summary>
    public int GetUnitCountByType(HeroType type)
    {
        return GetCategory(type, UnitTier.T1).Count +
               GetCategory(type, UnitTier.T2).Count;
    }

    /// <summary>
    /// Возвращает список юнитов категории (type, tier).
    /// Создаёт пустой список если категории нет.
    /// </summary>
    private List<Unit> GetCategory(HeroType type, UnitTier tier)
    {
        var key = (type, tier);
        if (!_crowd.ContainsKey(key))
            _crowd[key] = new List<Unit>();
        return _crowd[key];
    }

    /// <summary>Перестраивает плоский список всех юнитов (для UpdateFormation и итерации).</summary>
    private void RebuildFlatList()
    {
        _allUnits.Clear();
        foreach (var list in _crowd.Values)
            _allUnits.AddRange(list);
        _flatListDirty = false;
    }

    /// <summary>
    /// Возвращает индекс зоны для типа юнита.
    /// 0 = самый далёкий от камеры (впереди по движению).
    /// Чем больше индекс — тем ближе к камере (сзади).
    /// </summary>
    private static int GetCrowdZone(HeroType type)
    {
        return type switch
        {
            HeroType.Warrior   => 0,  // впереди
            HeroType.Assassin  => 0,  // впереди
            HeroType.Tank      => 0,
            HeroType.Mage      => 2,  // Стрелки
            HeroType.Archer    => 2,  // Стрелки
            HeroType.Healer    => 3,  // Тыл
            HeroType.Support   => 3,  // Тыл
            _                  => 2,
        };
    }

    private void PlaceCrowd()
    {
        if (_allUnits.Count == 0) return;

        const int ZONE_COUNT = 4;
        List<Unit>[] zones = new List<Unit>[ZONE_COUNT];
        for (int z = 0; z < ZONE_COUNT; z++)
            zones[z] = new List<Unit>();

        foreach (Unit u in _allUnits)
        {
            if (u == null) continue;
            zones[GetCrowdZone(u.HeroType)].Add(u);
        }

        // Фиксированные Z-позиции для каждой зоны
        // Зона 0 (Tank) = дальше всех от камеры
        // Зона 3 (Healer) = ближе всех к камере
        float[] zoneZ = new float[ZONE_COUNT];
        
        // Считаем сколько строк займёт каждая зона
        float currentZ = _crowdForwardOffset;
        for (int z = 0; z < ZONE_COUNT; z++)
        {
            if (zones[z].Count == 0) continue;
            
            int rows = Mathf.CeilToInt(zones[z].Count / (float)_maxPerRow);
            zoneZ[z] = currentZ + (rows - 1) * _crowdSpacing;
            currentZ += rows * _crowdSpacing + _zoneSpacing;
        }

        // Расставляем зоны — Tank (зона 0) дальше всех
        float maxZ = 0;
        for (int z = 0; z < ZONE_COUNT; z++)
            if (zones[z].Count > 0 && zoneZ[z] > maxZ) maxZ = zoneZ[z];

        for (int z = 0; z < ZONE_COUNT; z++)
        {
            if (zones[z].Count == 0) continue;
            // Инвертируем: зона 0 (Tank) получает максимальный Z
            float invertedZ = maxZ - zoneZ[z] + _crowdForwardOffset;

            if (z == 0)
                PlaceTankArc(zones[z], invertedZ); // танки — передней дугой
            else
                PlaceZone(zones[z], invertedZ);    // остальные — кучей
        }
    }

    /// <summary>
    /// Расставляет танков передней дугой (изогнутый ряд спереди),
    /// равномерно по ширине. Танки прикрывают остальную кучу.
    /// </summary>
    private void PlaceTankArc(List<Unit> tanks, float baseZ)
    {
        int total = tanks.Count;
        if (total == 0) return;

        for (int i = 0; i < total; i++)
        {
            Unit u = tanks[i];
            if (u == null || u.IsDead) continue;

            // Равномерно по ширине: t от -0.5 до +0.5
            float t = total > 1 ? (i / (float)(total - 1)) - 0.5f : 0f;

            float x = t * _tankArcWidth;
            // Горб дуги: центр выдвинут вперёд, края назад (парабола)
            float zCurve = (1f - 4f * t * t) * _tankArcCurve;
            float z = baseZ + zCurve;

            Vector3 anchorWorld = transform.position + new Vector3(x, 0f, z);

            CrowdAgent agent = u.GetComponent<CrowdAgent>();
            if (agent == null) agent = u.gameObject.AddComponent<CrowdAgent>();
            agent.Anchor = anchorWorld;
            agent.DensityScale = 1f; // у дуги фиксированный шаг, не раздуваем

            MeleeUnitController meleeCtrl = u.GetComponent<MeleeUnitController>();
            if (meleeCtrl != null)
            {
                meleeCtrl.FormationOffset = anchorWorld - transform.position;
                if (!meleeCtrl.IsInFormation) continue;
            }

            agent.Step(GetNeighborPositions(u), Time.deltaTime);
        }
    }

    private void PlaceZone(List<Unit> units, float baseZ)
    {
        int total = units.Count;
        if (total == 0) return;

        // Множитель плотности: 1.0 для маленького отряда, плавно растёт с числом.
        // 0.25 — насколько агрессивно расширяется (меньше = компактнее).
        float densityScale = 1f + Mathf.Sqrt(total) * _densityScale;

        // Общий центр зоны — все юниты зоны тянутся СЮДА,
        // а расталкивание раскидывает их в живую кучу вокруг центра.
        Vector3 zoneCenter = transform.position + new Vector3(0f, 0f, baseZ);

        foreach (Unit u in units)
        {
            if (u == null || u.IsDead) continue;

            CrowdAgent agent = u.GetComponent<CrowdAgent>();
            if (agent == null) agent = u.gameObject.AddComponent<CrowdAgent>();
            agent.Anchor = zoneCenter;
            agent.DensityScale = densityScale;

            MeleeUnitController meleeCtrl = u.GetComponent<MeleeUnitController>();
            if (meleeCtrl != null)
            {
                // offset уже не сетка — просто текущее смещение от лидера (для rejoin)
                meleeCtrl.FormationOffset = u.transform.position - transform.position;
                if (!meleeCtrl.IsInFormation) continue;
            }

            agent.Step(GetNeighborPositions(u), Time.deltaTime);
        }
    }

    // Временный список соседей, переиспользуем чтобы не плодить мусор
    private readonly List<Vector3> _neighborBuffer = new();

    /// <summary>Собирает позиции соседей для расталкивания (всех живых, кроме себя).</summary>
    private List<Vector3> GetNeighborPositions(Unit self)
    {
        _neighborBuffer.Clear();
        foreach (Unit u in _allUnits)
        {
            if (u == null || u == self || u.IsDead) continue;
            _neighborBuffer.Add(u.transform.position);
        }
        return _neighborBuffer;
    }

    private void Start()
    {
        // Инициализируем _crowd заранее для всех типов и тиров
        foreach (HeroType type in System.Enum.GetValues(typeof(HeroType)))
        {
            _crowd[(type, UnitTier.T1)] = new List<Unit>();
            _crowd[(type, UnitTier.T2)] = new List<Unit>();
        }

        // Спавним стартовый отряд
        foreach (StartUnitEntry entry in _startUnits)
        {
            for (int i = 0; i < entry.count; i++)
                AddUnit(entry.heroType);
        }
    }

    private void Update()
    {
        if (_flatListDirty) RebuildFlatList();
        PlaceCrowd();
    }



    /// <summary>
    /// Добавляет юнита в толпу. Главная точка входа для ворот и старта.
    /// После добавления — проверяет слияние T1 и общий лимит 50.
    /// </summary>
    public void AddUnit(HeroType type)
    {
        int totalModels = CountAllUnits();

        if (totalModels >= MAX_TOTAL_MODELS)
        {
            // Лимит 50 достигнут — усиляем существующих T1 этого типа
            // (или T2 если T1 нет)
            PowerUpCategory(type);
            Debug.Log($"[Crowd] Лимит {MAX_TOTAL_MODELS} достигнут. {type} PowerUp!", this);
            return;
        }

        // Добавляем обычного T1
        Unit unit = UnitPool.Instance.Get(type, UnitTier.T1);
        if (unit == null) return;

        GetCategory(type, UnitTier.T1).Add(unit);
        _flatListDirty = true;

        // Инициализация если это легендарка
        MeleeUnitController meleeCtrl = unit.GetComponent<MeleeUnitController>();
        if (meleeCtrl != null)
            meleeCtrl.Initialize(transform, Vector3.zero);

        Debug.Log($"[Crowd] +1 {type}_T1. Всего: {CountAllUnits()}", this);

        // Проверяем нужно ли сливать T1 → T2
        TryMergeT1(type);
    }

    /// <summary>
    /// Меняет стихию ВСЕХ юнитов в отряде.
    /// Вызывается ElementGate при прохождении.
    /// </summary>
    public void SetSquadElement(ElementType element)
    {
        if (_flatListDirty) RebuildFlatList();

        foreach (Unit u in _allUnits)
        {
            if (u == null) continue;
            try
            {
                u.SetElement(element);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Crowd] Не удалось сменить стихию у {u.name}: {e.Message}", this);
            }
        }

        Debug.Log($"[Crowd] Стихия отряда → {element}", this);
    }

    /// <summary>
    /// Убирает N юнитов указанного типа из отряда.
    /// Используется плохими воротами (-N Type).
    /// Убирает сначала T1, потом T2.
    /// </summary>
    public void RemoveUnits(HeroType type, int count)
    {
        int removed = 0;

        // Сначала убираем T1
        var t1list = GetCategory(type, UnitTier.T1);
        while (removed < count && t1list.Count > 0)
        {
            Unit u = t1list[^1];
            t1list.RemoveAt(t1list.Count - 1);
            UnitPool.Instance.Return(u);
            _flatListDirty = true;
            removed++;
        }

        // Если T1 кончились — убираем T2
        var t2list = GetCategory(type, UnitTier.T2);
        while (removed < count && t2list.Count > 0)
        {
            Unit u = t2list[^1];
            t2list.RemoveAt(t2list.Count - 1);
            UnitPool.Instance.Return(u);
            _flatListDirty = true;
            removed++;
        }

        Debug.Log($"[Crowd] -{removed} {type}. Всего: {CountAllUnits()}", this);
    }

    /// <summary>
    /// Усиливает категорию при достижении лимита 50.
    /// Приоритет — T2 юниты, если нет — T1.
    /// </summary>
    private void PowerUpCategory(HeroType type)
    {
        var t2list = GetCategory(type, UnitTier.T2);
        if (t2list.Count > 0)
        {
            foreach (Unit u in t2list)
                u.IncrementPowerMultiplier();
            Debug.Log($"[Crowd] {type}_T2 × PowerMultiplier++", this);
            return;
        }

        var t1list = GetCategory(type, UnitTier.T1);
        foreach (Unit u in t1list)
            u.IncrementPowerMultiplier();
        Debug.Log($"[Crowd] {type}_T1 × PowerMultiplier++", this);
    }

    /// <summary>
    /// Проверяет: если в T1-категории >= 15 → сливает 15 в 1 T2.
    /// Цикл на случай если добавили +30 и нужно слить дважды.
    /// </summary>
    private void TryMergeT1(HeroType type)
    {
        var t1list = GetCategory(type, UnitTier.T1);
        var data   = UnitPool.Instance.GetHeroData(type);

        // Если у этого типа нет T2 prefab — не апаем
        if (data == null || !data.CanUpgradeToT2) return;

        while (t1list.Count >= MAX_T1_PER_CATEGORY)
        {
            // Убираем 15 T1 в пул
            for (int i = 0; i < MAX_T1_PER_CATEGORY; i++)
            {
                Unit u = t1list[0];
                t1list.RemoveAt(0);
                UnitPool.Instance.Return(u);
            }

            _flatListDirty = true;
            AddT2(type);
            Debug.Log($"[Crowd] {type}: 15×T1 → 1×T2", this);
        }
    }

    /// <summary>
    /// Добавляет 1 T2-юнита или усиляет существующих если лимит 50 достигнут.
    /// </summary>
    private void AddT2(HeroType type)
    {
        int totalModels = CountAllUnits();

        if (totalModels < MAX_TOTAL_MODELS)
        {
            // Есть место — спавним новый T2
            Unit t2 = UnitPool.Instance.Get(type, UnitTier.T2);
            if (t2 == null) return;

            // Новый T2 = пакет из 15 T1 → multiplier=15
            var data = UnitPool.Instance.GetHeroData(type);
            if (data != null)
                t2.Initialize(data, UnitTier.T2, MAX_T1_PER_CATEGORY);

            GetCategory(type, UnitTier.T2).Add(t2);
            _flatListDirty = true;

            MeleeUnitController meleeCtrl = t2.GetComponent<MeleeUnitController>();
            if (meleeCtrl != null)
                meleeCtrl.Initialize(transform, Vector3.zero);

            Debug.Log($"[Crowd] +1 {type}_T2 (multiplier={MAX_T1_PER_CATEGORY}). Всего T2: {CountAllT2()}", this);
        }
        else
        {
            // Лимит — усиляем всех T2 этого типа
            var t2list = GetCategory(type, UnitTier.T2);
            foreach (Unit u in t2list)
                u.IncrementPowerMultiplier();
            Debug.Log($"[Crowd] Лимит! {type}_T2 PowerMultiplier++. Всего T2: {CountAllT2()}", this);
        }
    }

    /// <summary>Убирает последнего добавленного T1-юнита из любой категории.</summary>
    public void RemoveLastUnit()
    {
        foreach (var kv in _crowd)
        {
            if (kv.Key.Item2 != UnitTier.T1) continue;
            var list = kv.Value;
            if (list.Count == 0) continue;

            Unit unit = list[^1];
            list.RemoveAt(list.Count - 1);
            UnitPool.Instance.Return(unit);
            _flatListDirty = true;

            Debug.Log($"[Crowd] -1 {kv.Key.Item1}_T1. Всего: {CountAllUnits()}", this);
            return;
        }
    }

    /// <summary>Тест через кнопку в редакторе — добавить воина.</summary>
    [ContextMenu("Тест — добавить воина")]
    private void TestAddWarrior() => AddUnit(HeroType.Warrior);

    [ContextMenu("Тест — добавить мага")]
    private void TestAddMage() => AddUnit(HeroType.Mage);

    [ContextMenu("Тест — добавить танка")]
    private void TestAddTank() => AddUnit(HeroType.Tank);

    [ContextMenu("Тест — добавить лучника")]
    private void TestAddArcher() => AddUnit(HeroType.Archer);

    [ContextMenu("Тест — добавить ассасина")]
    private void TestAddAssassin() => AddUnit(HeroType.Assassin);

    /// <summary>Тест через кнопку в редакторе — убрать юнита.</summary>
    [ContextMenu("Тест — убрать юнита")]
    private void TestRemove() => RemoveLastUnit();
    /// <summary>
    /// Вызывается когда юнит погибает от урона врага.
    /// Убирает юнита из формации и возвращает в пул.
    /// </summary>
    public void OnUnitDied(Unit unit)
    {
        // Ищем юнита во всех категориях и убираем
        foreach (var list in _crowd.Values)
        {
            if (list.Remove(unit))
            {
                _flatListDirty = true;
                unit.gameObject.SetActive(false);

                Debug.Log($"[Squad] Юнит {unit.gameObject.name} удалён из отряда. " +
                          $"Осталось: {CountAllUnits()}", this);

                // Если отряд полностью пуст — Game Over
                if (CountAllUnits() == 0)
                {
                    Debug.Log("[Squad] Все юниты погибли! Game Over.", this);
                    if (GameStateManager.Instance != null)
                        GameStateManager.Instance.SetGameOver();
                }

                return;
            }
        }

        Debug.LogWarning($"[Squad] OnUnitDied: юнит {unit.gameObject.name} не найден в отряде!", this);
    }
    /// <summary>
    /// Возвращает юнита ближайшего по X к указанной позиции.
    /// Используется врагами для наведения.
    /// </summary>
    public Unit GetNearestUnitByX(float x)
    {
        if (_flatListDirty) RebuildFlatList();
        if (_allUnits.Count == 0) return null;

        Unit  nearest = null;
        float minDist = float.MaxValue;

        foreach (Unit u in _allUnits)
        {
            if (u == null || !u.gameObject.activeSelf) continue;
            float dist = Mathf.Abs(u.transform.position.x - x);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = u;
            }
        }
        return nearest;
    }
    /// <summary>
    /// Возвращает случайного живого юнита из отряда.
    /// Используется для естественного распределения врагов по целям.
    /// </summary>
    public Unit GetRandomUnit()
    {
        if (_flatListDirty) RebuildFlatList();
        if (_allUnits.Count == 0) return null;

        // Фильтруем только активных
        List<Unit> active = _allUnits.FindAll(u => u != null && u.gameObject.activeSelf);
        if (active.Count == 0) return null;

        return active[Random.Range(0, active.Count)];
    }
}
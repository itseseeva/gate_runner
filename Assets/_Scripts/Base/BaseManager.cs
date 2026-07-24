using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// Синглтон управления базой. DontDestroyOnLoad.
/// Хранит список зданий, сохраняет/загружает через PlayerPrefs (JSON).
///
/// На первый запуск — создаёт стартовую базу из 6 зданий Lvl 1.
/// </summary>
public class BaseManager : MonoBehaviour
{
    public static BaseManager Instance { get; private set; }

    private const string SAVE_KEY = "mgr_base_state";

    [Header("Данные зданий")]
    [Tooltip("Все шаблоны зданий (по одному на тип). Создаются как SO-ассеты.")]
    [SerializeField] private List<BuildingDataSO> _buildingDataAssets = new();

    // ─── Состояние ──────────────────────────────────────────
    private BaseState _state = new();

    /// <summary>Все здания на базе (только чтение).</summary>
    public IReadOnlyList<BuildingInstance> Buildings => _state.Buildings;

    // ─── События ─────────────────────────────────────────────
    public static event Action OnBuildingsChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    // ─── Доступ к данным шаблона ────────────────────────────

    /// <summary>Возвращает шаблон данных по типу здания.</summary>
    public BuildingDataSO GetDataFor(BuildingType type)
    {
        foreach (var data in _buildingDataAssets)
        {
            if (data != null && data.Type == type) return data;
        }
        Debug.LogError($"[Base] Не найден BuildingData для типа {type}", this);
        return null;
    }

    /// <summary>Возвращает здание по Id, или null.</summary>
    public BuildingInstance GetBuilding(string id)
    {
        foreach (var b in _state.Buildings)
            if (b.Id == id) return b;
        return null;
    }

    /// <summary>Возвращает первое здание указанного типа (если есть).</summary>
    public BuildingInstance GetBuildingByType(BuildingType type)
    {
        foreach (var b in _state.Buildings)
            if (b.Type == type) return b;
        return null;
    }

    // ─── Сохранение / загрузка ──────────────────────────────

    private void Load()
    {
        string json = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(json))
        {
            // Первый запуск — создаём стартовую базу
            CreateStartingBase();
            Save();
        }
        else
        {
            try
            {
                _state = JsonUtility.FromJson<BaseState>(json);
                if (_state == null) _state = new BaseState();
                if (_state.Buildings == null) _state.Buildings = new List<BuildingInstance>();
                {}
            }
            catch (Exception e)
            {
                Debug.LogError($"[Base] Ошибка загрузки: {e.Message}. Создаю новую базу.", this);
                _state = new BaseState();
                CreateStartingBase();
                Save();
            }
        }
    }

    private void Save()
    {
        _state.LastSaveTicks = DateTime.UtcNow.Ticks;
        string json = JsonUtility.ToJson(_state);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    // ─── Создание стартовой базы ────────────────────────────

    /// <summary>
    /// Стартовая раскладка: все 6 зданий Lvl 1 на фиксированных позициях.
    /// Позиции — на сетке 3×2 в центре базы.
    /// </summary>
    private void CreateStartingBase()
    {
        _state.Buildings.Clear();

        // Раскладка на сетке (X, Z), Y=0
        // Расстояние между зданиями: 4 unit
        var layout = new (BuildingType type, Vector3 pos)[]
        {
            (BuildingType.HQ,       new Vector3(  0, 0,  0)),
            (BuildingType.GoldMine, new Vector3( -5, 0,  4)),
            (BuildingType.IronMine, new Vector3(  5, 0,  4)),
            (BuildingType.Barracks, new Vector3( -5, 0, -4)),
            (BuildingType.Training, new Vector3(  5, 0, -4)),
            (BuildingType.Storage,  new Vector3(  0, 0, -8)),
        };

        DateTime now = DateTime.UtcNow;

        foreach (var (type, pos) in layout)
        {
            var b = new BuildingInstance
            {
                Id              = type.ToString(),
                Type            = type,
                Level           = 1,
                Position        = pos,
                IsUpgrading     = false,
                LastCollectTime = now,
            };
            _state.Buildings.Add(b);
        }

        {}
    }

    // ─── Изменение состояния ────────────────────────────────

    /// <summary>Принудительное сохранение и оповещение UI. Вызывать после любого изменения.</summary>
    public void NotifyChanged()
    {
        Save();
        OnBuildingsChanged?.Invoke();
    }

    // ─── Апгрейды ───────────────────────────────────────────

    /// <summary>
    /// Запускает апгрейд здания. Списывает ресурсы. Возвращает true если получилось.
    /// </summary>
    public bool StartUpgrade(string buildingId)
    {
        var building = GetBuilding(buildingId);
        if (building == null)
        {
            Debug.LogError($"[Base] Здание {buildingId} не найдено!", this);
            return false;
        }

        // Уже в процессе апгрейда?
        if (building.IsUpgrading)
        {
            Debug.LogWarning($"[Base] {buildingId} уже апгрейдится", this);
            return false;
        }

        // Достигли максимума?
        var data = GetDataFor(building.Type);
        if (data == null) return false;
        if (building.Level >= data.MaxLevel)
        {
            Debug.LogWarning($"[Base] {buildingId} уже на максимуме", this);
            return false;
        }

        // Проверка очереди — пока только 1 апгрейд одновременно
        if (IsAnyUpgrading())
        {
            Debug.LogWarning($"[Base] Уже идёт другой апгрейд — очередь пока 1", this);
            return false;
        }

        // Получаем данные следующего уровня
        var nextLevelData = data.GetLevel(building.Level + 1);
        if (nextLevelData == null) return false;

        // Списываем ресурсы
        if (ResourceManager.Instance == null)
        {
            Debug.LogError("[Base] ResourceManager не найден!", this);
            return false;
        }

        if (!ResourceManager.Instance.TrySpend(nextLevelData.CostGold, nextLevelData.CostIron))
        {
            Debug.LogWarning("[Base] Не хватает ресурсов", this);
            return false;
        }

        // Запускаем таймер
        building.IsUpgrading = true;
        building.UpgradeEndTime = DateTime.UtcNow.AddSeconds(nextLevelData.UpgradeTimeSeconds);

        Save();
        OnBuildingsChanged?.Invoke();

        {}
        return true;
    }

    /// <summary>Завершает апгрейд — уровень +1, флаги сбрасываются.</summary>
    public void CompleteUpgrade(string buildingId)
    {
        var building = GetBuilding(buildingId);
        if (building == null || !building.IsUpgrading) return;

        building.Level++;
        building.IsUpgrading = false;

        Save();
        OnBuildingsChanged?.Invoke();

        {}
    }

    /// <summary>Есть ли хоть одно здание которое сейчас апгрейдится.</summary>
    public bool IsAnyUpgrading()
    {
        foreach (var b in _state.Buildings)
            if (b.IsUpgrading) return true;
        return false;
    }

    /// <summary>
    /// Проверяет все здания — если у кого-то таймер истёк, завершает апгрейд.
    /// Вызывается из UpgradeTicker каждую секунду.
    /// </summary>
    public void CheckUpgradeTimers()
    {
        DateTime now = DateTime.UtcNow;
        foreach (var b in _state.Buildings)
        {
            if (b.IsUpgrading && now >= b.UpgradeEndTime)
            {
                CompleteUpgrade(b.Id);
            }
        }
    }

    // ─── Отладка ───────────────────────────────────────────

    [ContextMenu("Сбросить базу")]
    public void ResetBase()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        _state = new BaseState();
        CreateStartingBase();
        Save();
        OnBuildingsChanged?.Invoke();
        {}
    }

    [ContextMenu("Показать список зданий")]
    public void DebugPrintBuildings()
    {
        {}
        foreach (var b in _state.Buildings)
        {
            {}
        }
    }
}

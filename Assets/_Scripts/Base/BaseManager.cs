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
                Debug.Log($"[Base] Загружено: {_state.Buildings.Count} зданий", this);
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

        Debug.Log($"[Base] Стартовая база создана: {_state.Buildings.Count} зданий", this);
    }

    // ─── Изменение состояния ────────────────────────────────

    /// <summary>Принудительное сохранение и оповещение UI. Вызывать после любого изменения.</summary>
    public void NotifyChanged()
    {
        Save();
        OnBuildingsChanged?.Invoke();
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
        Debug.Log("[Base] База сброшена в стартовое состояние", this);
    }

    [ContextMenu("Показать список зданий")]
    public void DebugPrintBuildings()
    {
        Debug.Log($"=== Здания на базе ({_state.Buildings.Count}) ===");
        foreach (var b in _state.Buildings)
        {
            Debug.Log($"  • {b.Id} [{b.Type}] Lvl {b.Level} @ {b.Position}");
        }
    }
}

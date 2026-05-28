using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул юнитов по тирам. Никаких Instantiate/Destroy в рантайме.
/// Get(type, tier) — берёт из нужного пула (T1 или T2).
/// Return(unit) — возвращает в правильный пул по unit.Tier.
/// </summary>
public class UnitPool : MonoBehaviour
{
    public static UnitPool Instance;

    [Tooltip("Сколько юнитов каждого типа создать заранее (для T1 и T2 отдельно)")]
    [SerializeField] private int _preloadCount = 10;

    [Tooltip("Все 5+ HeroDefinitionSO сюда (Warrior, Mage, Archer, Tank, Assassin, ...)")]
    [SerializeField] private HeroDefinitionSO[] _heroData;

    // Ключ — (тип, тир). Каждая пара имеет свою очередь.
    private Dictionary<(HeroType, UnitTier), Queue<Unit>> _pool = new();
    private Dictionary<HeroType, HeroDefinitionSO> _dataMap = new();

    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        foreach (var data in _heroData)
        {
            _dataMap[data.HeroType] = data;

            // T1 пул — всегда создаём
            CreatePoolForTier(data, UnitTier.T1);

            // T2 пул — только если у SO есть _prefabT2
            if (data.CanUpgradeToT2)
            {
                CreatePoolForTier(data, UnitTier.T2);
            }
        }
        Debug.Log("[UnitPool] Пул готов!", this);
    }

    private void CreatePoolForTier(HeroDefinitionSO data, UnitTier tier)
    {
        var key = (data.HeroType, tier);
        _pool[key] = new Queue<Unit>();

        for (int i = 0; i < _preloadCount; i++)
        {
            Unit unit = CreateUnit(data, tier);
            unit.gameObject.SetActive(false);
            _pool[key].Enqueue(unit);
        }
    }

    private Unit CreateUnit(HeroDefinitionSO data, UnitTier tier)
    {
        GameObject prefab = data.GetPrefab(tier);
        if (prefab == null)
        {
            Debug.LogError($"[UnitPool] Нет prefab для {data.HeroType} {tier}!", this);
            return null;
        }

        GameObject go = Instantiate(prefab);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(data, tier);
        return unit;
    }

    /// <summary>Возвращает HeroDefinitionSO по типу героя.</summary>
    public HeroDefinitionSO GetHeroData(HeroType type)
    {
        return _dataMap.ContainsKey(type) ? _dataMap[type] : null;
    }

    /// <summary>Берёт юнита из пула указанного тира.</summary>
    public Unit Get(HeroType type, UnitTier tier = UnitTier.T1)
    {
        var key = (type, tier);

        // Если категории нет — значит у этого типа нет такого тира
        if (!_pool.ContainsKey(key))
        {
            Debug.LogError($"[UnitPool] Нет пула для {type} {tier}. Проверь что _prefabT2 указан в SO.", this);
            return null;
        }

        if (_pool[key].Count > 0)
        {
            Unit unit = _pool[key].Dequeue();
            unit.gameObject.SetActive(true);
            return unit;
        }

        // Пул пуст — создаём новый на лету
        return CreateUnit(_dataMap[type], tier);
    }

    /// <summary>Возвращает юнита в пул (автоматически в нужный тир).</summary>
    public void Return(Unit unit)
    {
        if (unit == null) return;

        var key = (unit.HeroType, unit.Tier);

        if (!_pool.ContainsKey(key))
        {
            Debug.LogWarning($"[UnitPool] Возврат в несуществующий пул {key}, уничтожаем юнита.", this);
            Destroy(unit.gameObject);
            return;
        }

        unit.ResetVisual(); // ← восстанавливаем визуал перед возвратом
        unit.gameObject.SetActive(false);
        _pool[key].Enqueue(unit);
    }
}
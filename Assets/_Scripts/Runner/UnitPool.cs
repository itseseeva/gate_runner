using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул юнитов. Никаких Instantiate/Destroy в рантайме.
/// Get() — берёт из пула. Return() — возвращает в пул.
/// </summary>
public class UnitPool : MonoBehaviour
{
    public static UnitPool Instance;

    [SerializeField] private int _preloadCount = 10; // сколько создать заранее
    [SerializeField] private HeroDefinitionSO[] _heroData; // все 3 SO сюда

    private Dictionary<HeroType, Queue<Unit>> _pool = new();
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
            _pool[data.HeroType] = new Queue<Unit>();

            for (int i = 0; i < _preloadCount; i++)
            {
                Unit unit = CreateUnit(data);
                unit.gameObject.SetActive(false);
                _pool[data.HeroType].Enqueue(unit);
            }
        }
        Debug.Log("[UnitPool] Пул готов!", this);
    }

    private Unit CreateUnit(HeroDefinitionSO data)
    {
        GameObject go = Instantiate(data.Prefab);
        Unit unit = go.GetComponent<Unit>();
        unit.Initialize(data);
        return unit;
    }

    /// <summary>Берёт юнита из пула.</summary>
    public Unit Get(HeroType type)
    {
        if (_pool[type].Count > 0)
        {
            Unit unit = _pool[type].Dequeue();
            unit.gameObject.SetActive(true);
            return unit;
        }
        // Пул пуст — создаём новый
        return CreateUnit(_dataMap[type]);
    }

    /// <summary>Возвращает юнита в пул.</summary>
    public void Return(Unit unit)
    {
        unit.gameObject.SetActive(false);
        _pool[unit.HeroType].Enqueue(unit);
    }
}
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Спавнит 3D-здания на сцене BaseScene из данных BaseManager.
/// Запускается при старте сцены. Если игрок вернулся на базу позже —
/// здания пересоздаются заново.
/// </summary>
public class BaseBuilder : MonoBehaviour
{
    [Header("Префаб")]
    [Tooltip("Базовый prefab здания (куб с BuildingView). Цвет/размер настраивается при спавне.")]
    [SerializeField] private GameObject _buildingPrefab;

    [Header("Контейнер")]
    [Tooltip("Родительский transform для всех спавненых зданий")]
    [SerializeField] private Transform _container;

    private readonly List<BuildingView> _spawnedViews = new();

    private void Start()
    {
        if (BaseManager.Instance == null)
        {
            Debug.LogError("[BaseBuilder] BaseManager не найден на сцене!", this);
            return;
        }

        SpawnAllBuildings();

        // Подписываемся на изменения (для будущего апгрейда)
        BaseManager.OnBuildingsChanged += OnBuildingsChanged;
    }

    private void OnDestroy()
    {
        BaseManager.OnBuildingsChanged -= OnBuildingsChanged;
    }

    private void SpawnAllBuildings()
    {
        // Чистим если что-то было
        foreach (var v in _spawnedViews)
            if (v != null) Destroy(v.gameObject);
        _spawnedViews.Clear();

        var buildings = BaseManager.Instance.Buildings;

        foreach (var inst in buildings)
        {
            BuildingDataSO data = BaseManager.Instance.GetDataFor(inst.Type);
            if (data == null) continue;

            GameObject go = Instantiate(_buildingPrefab, _container != null ? _container : transform);
            go.name = $"Building_{inst.Type}";

            BuildingView view = go.GetComponent<BuildingView>();
            if (view != null)
            {
                view.Setup(inst, data);
                _spawnedViews.Add(view);
            }
        }

        Debug.Log($"[BaseBuilder] Заспавнено зданий: {_spawnedViews.Count}", this);
    }

    private void OnBuildingsChanged()
    {
        // Простой подход — пересоздаём всё. В Дне 11 оптимизируем.
        SpawnAllBuildings();
    }
}

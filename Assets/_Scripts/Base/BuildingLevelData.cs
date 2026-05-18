using System;
using UnityEngine;

/// <summary>
/// Базовые данные одного уровня здания.
/// Содержит только общие для всех типов поля: стоимость апгрейда и время.
/// Подклассы (ProducerLevelData, StorageLevelData) добавляют специфичные поля.
/// </summary>
[Serializable]
public class BuildingLevelData
{
    public int Level;

    [Header("Стоимость апгрейда")]
    public int CostGold;
    public int CostIron;
    public int CostFood;

    [Header("Время апгрейда (сек)")]
    public float UpgradeTimeSeconds;
}

/// <summary>Уровень здания-производителя ресурсов (GoldMine, IronMine).</summary>
[Serializable]
public class ProducerLevelData : BuildingLevelData
{
    [Header("Производство")]
    [Tooltip("Сколько ресурса в секунду")]
    public int ProductionPerSecond;
}

/// <summary>Уровень здания-хранилища (Storage).</summary>
[Serializable]
public class StorageLevelData : BuildingLevelData
{
    [Header("Хранилище")]
    [Tooltip("Лимит ресурсов которые могут храниться")]
    public int StorageCapacity;
}

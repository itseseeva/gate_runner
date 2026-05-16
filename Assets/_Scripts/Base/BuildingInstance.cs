using System;
using UnityEngine;

/// <summary>
/// Состояние конкретного здания на базе игрока.
/// Сериализуется в JSON для сохранения через PlayerPrefs.
/// </summary>
[Serializable]
public class BuildingInstance
{
    public string Id;             // уникальный (HQ, GoldMine_1, etc.)
    public BuildingType Type;     // тип
    public int Level = 1;         // текущий уровень

    // Позиция на сцене базы
    public Vector3 Position;

    // Апгрейд
    public bool IsUpgrading;
    public long UpgradeEndTimeTicks;  // DateTime.UtcNow.Ticks при завершении

    // Production
    public long LastCollectTicks;     // когда последний раз обновляли pending
    public int PendingResources;      // накоплено, не собрано

    /// <summary>Свойство-обёртка над тиками для удобства.</summary>
    public DateTime UpgradeEndTime
    {
        get => new DateTime(UpgradeEndTimeTicks, DateTimeKind.Utc);
        set => UpgradeEndTimeTicks = value.Ticks;
    }

    public DateTime LastCollectTime
    {
        get => new DateTime(LastCollectTicks, DateTimeKind.Utc);
        set => LastCollectTicks = value.Ticks;
    }
}

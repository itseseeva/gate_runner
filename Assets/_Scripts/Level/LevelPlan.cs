using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// План уровня — список того что должно появиться по мере прохождения.
/// Создаётся LevelGenerator-ом при старте сцены, потом lazy-spawn'ится.
/// </summary>
public class LevelPlan
{
    public readonly List<WaveData> Waves = new();
    public readonly List<GateData> Gates = new();
    public float LevelLength;
}

/// <summary>Данные о волне врагов: где, сколько, насколько сильные.</summary>
public class WaveData
{
    public float Z;              // позиция по Z где спавнится
    public int   EnemyCount;     // сколько врагов
    public float HealthMultiplier; // множитель HP (от Z + от уровня игрока)
    public bool  Spawned;        // уже создан?
}

/// <summary>Данные о воротах: где, какой prefab, какие настройки если Quantity.</summary>
public class GateData
{
    public float      Z;
    public float      X;          // -1.2 (слева) или +1.2 (справа)
    public GameObject Prefab;     // какой prefab спавнить
    public bool       NeedsRandomQuantity; // если true — настройки настраиваются на лету
    public HeroType   HeroType;
    public bool       IsMultiply;
    public int        Value;
    public bool       Spawned;
}

using UnityEngine;

/// <summary>
/// Структура со всеми производными статами героя.
/// Заполняется через HeroStatsCalculator.Compute().
/// </summary>
public struct HeroStats
{
    public int   MaxHP;
    public int   MaxMana;
    public int   PhysATK;
    public int   MagATK;
    public int   DEF;
    public float Speed;
    public float CritChance;   // в процентах, например 5.9
    public float CritDMG;      // в процентах, например 150
    public float Accuracy;     // базово 100
    public float Resistance;   // базово 0
}

/// <summary>
/// Статический класс — считает производные статы и уровни.
/// Не MonoBehaviour — можно вызывать откуда угодно без поиска объекта в сцене.
/// </summary>
public static class HeroStatsCalculator
{
    // ─── Таблица XP ────────────────────────────────────────────────
    // Индекс 0 = порог для уровня 2 (нужно набрать 150 суммарного XP)
    // Индекс N = порог для уровня (N+2)
    // Макс уровень = 30
    private static readonly int[] _xpThresholds = new int[]
    {
           150,  // уровень 2
           689,  // уровень 3
          2189,  // уровень 4
          3689,  // уровень 5
          5190,  // уровень 6
          8906,  // уровень 7
         12622,  // уровень 8
         16338,  // уровень 9
         20054,  // уровень 10
         23770,  // уровень 11
         30836,  // уровень 12
         37902,  // уровень 13
         44968,  // уровень 14
         52034,  // уровень 15
         59100,  // уровень 16
         70200,  // уровень 17
         81300,  // уровень 18
         92400,  // уровень 19
        103500,  // уровень 20
        114600,  // уровень 21
        130360,  // уровень 22
        146120,  // уровень 23
        161880,  // уровень 24
        177640,  // уровень 25
        193400,  // уровень 26
        214220,  // уровень 27
        235040,  // уровень 28
        255860,  // уровень 29
        276680,  // уровень 30 (максимум)
    };

    public const int MaxLevel = 30;

    // ─── Уровень и XP ──────────────────────────────────────────────

    /// <summary>
    /// Возвращает текущий уровень героя по суммарному XP.
    /// Начинается с 1, максимум MaxLevel.
    /// </summary>
    public static int GetLevel(int totalXP)
    {
        int level = 1;
        for (int i = 0; i < _xpThresholds.Length; i++)
        {
            if (totalXP >= _xpThresholds[i])
                level = i + 2;
            else
                break;
        }
        return Mathf.Min(level, MaxLevel);
    }

    /// <summary>
    /// Суммарный XP нужный чтобы достичь следующего уровня.
    /// Возвращает 0 если уже максимальный уровень.
    /// </summary>
    public static int GetXPForNextLevel(int currentLevel)
    {
        int index = currentLevel - 1; // уровень 1 → индекс 0
        if (index >= _xpThresholds.Length) return 0; // макс уровень
        return _xpThresholds[index];
    }

    /// <summary>
    /// Прогресс внутри текущего уровня от 0.0 до 1.0 (для прогресс-бара).
    /// </summary>
    public static float GetLevelProgress(int totalXP, int currentLevel)
    {
        if (currentLevel >= MaxLevel) return 1f;

        int prevThreshold = currentLevel > 1 ? _xpThresholds[currentLevel - 2] : 0;
        int nextThreshold = _xpThresholds[currentLevel - 1];

        int range = nextThreshold - prevThreshold;
        if (range <= 0) return 1f;

        return Mathf.Clamp01((float)(totalXP - prevThreshold) / range);
    }

    /// <summary>
    /// XP внутри текущего уровня (например 450 из 1000).
    /// Используется для отображения "XP: 450 / 1000".
    /// </summary>
    public static int GetXPWithinLevel(int totalXP, int currentLevel)
    {
        if (currentLevel <= 1) return totalXP;
        int prevThreshold = _xpThresholds[currentLevel - 2];
        return totalXP - prevThreshold;
    }

    /// <summary>
    /// XP нужно для перехода ВНУТРИ текущего уровня (не суммарно).
    /// Например: "нужно 1000 XP на этом уровне".
    /// </summary>
    public static int GetXPNeededForCurrentLevel(int currentLevel)
    {
        if (currentLevel >= MaxLevel) return 0;
        int prevThreshold = currentLevel > 1 ? _xpThresholds[currentLevel - 2] : 0;
        int nextThreshold = _xpThresholds[currentLevel - 1];
        return nextThreshold - prevThreshold;
    }

    // ─── Формулы статов ────────────────────────────────────────────

    /// <summary>
    /// Считает все производные статы героя по базовым данным из SO + прокачанным из Save.
    /// Вызывай это каждый раз когда нужно показать статы в UI.
    /// </summary>
    public static HeroStats Compute(HeroDefinitionSO def, HeroSaveData save)
    {
        HeroStats s;

        // Основные статы из save
        int str = save.strength;
        int agi = save.agility;
        int intel = save.intellect;
        int end = save.endurance;

        // ┌──────────────────────────────────────────────────────────┐
        // │  Формулы производных статов                              │
        // └──────────────────────────────────────────────────────────┘
        s.MaxHP       = def.MaxHP     + (end   * 10) + (str * 5);
        s.MaxMana     = def.BaseMana  + (intel * 5);
        s.PhysATK     = def.Attack    + (str   * 2);
        s.MagATK      = def.Attack    + (intel * 2);
        s.DEF         = Mathf.RoundToInt(def.Defense + (end * 1.5f));
        s.Speed       = def.BaseSpeed + (agi * 0.5f);
        s.CritChance  = 5f            + (agi * 0.3f);   // %
        s.CritDMG     = 150f;                            // % фиксировано
        s.Accuracy    = 100f;                            // фиксировано
        s.Resistance  = end * 0.5f;

        return s;
    }
}

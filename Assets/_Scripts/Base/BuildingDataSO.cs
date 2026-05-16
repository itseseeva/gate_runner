using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Шаблон здания. ScriptableObject — статичные данные.
/// На каждый тип здания создаётся один ассет (HQ_Data, GoldMine_Data, etc.).
/// Содержит таблицу уровней с балансом.
/// </summary>
[CreateAssetMenu(fileName = "BuildingData", menuName = "MGR/Base/Building Data")]
public class BuildingDataSO : ScriptableObject
{
    [Header("Идентификация")]
    public BuildingType Type;
    public string DisplayName = "Здание";
    public string Description = "Описание";

    [Header("Визуал (заглушки)")]
    [Tooltip("Цвет примитива на базе (пока нет 3D-моделей)")]
    public Color PrimitiveColor = Color.gray;

    [Tooltip("Высота примитива (для масштабирования куба)")]
    public float PrimitiveHeight = 1.5f;

    [Header("Уровни (баланс)")]
    [Tooltip("Уровни 1-10 с балансом. Элемент 0 = уровень 1.")]
    public List<BuildingLevelData> Levels = new();

    /// <summary>Получает данные конкретного уровня. Защита от выхода за пределы.</summary>
    public BuildingLevelData GetLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, Levels.Count - 1);
        return Levels.Count > 0 ? Levels[idx] : null;
    }

    /// <summary>Максимальный уровень здания (последний в таблице).</summary>
    public int MaxLevel => Levels.Count;
}

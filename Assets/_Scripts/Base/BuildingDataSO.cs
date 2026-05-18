using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Базовый шаблон здания (HQ, Barracks, Training).
/// Подклассы используют свои списки уровней с дополнительными полями.
/// </summary>
[CreateAssetMenu(fileName = "BuildingData_Basic", menuName = "MGR/Base/Building Data (Basic)")]
public class BuildingDataSO : ScriptableObject
{
    [Header("Идентификация")]
    public BuildingType Type;
    public string DisplayName = "Building";
    public string Description = "Description";

    [Header("Визуал")]
    public Color PrimitiveColor = Color.gray;
    public float PrimitiveHeight = 1.5f;

    [Header("Уровни (баланс) — используется только для HQ/Barracks/Training")]
    [SerializeField] protected List<BuildingLevelData> _levels = new();

    public virtual BuildingLevelData GetLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _levels.Count - 1);
        return _levels.Count > 0 ? _levels[idx] : null;
    }

    public virtual int MaxLevel => _levels.Count;
}

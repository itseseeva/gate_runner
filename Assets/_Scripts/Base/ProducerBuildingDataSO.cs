using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BuildingData_Producer", menuName = "MGR/Base/Building Data (Producer)")]
public class ProducerBuildingDataSO : BuildingDataSO
{
    private void OnValidate()
    {
        // Очищаем базовый список — Producer использует свой _producerLevels
        if (_levels != null && _levels.Count > 0) _levels.Clear();
    }

    [Header("Уровни производства")]
    [SerializeField] private List<ProducerLevelData> _producerLevels = new();

    public override BuildingLevelData GetLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _producerLevels.Count - 1);
        return _producerLevels.Count > 0 ? _producerLevels[idx] : null;
    }

    public override int MaxLevel => _producerLevels.Count;

    public int GetProduction(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _producerLevels.Count - 1);
        return _producerLevels.Count > 0 ? _producerLevels[idx].ProductionPerSecond : 0;
    }
}

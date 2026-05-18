using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BuildingData_Storage", menuName = "MGR/Base/Building Data (Storage)")]
public class StorageBuildingDataSO : BuildingDataSO
{
    private void OnValidate()
    {
        if (_levels != null && _levels.Count > 0) _levels.Clear();
    }

    [Header("Уровни хранилища")]
    [SerializeField] private List<StorageLevelData> _storageLevels = new();

    public override BuildingLevelData GetLevel(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _storageLevels.Count - 1);
        return _storageLevels.Count > 0 ? _storageLevels[idx] : null;
    }

    public override int MaxLevel => _storageLevels.Count;

    public int GetCapacity(int level)
    {
        int idx = Mathf.Clamp(level - 1, 0, _storageLevels.Count - 1);
        return _storageLevels.Count > 0 ? _storageLevels[idx].StorageCapacity : 0;
    }
}

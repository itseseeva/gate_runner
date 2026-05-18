using UnityEngine;

/// <summary>
/// Производит ресурсы каждую секунду пока игра запущена.
/// Только здания типа GoldMine / IronMine с ProducerBuildingDataSO производят.
/// </summary>
public class BuildingProducer : MonoBehaviour
{
    [SerializeField] private float _tickInterval = 1f;
    private float _timer;

    private void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < _tickInterval) return;

        _timer = 0f;
        ProduceOneSecond();
    }

    private void ProduceOneSecond()
    {
        if (BaseManager.Instance == null || ResourceManager.Instance == null) return;

        int storageCapacity = GetStorageCapacity();

        foreach (var b in BaseManager.Instance.Buildings)
        {
            if (b.IsUpgrading) continue;

            // Производят только Producer-здания
            var producer = BaseManager.Instance.GetDataFor(b.Type) as ProducerBuildingDataSO;
            if (producer == null) continue;

            int produced = producer.GetProduction(b.Level);
            if (produced <= 0) continue;

            // Применяем production buff если активен
            if (ResourceManager.Instance.IsBuffActive)
            {
                produced = Mathf.RoundToInt(produced * ResourceManager.Instance.BuffMultiplier);
            }

            if (b.Type == BuildingType.GoldMine)
            {
                if (PlayerDataManager.Instance != null
                    && PlayerDataManager.Instance.Gold < storageCapacity)
                {
                    PlayerDataManager.Instance.AddGold(produced);
                }
            }
            else if (b.Type == BuildingType.IronMine)
            {
                if (ResourceManager.Instance.Iron < storageCapacity)
                {
                    ResourceManager.Instance.AddIron(produced);
                }
            }
        }
    }

    /// <summary>Суммарный лимит всех Storage-зданий. По умолчанию 10000 если нет Storage.</summary>
    private int GetStorageCapacity()
    {
        int total = 0;
        bool hasStorage = false;

        foreach (var b in BaseManager.Instance.Buildings)
        {
            var storage = BaseManager.Instance.GetDataFor(b.Type) as StorageBuildingDataSO;
            if (storage == null) continue;

            hasStorage = true;
            total += storage.GetCapacity(b.Level);
        }

        return hasStorage ? total : 10000;
    }
}

using UnityEngine;

/// <summary>
/// Производит ресурсы каждую секунду пока игра запущена.
/// Висит на BaseManager-объекте, тикает все здания типа GoldMine/IronMine.
///
/// MVP — без offline-производства. В Дне 9 расширим под закрытое состояние игры.
/// </summary>
public class BuildingProducer : MonoBehaviour
{
    [Tooltip("Интервал тиков производства (секунды). 1 = каждую секунду.")]
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
        if (BaseManager.Instance == null || ResourceManager.Instance == null)
        {
            Debug.LogWarning("[Producer] Менеджеры null");
            return;
        }

        // Storage capacity — сумма лимитов всех Storage-зданий
        int storageCapacity = GetStorageCapacity();
        Debug.Log($"[Producer] Tick. Storage capacity: {storageCapacity}");

        foreach (var b in BaseManager.Instance.Buildings)
        {
            // Здание апгрейдится — не производит
            if (b.IsUpgrading) continue;

            var data = BaseManager.Instance.GetDataFor(b.Type);
            if (data == null) continue;

            var levelData = data.GetLevel(b.Level);
            if (levelData == null) continue;

            Debug.Log($"[Producer]   {b.Type} Lvl {b.Level}: production = {levelData.ProductionPerSecond}");

            if (levelData.ProductionPerSecond <= 0) continue;

            int produced = levelData.ProductionPerSecond;

            // Добавляем по типу здания
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

    /// <summary>Суммарный лимит всех Storage-зданий. По умолчанию 10000.</summary>
    private int GetStorageCapacity()
    {
        int total = 0;
        bool hasStorage = false;

        foreach (var b in BaseManager.Instance.Buildings)
        {
            if (b.Type != BuildingType.Storage) continue;
            hasStorage = true;

            var data = BaseManager.Instance.GetDataFor(b.Type);
            if (data == null) continue;

            var levelData = data.GetLevel(b.Level);
            if (levelData != null) total += levelData.StorageCapacity;
        }

        // Если Storage нет — дефолтный лимит чтобы что-то накапливалось
        return hasStorage ? total : 10000;
    }
}

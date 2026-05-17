using System;

/// <summary>
/// Данные одного уровня здания.
/// Не все поля нужны для всех типов — см. подсказки.
/// </summary>
[Serializable]
public class BuildingLevelData
{
    public int Level;

    [UnityEngine.Header("Стоимость апгрейда до этого уровня")]
    public int CostGold;
    public int CostIron;
    public int CostFood;

    [UnityEngine.Header("Время апгрейда (сек)")]
    public float UpgradeTimeSeconds;

    [UnityEngine.Header("ТОЛЬКО для GoldMine / IronMine")]
    [UnityEngine.Tooltip("Сколько ресурса в секунду. У HQ/Barracks/Training/Storage = 0")]
    public int ProductionPerSecond;

    [UnityEngine.Header("ТОЛЬКО для Storage")]
    [UnityEngine.Tooltip("Лимит хранилища. У всех остальных = 0")]
    public int StorageCapacity;
}

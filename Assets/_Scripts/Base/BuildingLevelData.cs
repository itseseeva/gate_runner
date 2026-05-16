using System;

/// <summary>
/// Данные одного уровня здания.
/// Стоимость апгрейда до этого уровня, что производит, время постройки.
/// </summary>
[Serializable]
public class BuildingLevelData
{
    public int Level;

    // Стоимость улучшения до этого уровня
    public int CostGold;
    public int CostIron;
    public int CostFood;

    // Время апгрейда (в секундах для теста; будут реальные часы потом)
    public float UpgradeTimeSeconds;

    // Что производит здание на этом уровне (для GoldMine/IronMine)
    public int ProductionPerSecond;

    // Для Storage — лимит хранилища
    public int StorageCapacity;
}

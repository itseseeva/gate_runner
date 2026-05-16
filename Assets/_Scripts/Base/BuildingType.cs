/// <summary>
/// Типы зданий на базе. Используется в BuildingDataSO и BuildingInstance.
/// Добавляй новые типы при расширении базы.
/// </summary>
public enum BuildingType
{
    HQ,         // Главное — ограничивает уровни остальных
    GoldMine,   // Производит золото
    IronMine,   // Производит iron
    Barracks,   // Открывает героев в магазине
    Training,   // Бонус урона юнитам в бою
    Storage,    // Хранилище ресурсов (лимит)
}

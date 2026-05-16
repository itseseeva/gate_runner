using System;
using System.Collections.Generic;

/// <summary>
/// DTO для сохранения всего состояния базы в JSON.
/// JsonUtility не сериализует List напрямую, нужен класс-обёртка.
/// </summary>
[Serializable]
public class BaseState
{
    public List<BuildingInstance> Buildings = new();
    public long LastSaveTicks;  // когда сохраняли (для offline-расчёта в День 9)
}

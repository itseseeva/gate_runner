/// <summary>
/// Стихия юнита/снаряда/врага.
/// None = нейтральная, базовый урон без бонусов и статусов.
/// Fire/Ice/Lightning — наносят +50% урона и накладывают соответствующий статус.
/// </summary>
public enum ElementType
{
    None,
    Fire,
    Ice,
    Lightning,
}

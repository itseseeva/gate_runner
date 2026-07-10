/// <summary>
/// Тип цифры урона — определяет цвет, размер, префикс.
/// </summary>
public enum DamageNumberType
{
    Normal,     // белый
    Critical,   // жёлтый, большой, с "!"
    Burn,       // оранжевый, маленький — для тиков огня
    Freeze,     // голубой
    Shock,      // фиолетовый
    Heal,       // зелёный, с "+"
}

using UnityEngine;

/// <summary>
/// Настройки одного декора: размер и отступ от дороги.
/// Вешается на префаб декора — так каждый тип знает свой масштаб,
/// а спавнер не пытается угадать по габаритам.
/// </summary>
public class DecorSettings : MonoBehaviour
{
    [Tooltip("Целевой масштаб этого декора в сцене")]
    public float Scale = 0.3f;

    [Tooltip("Дополнительный отступ от края дороги (для широкого декора вроде камней)")]
    public float ExtraMargin = 0f;
}

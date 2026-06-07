using UnityEngine;

/// <summary>
/// Конфиг VFX эффектов ударов милишников + смерти юнита.
/// Дальники (маг, лучник) сюда НЕ входят — у них визуал попадания
/// идёт через снаряд (HitEffectPool по стихии), а не отсюда.
/// Создаётся через Assets - Create - Game - VFX Config
/// </summary>
[CreateAssetMenu(fileName = "VfxConfig", menuName = "Game/VFX Config")]
public class VfxConfig : ScriptableObject
{
    [Header("Эффекты ударов милишников")]
    public GameObject WarriorHitVfx;
    public GameObject TankHitVfx;
    public GameObject AssassinHitVfx;

    [Header("Смерть юнита")]
    public GameObject UnitDeathVfx;
}

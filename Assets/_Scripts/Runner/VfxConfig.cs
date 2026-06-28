using UnityEngine;

/// <summary>
/// Конфиг VFX эффектов ударов милишников + смерти юнита.
/// Дальники (маг, лучник) сюда НЕ входят — у них визуал попадания
/// идёт через снаряд (HitEffectPool по стихии), а не отсюда.
/// Создаётся через Assets → Create → Game → VFX Config
/// </summary>
[CreateAssetMenu(fileName = "VfxConfig", menuName = "Game/VFX Config")]
public class VfxConfig : ScriptableObject
{
    [Header("Эффекты ударов милишников (на враге)")]
    public GameObject WarriorHitVfx;
    public GameObject TankHitVfx;
    public GameObject AssassinHitVfx;

    [Header("Слеш-эффекты Воина (на герое, по стихии)")]
    public GameObject WarriorSlashFire;
    public GameObject WarriorSlashIce;
    public GameObject WarriorSlashLightning;

    [Header("Слеш-эффекты Танка (на герое, по стихии)")]
    public GameObject TankSlashFire;
    public GameObject TankSlashIce;
    public GameObject TankSlashLightning;

    [Header("Слеш-эффекты Ассасина (на герое, по стихии)")]
    public GameObject AssassinSlashFire;
    public GameObject AssassinSlashIce;
    public GameObject AssassinSlashLightning;

    [Header("Смерть юнита")]
    public GameObject UnitDeathVfx;

    /// <summary>
    /// Возвращает слеш воина по стихии.
    /// Если стихийный не назначен — возвращает null (слеш не спавнится).
    /// </summary>
    public GameObject GetWarriorSlash(ElementType element) => element switch
    {
        ElementType.Fire      => WarriorSlashFire,
        ElementType.Ice       => WarriorSlashIce,
        ElementType.Lightning => WarriorSlashLightning,
        _                     => null,
    };

    /// <summary>
    /// Возвращает слеш танка по стихии.
    /// Если стихийный не назначен — возвращает null (слеш не спавнится).
    /// </summary>
    public GameObject GetTankSlash(ElementType element) => element switch
    {
        ElementType.Fire      => TankSlashFire,
        ElementType.Ice       => TankSlashIce,
        ElementType.Lightning => TankSlashLightning,
        _                     => null,
    };

    /// <summary>
    /// Возвращает слеш ассасина по стихии.
    /// Если стихийный не назначен — возвращает null (слеш не спавнится).
    /// </summary>
    public GameObject GetAssassinSlash(ElementType element) => element switch
    {
        ElementType.Fire      => AssassinSlashFire,
        ElementType.Ice       => AssassinSlashIce,
        ElementType.Lightning => AssassinSlashLightning,
        _                     => null,
    };
}

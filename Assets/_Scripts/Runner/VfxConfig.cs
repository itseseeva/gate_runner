using UnityEngine;

/// <summary>
/// Конфиг VFX эффектов для каждого типа юнита.
/// Создаётся через Assets → Create → Game → VFX Config
/// </summary>
[CreateAssetMenu(fileName = "VfxConfig", menuName = "Game/VFX Config")]
public class VfxConfig : ScriptableObject
{
    [Header("Эффекты ударов")]
    public GameObject WarriorHitVfx;
    public GameObject TankHitVfx;
    public GameObject MageHitVfx;
    public GameObject ArcherHitVfx;
    public GameObject AssassinHitVfx;
}

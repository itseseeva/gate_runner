using UnityEngine;

[System.Serializable]
public class DecorEntry
{
    public GameObject Prefab;

    [Tooltip("Масштаб этого декора в сцене")]
    public float Scale = 0.3f;

    [Tooltip("Разброс масштаба: 0.25 = ±25%")]
    public float ScaleVariation = 0.25f;

    [Tooltip("Спавнить по всей ширине дороги, а не только у краёв")]
    public bool SpawnOnRoadCenter = false;

    [Tooltip("Не крутить случайно по Y — для забора, бордюров и всего, что должно стоять ровно")]
    public bool NoRandomRotation = false;

    [Tooltip("Шанс появления при спавне (1 = всегда, 0.1 = в 10% случаев)")]
    [Range(0f, 1f)]
    public float SpawnChance = 1f;

    [Tooltip("Вес при выборе: чем больше, тем чаще выпадает")]
    [Min(0.01f)]
    public float Weight = 1f;
}

/// <summary>
/// Данные одного биома: как выглядит мир этой локации.
/// Один ассет на биом (Forest, Desert, Ice...).
/// </summary>
[CreateAssetMenu(fileName = "Biome_Data", menuName = "MGR/Biome")]
public class BiomeSO : ScriptableObject
{
    [Header("Основное")]
    [Tooltip("Название биома для удобства")]
    [SerializeField] private string _biomeName = "Forest";

    [Header("Визуал")]
    [Tooltip("Цвет тумана")]
    [SerializeField] private Color _fogColor = Color.gray;

    [Tooltip("Skybox биома (можно оставить пустым, если общий)")]
    [SerializeField] private Material _skybox;

    [Header("Трава")]
    [Tooltip("Модели травы по бокам дороги.")]
    [SerializeField] private DecorEntry[] _grassDecor;

    [Header("Цветы")]
    [Tooltip("Модели цветов по бокам дороги.")]
    [SerializeField] private DecorEntry[] _flowerDecor;

    [Header("Кусты")]
    [Tooltip("Модели кустов по бокам дороги.")]
    [SerializeField] private DecorEntry[] _bushDecor;

    [Header("Камни")]
    [Tooltip("Модели камней по бокам дороги.")]
    [SerializeField] private DecorEntry[] _rockDecor;

    [Header("Деревья")]
    [Tooltip("Деревья по бокам дороги.")]
    [SerializeField] private DecorEntry[] _treeDecor;

    [Header("Декор на дороге")]
    [Tooltip("Мелкий декор на самой дороге: трава, камешки.")]
    [SerializeField] private DecorEntry[] _roadDecor;

    [Header("Забор вдоль дороги")]
    [Tooltip("Секция забора. Ставится сплошной линией по обеим сторонам, не конкурирует с остальным декором.")]
    [SerializeField] private DecorEntry _fence;

    public string       BiomeName    => _biomeName;
    public Color         FogColor     => _fogColor;
    public Material      Skybox       => _skybox;
    public DecorEntry[]  GrassDecor   => _grassDecor;
    public DecorEntry[]  FlowerDecor  => _flowerDecor;
    public DecorEntry[]  BushDecor    => _bushDecor;
    public DecorEntry[]  RockDecor    => _rockDecor;
    public DecorEntry[]  TreeDecor    => _treeDecor;
    public DecorEntry[]  RoadDecor    => _roadDecor;
    public DecorEntry    Fence        => _fence;
}

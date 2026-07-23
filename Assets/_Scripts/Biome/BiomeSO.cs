using UnityEngine;

[System.Serializable]
public class DecorEntry
{
    public GameObject Prefab;

    [Tooltip("Масштаб этого декора в сцене")]
    public float Scale = 0.3f;

    [Tooltip("Разброс масштаба: 0.25 = ±25%")]
    public float ScaleVariation = 0.25f;

    [Tooltip("Отступ от края дороги (широкому декору больше)")]
    public float SideMargin = 1.5f;
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
    [Tooltip("Материал дороги для этого биома")]
    [SerializeField] private Material _roadMaterial;

    [Tooltip("Цвет тумана")]
    [SerializeField] private Color _fogColor = Color.gray;

    [Tooltip("Skybox биома (можно оставить пустым, если общий)")]
    [SerializeField] private Material _skybox;

    [Header("Декор")]
    [SerializeField] private DecorEntry[] _decor;

    [Header("Декор на дороге")]
    [Tooltip("Мелкий декор на самой дороге: трава, камешки. Спавнится у краёв дороги.")]
    [SerializeField] private DecorEntry[] _roadDecor;

    public string       BiomeName    => _biomeName;
    public Material      RoadMaterial => _roadMaterial;
    public Color         FogColor     => _fogColor;
    public Material      Skybox       => _skybox;
    public DecorEntry[]  Decor        => _decor;
    public DecorEntry[]  RoadDecor    => _roadDecor;
}

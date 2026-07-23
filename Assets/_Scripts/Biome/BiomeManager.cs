using UnityEngine;

/// <summary>
/// Применяет биом к сцене: материал дороги, туман, skybox.
/// Один на сцену. Пока просто перекрашивает мир под текущий биом.
/// В следующих фазах сюда добавится декор и сегменты.
/// </summary>
public class BiomeManager : MonoBehaviour
{
    public static BiomeManager Instance { get; private set; }

    [Header("Ссылки на сцену")]
    [Tooltip("Renderer дороги — того самого Plane, по которому идут враги")]
    [SerializeField] private Renderer _roadRenderer;

    [Header("Биом этого уровня")]
    [Tooltip("Какой биом применить при старте. Позже будет задаваться прогрессом уровня.")]
    [SerializeField] private BiomeSO _currentBiome;

    private BiomeSO _appliedBiome;

    private void Awake()
    {
        Instance = this;
        if (_currentBiome != null)
            ApplyBiome(_currentBiome);
    }

    /// <summary>Красит сцену под указанный биом.</summary>
    public void ApplyBiome(BiomeSO biome)
    {
        if (biome == null)
        {
            Debug.LogWarning("[BiomeManager] Передан пустой биом.", this);
            return;
        }

        _appliedBiome = biome;

        // Материал дороги
        if (_roadRenderer != null && biome.RoadMaterial != null)
            _roadRenderer.material = biome.RoadMaterial;

        // Туман
        RenderSettings.fogColor = biome.FogColor;

        // Skybox (только если задан в биоме)
        if (biome.Skybox != null)
            RenderSettings.skybox = biome.Skybox;

        Debug.Log($"[BiomeManager] Применён биом: {biome.BiomeName}", this);
    }

    /// <summary>Текущий применённый биом — пригодится декору и сегментам позже.</summary>
    public BiomeSO CurrentBiome => _appliedBiome != null ? _appliedBiome : _currentBiome;
}

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

    [Tooltip("Renderer левой части дороги")]
    [SerializeField] private Renderer _leftRoadRenderer;

    [Tooltip("Renderer правой части дороги")]
    [SerializeField] private Renderer _rightRoadRenderer;

    [Header("Биом этого уровня")]
    [Tooltip("Какой биом применить при старте. Позже будет задаваться прогрессом уровня.")]
    [SerializeField] private BiomeSO _currentBiome;

    private BiomeSO _appliedBiome;

    private void Awake()
    {
        Instance = this;
        ActivateRoadRenderers();

        BiomeSO biomeToApply = _currentBiome;
        if (LevelLauncher.Instance != null && LevelLauncher.Instance.SelectedBiome != null && LevelLauncher.Instance.SelectedBiome.VisualBiome != null)
        {
            biomeToApply = LevelLauncher.Instance.SelectedBiome.VisualBiome;
        }

        if (biomeToApply != null)
            ApplyBiome(biomeToApply);
    }

    private void OnEnable()
    {
        ActivateRoadRenderers();
    }

    /// <summary>Активирует объекты дороги и обочин в сцене, чтобы они отображались в игре.</summary>
    public void ActivateRoadRenderers()
    {
        EnableRendererObject(_roadRenderer);
        EnableRendererObject(_leftRoadRenderer);
        EnableRendererObject(_rightRoadRenderer);
    }

    private void EnableRendererObject(Renderer rend)
    {
        if (rend == null) return;

        if (!rend.gameObject.activeSelf)
            rend.gameObject.SetActive(true);

        rend.enabled = true;

        // Если нет скрипта скролла текстуры, добавляем его автоматической прокрутке
        if (rend.GetComponent<ScrollingTexture>() == null)
        {
            rend.gameObject.AddComponent<ScrollingTexture>();
        }
    }

    /// <summary>Устанавливает и активирует указанные объекты дороги в игре.</summary>
    public void SetRoadRenderers(Renderer road, Renderer left, Renderer right)
    {
        if (_roadRenderer != null && _roadRenderer != road) _roadRenderer.gameObject.SetActive(false);
        if (_leftRoadRenderer != null && _leftRoadRenderer != left) _leftRoadRenderer.gameObject.SetActive(false);
        if (_rightRoadRenderer != null && _rightRoadRenderer != right) _rightRoadRenderer.gameObject.SetActive(false);

        _roadRenderer = road;
        _leftRoadRenderer = left;
        _rightRoadRenderer = right;

        ActivateRoadRenderers();
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
        ActivateRoadRenderers();

        // Туман
        RenderSettings.fogColor = biome.FogColor;

        // Skybox (только если задан в биоме)
        if (biome.Skybox != null)
            RenderSettings.skybox = biome.Skybox;
    }

    /// <summary>Текущий применённый биом — пригодится декору и сегментам позже.</summary>
    public BiomeSO CurrentBiome => _appliedBiome != null ? _appliedBiome : _currentBiome;

    public Renderer RoadRenderer => _roadRenderer;
    public Renderer LeftRoadRenderer => _leftRoadRenderer;
    public Renderer RightRoadRenderer => _rightRoadRenderer;
}

using UnityEngine;

/// <summary>
/// Прокручивает текстуру по объекту в такт скорости мира.
/// Скорость считается в МИРОВЫХ метрах: скрипт сам учитывает Tiling материала
/// и масштаб объекта, поэтому дорога и земля с разными настройками
/// едут визуально одинаково без ручной подгонки.
/// </summary>
[RequireComponent(typeof(Renderer))]
public class ScrollingTexture : MonoBehaviour
{
    // Базовый размер Unity Plane в юнитах — от него считается реальная длина меша.
    private const float PlaneBaseSize = 10f;

    [Tooltip("Множитель скорости. 1 = текстура едет ровно со скоростью мира.")]
    [SerializeField] private float _speedMultiplier = 1f;

    [Tooltip("Ось скролла. Y — движение вперёд по дороге.")]
    [SerializeField] private bool _scrollY = true;

    private Renderer _renderer;
    private float _offset;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void LateUpdate()
    {
        if (GameStateManager.Instance != null && !GameStateManager.Instance.IsPlaying) return;
        if (_renderer == null) return;

        // Материал берём каждый кадр — BiomeManager может его подменить.
        Material mat = _renderer.material;

        // Сколько метров мира покрывает один цикл текстуры.
        // Длина меша по оси × сколько раз текстура на нём повторяется.
        Vector2 tiling = mat.GetTextureScale("_BaseMap");
        Vector3 scale = transform.lossyScale;

        float meshLength = (_scrollY ? scale.z : scale.x) * PlaneBaseSize;
        float tileCount  = _scrollY ? tiling.y : tiling.x;

        if (meshLength < 0.001f || tileCount < 0.001f) return;

        // Метров мира на один тайл текстуры.
        float metersPerTile = meshLength / tileCount;

        // Смещение в долях тайла = пройденные метры / метров на тайл.
        // Так скорость одинакова для любых Tiling и Scale.
        float metersThisFrame = WorldScroller.WorldSpeed * _speedMultiplier * Time.deltaTime;
        _offset += metersThisFrame / metersPerTile;

        if (_offset > 1f) _offset -= 1f;

        Vector2 uv = _scrollY ? new Vector2(0f, -_offset) : new Vector2(-_offset, 0f);
        mat.SetTextureOffset("_BaseMap", uv);
    }
}

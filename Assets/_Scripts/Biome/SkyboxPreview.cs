using UnityEngine;

/// <summary>
/// Быстрый переключатель скайбокса для теста прямо в сцене.
/// Меняешь материал в инспекторе — небо меняется сразу, без окна Lighting.
/// В игре BiomeManager перезапишет скайбокс на тот, что в текущем биоме.
/// </summary>
[ExecuteAlways]
public class SkyboxPreview : MonoBehaviour
{
    [Tooltip("Скайбокс для теста в редакторе. Перетащи сюда материал — небо сменится сразу.")]
    [SerializeField] private Material _skybox;

    private Material _lastApplied;

    private void OnEnable()
    {
        Apply();
    }

    private void Update()
    {
        // В редакторе следим за сменой поля — применяем на лету.
        if (_skybox != _lastApplied) Apply();
    }

    private void Apply()
    {
        if (_skybox == null) return;
        RenderSettings.skybox = _skybox;
        _lastApplied = _skybox;
    }
}

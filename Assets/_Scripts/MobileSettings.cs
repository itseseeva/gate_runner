using UnityEngine;

/// <summary>
/// Базовые настройки для мобильного — снимает 30 FPS лимит.
/// Висит на любом объекте сцены, выполняется один раз при старте.
/// </summary>
public class MobileSettings : MonoBehaviour
{
    [SerializeField] private int _targetFrameRate = 60;

    private void Awake()
    {
        Application.targetFrameRate = _targetFrameRate;
        QualitySettings.vSyncCount  = 0;

        {}
    }
}

using UnityEngine;
using TMPro;

/// <summary>
/// Показывает текущий FPS и среднее значение в углу экрана.
/// Обновляется раз в 0.5 секунды чтобы цифры не дёргались.
/// Автоматически инициализируется при старте игры и сохраняется между сценами.
/// </summary>
public class FPSCounterUI : MonoBehaviour
{
    private static FPSCounterUI _instance;

    [SerializeField] private TextMeshProUGUI _label;

    [Tooltip("Как часто обновлять счётчик (секунды)")]
    [SerializeField] private float _updateInterval = 0.5f;

    [Tooltip("Показывать ли OnGUI счётчик справа сверху, если TextMeshProUGUI не привязан")]
    [SerializeField] private bool _enableOnGUIFallback = true;

    private float _accumulator;
    private int   _frames;
    private float _timeLeft;
    private float _currentFps = 60f;
    private Color _currentColor = Color.green;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoInit()
    {
        if (_instance == null)
        {
            var existing = FindFirstObjectByType<FPSCounterUI>();
            if (existing != null)
            {
                _instance = existing;
            }
            else
            {
                var go = new GameObject("[FPSCounter]");
                _instance = go.AddComponent<FPSCounterUI>();
                DontDestroyOnLoad(go);
            }
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (_label == null)
            _label = GetComponent<TextMeshProUGUI>();

        if (_label == null)
            _label = GetComponentInChildren<TextMeshProUGUI>(true);

        _timeLeft = _updateInterval;
    }

    private void Update()
    {
        _timeLeft   -= Time.deltaTime;
        _accumulator += Time.timeScale / Time.deltaTime;
        _frames++;

        if (_timeLeft <= 0f)
        {
            _currentFps = _frames > 0 ? _accumulator / _frames : 0f;

            if      (_currentFps >= 50f) _currentColor = new Color(0.4f, 1f, 0.4f);   // зелёный
            else if (_currentFps >= 30f) _currentColor = new Color(1f, 0.9f, 0.3f);   // жёлтый
            else                         _currentColor = new Color(1f, 0.4f, 0.3f);   // красный

            if (_label != null)
            {
                _label.text = $"FPS: {_currentFps:F0}";
                _label.color = _currentColor;
            }

            _timeLeft   = _updateInterval;
            _accumulator = 0f;
            _frames      = 0;
        }
    }

    private void OnGUI()
    {
        // Если привязан TMP-текст на Canvas, OnGUI не дублируем
        if (_label != null || !_enableOnGUIFallback) return;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.UpperRight
        };
        style.normal.textColor = _currentColor;

        // Справа сверху
        float width = 150f;
        float height = 40f;
        Rect rect = new Rect(Screen.width - width - 20f, 20f, width, height);

        GUI.Label(rect, $"FPS: {_currentFps:F0}", style);
    }
}

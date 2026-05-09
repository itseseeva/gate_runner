using UnityEngine;
using TMPro;

/// <summary>
/// Показывает текущий FPS и среднее значение в углу экрана.
/// Обновляется раз в 0.5 секунды чтобы цифры не дёргались.
/// </summary>
public class FPSCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;

    [Tooltip("Как часто обновлять счётчик (секунды)")]
    [SerializeField] private float _updateInterval = 0.5f;

    private float _accumulator;
    private int   _frames;
    private float _timeLeft;

    private void Start()
    {
        if (_label == null)
            _label = GetComponent<TextMeshProUGUI>();

        _timeLeft = _updateInterval;
    }

    private void Update()
    {
        if (_label == null) return;

        _timeLeft   -= Time.deltaTime;
        _accumulator += Time.timeScale / Time.deltaTime;
        _frames++;

        if (_timeLeft <= 0f)
        {
            float fps = _accumulator / _frames;
            _label.text = $"FPS: {fps:F0}";

            // Цвет в зависимости от FPS — зелёный/жёлтый/красный
            if      (fps >= 50f) _label.color = new Color(0.4f, 1f, 0.4f);   // зелёный
            else if (fps >= 30f) _label.color = new Color(1f, 0.9f, 0.3f);   // жёлтый
            else                 _label.color = new Color(1f, 0.4f, 0.3f);   // красный

            _timeLeft   = _updateInterval;
            _accumulator = 0f;
            _frames      = 0;
        }
    }
}

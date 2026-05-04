using UnityEngine;
using System.Collections;

/// <summary>
/// Эффект разбития стекла ворот.
/// Вызывается из BaseGate при срабатывании триггера.
/// </summary>
public class GateGlassEffect : MonoBehaviour
{
    [SerializeField] private float _duration  = 0.3f;
    [SerializeField] private float _scaleUp   = 1.5f;

    private Renderer _renderer;
    private Material _material;
    private Vector3  _startScale;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null)
        {
            _material   = _renderer.material; // делаем копию материала
            _startScale = transform.localScale;
        }
    }

    /// <summary>Запускает эффект разбития.</summary>
    public void Shatter()
    {
        StartCoroutine(ShatterCoroutine());
    }

    private IEnumerator ShatterCoroutine()
    {
        if (_material == null) yield break;

        float t = 0f;
        
        // URP использует _BaseColor вместо .color
        Color startColor = _material.HasProperty("_BaseColor") 
            ? _material.GetColor("_BaseColor") 
            : _material.color;

        while (t < _duration)
        {
            t += Time.deltaTime;
            float p = t / _duration;

            // Растёт в размерах
            transform.localScale = _startScale * Mathf.Lerp(1f, _scaleUp, p);

            // Растворяется (alpha → 0)
            Color c = startColor;
            c.a = Mathf.Lerp(startColor.a, 0f, p);
            
            if (_material.HasProperty("_BaseColor"))
                _material.SetColor("_BaseColor", c);
            else
                _material.color = c;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}

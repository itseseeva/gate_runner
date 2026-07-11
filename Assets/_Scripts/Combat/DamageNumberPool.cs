using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул цифр урона. Один на сцену.
/// Настройки спавна (offset, высота) — здесь, а не в самой цифре.
/// Так все цифры используют одни параметры без правки префаба каждой.
/// </summary>
public class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance { get; private set; }

    [Header("Пул")]
    [SerializeField] private DamageNumber _prefab;
    [SerializeField] private int _preloadCount = 20;

    [Header("Смещение спавна от цели")]
    [Tooltip("Насколько цифра сдвигается вбок от центра модели (метры)")]
    [SerializeField] private float _sideOffset = 0.45f;

    [Tooltip("Высота спавна над позицией цели (метры)")]
    [SerializeField] private float _spawnHeight = 1.8f;

    [Header("Форматирование эмодзи (Live Update)")]
    [Tooltip("Отступ между цифрой и эмодзи (em). Отрицательные значения двигают эмодзи влево!")]
    [Range(-2f, 2f)]
    public float EmojiSpacing = 0.5f;

    [Tooltip("Вертикальное смещение эмодзи (em)")]
    [Range(-2f, 2f)]
    public float EmojiVOffset = 0.25f;

    [Tooltip("Размер эмодзи (в процентах)")]
    [Range(10f, 200f)]
    public float EmojiSize = 65f;

    private readonly Queue<DamageNumber> _pool = new();
    


    private void Awake()
    {
        Instance = this;
        Preload();
    }

    private void Preload()
    {
        for (int i = 0; i < _preloadCount; i++)
        {
            DamageNumber dn = Instantiate(_prefab, transform);
            dn.gameObject.SetActive(false);
            _pool.Enqueue(dn);
        }
    }

    /// <summary>Показывает цифру, прикреплённую к цели (двигается вместе с ней).</summary>
    public void Spawn(int damage, Transform followTarget, DamageNumberType type = DamageNumberType.Normal)
    {
        DamageNumber dn;
        if (_pool.Count > 0)
        {
            dn = _pool.Dequeue();
        }
        else
        {
            dn = Instantiate(_prefab, transform);
        }

        dn.gameObject.SetActive(true);
        dn.Show(damage, followTarget, type, _sideOffset, _spawnHeight);
    }

    public void Return(DamageNumber dn)
    {
        if (dn == null) return;
        dn.gameObject.SetActive(false);
        _pool.Enqueue(dn);
    }
}

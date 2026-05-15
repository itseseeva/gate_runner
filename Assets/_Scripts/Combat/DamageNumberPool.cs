using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Пул цифр урона. Один на сцену.
/// При запросе берёт свободный объект, при окончании — возвращает в пул.
/// </summary>
public class DamageNumberPool : MonoBehaviour
{
    public static DamageNumberPool Instance { get; private set; }

    [SerializeField] private DamageNumber _prefab;
    [SerializeField] private int _preloadCount = 20;

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

    /// <summary>Показывает цифру урона в указанной позиции.</summary>
    public void Spawn(int damage, Vector3 worldPosition, bool isCritical = false)
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
        dn.Show(damage, worldPosition, isCritical);
    }

    public void Return(DamageNumber dn)
    {
        if (dn == null) return;
        dn.gameObject.SetActive(false);
        _pool.Enqueue(dn);
    }
}

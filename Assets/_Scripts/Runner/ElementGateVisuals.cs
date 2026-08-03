using UnityEngine;

/// <summary>
/// Справочник 3D-моделей элементальных ворот по стихиям.
/// Один ассет на проект: сюда складываются модели (Fire/Ice/Lightning),
/// а ворота (ElementGate) достают нужную по своей стихии.
/// Меняешь модель здесь — обновляется во всех воротах разом.
/// </summary>
[CreateAssetMenu(fileName = "ElementGateVisuals", menuName = "MGR/Element Gate Visuals")]
public class ElementGateVisuals : ScriptableObject
{
    [Header("Модели ворот по стихиям")]
    [Tooltip("Крутящаяся модель огненных ворот")]
    [SerializeField] private GameObject _fireModel;

    [Tooltip("Крутящаяся модель ледяных ворот")]
    [SerializeField] private GameObject _iceModel;

    [Tooltip("Крутящаяся модель молниевых ворот")]
    [SerializeField] private GameObject _lightningModel;

    /// <summary>
    /// Возвращает префаб модели для указанной стихии.
    /// None или отсутствующая модель → null (ворота просто не поставят меш).
    /// </summary>
    public GameObject GetModel(ElementType element)
    {
        return element switch
        {
            ElementType.Fire      => _fireModel,
            ElementType.Ice       => _iceModel,
            ElementType.Lightning => _lightningModel,
            _                     => null,
        };
    }
}

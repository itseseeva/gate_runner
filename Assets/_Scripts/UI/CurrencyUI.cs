using UnityEngine;
using TMPro;

public class CurrencyUI : MonoBehaviour
{
    [Header("Текст на экране")]
    public TextMeshProUGUI goldText;
    public TextMeshProUGUI crystalsText;

    void Start()
    {
        RefreshUI();
    }

    public void RefreshUI()
    {
        var save = SaveSystem.Instance.GetSaveData();
        goldText.text = $"Золото: {save.gold}";
        crystalsText.text = $"Кристаллы: {save.crystals}";
    }
}

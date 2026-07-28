using TMPro;
using UnityEngine;

public class GoldDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI goldText;

    private void Start()
    {
        if (GoldManager.Instance == null)
        {
            Debug.LogWarning("GoldManager instance not found in scene.");
            return;
        }

        GoldManager.Instance.OnGoldChanged += UpdateText;
        UpdateText(GoldManager.Instance.CurrentGold);
    }

    private void OnDestroy()
    {
        if (GoldManager.Instance != null)
            GoldManager.Instance.OnGoldChanged -= UpdateText;
    }

    private void UpdateText(int gold)
    {
        if (goldText != null)
            goldText.text = $"보유 골드: {gold} G";
    }
}

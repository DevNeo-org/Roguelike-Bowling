using TMPro;
using UnityEngine;

public class CollectionEntry : MonoBehaviour
{
    [SerializeField] private string itemName = "아이템";
    [TextArea]
    [SerializeField] private string description = "";

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private void OnEnable()
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        bool owned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemName);

        if (nameText != null)
            nameText.text = owned ? $"{itemName} <color=#7CFC9E>(보유)</color>" : itemName;

        if (descriptionText != null)
            descriptionText.text = description;
    }
}

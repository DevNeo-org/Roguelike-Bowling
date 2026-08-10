using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// 패시브 아이템 1개의 아이콘 슬롯. 보유 중일 때만 보이고, 마우스를 올리면 공용 설명 텍스트에
// 이름+설명을 표시한다 (ShopItem.cs의 SetDescriptionBar와 동일한 패턴).
public class PassiveItemIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string itemId = ItemIds.BalanceShoes;
    [SerializeField] private Image iconImage;

    private const string IconResourceFolder = "ShopIcons/";

    private string itemName = "";
    private string description = "";
    private TextMeshProUGUI descriptionBar;
    private string descriptionBarIdleText = "";

    public void SetDescriptionBar(TextMeshProUGUI bar, string idleText)
    {
        descriptionBar = bar;
        descriptionBarIdleText = idleText;
    }

    public void Refresh(List<ShopEntry> shopEntries)
    {
        bool owned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemId);
        gameObject.SetActive(owned);
        if (!owned || shopEntries == null) return;

        foreach (var entry in shopEntries)
        {
            if (entry.name != itemId) continue;

            itemName = entry.name;
            description = entry.description;

            if (iconImage != null)
            {
                Sprite icon = Resources.Load<Sprite>(IconResourceFolder + entry.icon);
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            break;
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (descriptionBar != null)
            descriptionBar.text = $"{itemName}: {description}";
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (descriptionBar != null)
            descriptionBar.text = descriptionBarIdleText;
    }
}

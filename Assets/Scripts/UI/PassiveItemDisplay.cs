using TMPro;
using UnityEngine;

// 좌측 액티브 아이템 패널 위에, 현재 보유 중인 패시브 아이템(예: 밸런스화) 아이콘을 보여준다.
// 액티브 아이템과 달리 클릭 동작이 없다 - 보유하면 항상 켜져 있는 효과이므로 아이콘은 보유 여부만
// 표시하고, 마우스를 올리면 하단 공용 설명 텍스트에 이름+설명이 뜬다.
public class PassiveItemDisplay : MonoBehaviour
{
    [SerializeField] private PassiveItemIcon[] icons;
    [SerializeField] private TextMeshProUGUI descriptionText;
    private const string IdleText = "";

    private void OnEnable()
    {
        RefreshAll();
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += HandleInventoryChanged;
            InventoryManager.Instance.OnItemRemoved += HandleInventoryChanged;
        }
    }

    private void OnDisable()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= HandleInventoryChanged;
            InventoryManager.Instance.OnItemRemoved -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged(string changedItemId) => RefreshAll();

    private void RefreshAll()
    {
        if (icons == null) return;

        var shopEntries = GameDataManager.LoadShopItems();
        foreach (var icon in icons)
        {
            if (icon == null) continue;
            icon.SetDescriptionBar(descriptionText, IdleText);
            icon.Refresh(shopEntries);
        }
    }
}

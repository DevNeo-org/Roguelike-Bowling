using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ShopItem : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string itemName = "아이템";
    [TextArea]
    [SerializeField] private string description = "";
    [SerializeField] private int price = 100;

    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI priceText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image iconImage;

    private TextMeshProUGUI descriptionBar;
    private string descriptionBarIdleText = "";

    private void Awake()
    {
        if (buyButton != null)
            buyButton.onClick.AddListener(OnBuyButtonClicked);
    }

    private void OnEnable()
    {
        RefreshDisplay();

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

    // 다른 카드의 구매/소모로 상충 관계가 바뀔 수 있으므로, 어떤 아이템이 바뀌든 항상 갱신한다.
    private void HandleInventoryChanged(string changedItemId)
    {
        RefreshDisplay();
    }

    public void Configure(string newName, string newDescription, int newPrice, Sprite newIcon)
    {
        itemName = newName;
        description = newDescription;
        price = newPrice;

        if (iconImage != null)
        {
            iconImage.sprite = newIcon;
            iconImage.enabled = newIcon != null;
        }

        RefreshDisplay();
    }

    // 상점 그리드의 공용 설명 표시줄을 등록한다. 마우스 오버 시 해당 표시줄에 설명을 띄운다.
    public void SetDescriptionBar(TextMeshProUGUI bar, string idleText)
    {
        descriptionBar = bar;
        descriptionBarIdleText = idleText;
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

    private void OnDestroy()
    {
        if (buyButton != null)
            buyButton.onClick.RemoveListener(OnBuyButtonClicked);
    }

    private bool IsOwned()
    {
        return InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemName);
    }

    private string ConflictingOwnedItem()
    {
        return InventoryManager.Instance != null ? InventoryManager.Instance.GetConflictingOwnedItem(itemName) : null;
    }

    private void RefreshDisplay()
    {
        if (nameText != null)
            nameText.text = itemName;

        bool owned = IsOwned();
        string conflict = owned ? null : ConflictingOwnedItem();

        if (priceText != null)
            priceText.text = owned ? "보유중" : conflict != null ? $"{conflict} 보유 중" : $"{price} G";

        if (buyButton != null)
            buyButton.interactable = !owned && conflict == null;
    }

    public void OnBuyButtonClicked()
    {
        if (IsOwned())
        {
            Debug.Log($"[구매 실패] {itemName}은(는) 이미 보유하고 있어 중복 구매할 수 없습니다.");
            return;
        }

        string conflict = ConflictingOwnedItem();
        if (conflict != null)
        {
            Debug.Log($"[구매 실패] {itemName}은(는) {conflict}와(과) 함께 보유할 수 없습니다.");
            return;
        }

        if (GoldManager.Instance == null)
        {
            Debug.LogWarning("GoldManager instance not found in scene.");
            return;
        }

        if (GoldManager.Instance.TrySpend(price))
        {
            Debug.Log($"[구매 성공] {itemName} 구매 완료 (-{price} G, 남은 골드: {GoldManager.Instance.CurrentGold} G)");

            InventoryManager.Instance?.TryAddItem(itemName);

            RefreshDisplay();
        }
        else
        {
            Debug.Log($"[구매 실패] 골드가 부족합니다. (필요: {price} G, 보유: {GoldManager.Instance.CurrentGold} G)");
        }
    }
}

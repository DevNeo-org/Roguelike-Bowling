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

    // true면 InventoryManager(상점 아이템)가 아니라 SkillManager(장신구)를 보유/구매 대상으로 사용한다.
    [SerializeField] private bool isSkill;

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
    }

    public void Configure(string newName, string newDescription, int newPrice, Sprite newIcon, bool newIsSkill = false)
    {
        itemName = newName;
        description = newDescription;
        price = newPrice;
        isSkill = newIsSkill;

        if (iconImage != null)
        {
            iconImage.sprite = newIcon;
            iconImage.enabled = newIcon != null;
        }

        if (isSkill)
            ApplyCompactLayout();

        RefreshDisplay();
    }

    // 장신구 카드는 아이콘이 없고(iconImage 비활성) 상점 아이템 카드보다 세로로 작게 배치되는데,
    // 이름/가격/구매 버튼은 원래 프리팹의 150px 기준 위치 그대로라 카드 밖으로 벗어난다.
    // 아이콘이 있던 자리를 활용해 위로 당겨서 재배치한다.
    private void ApplyCompactLayout()
    {
        if (nameText != null)
        {
            var rt = nameText.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -10f);
        }

        if (priceText != null)
        {
            var rt = priceText.rectTransform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -46f);
        }

        if (buyButton != null)
        {
            var rt = (RectTransform)buyButton.transform;
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -48f);
        }
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

    // 상점 할인 아이템 소지 시 가격을 할인해준다("상점가를 할인해 줍니다").
    private int EffectivePrice()
    {
        bool discounted = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned("상점 할인");
        return discounted ? Mathf.RoundToInt(price * 0.85f) : price;
    }

    private bool IsOwned()
    {
        if (isSkill)
            return SkillManager.Instance != null && SkillManager.Instance.IsOwned(itemName);

        return InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(itemName);
    }

    private void RefreshDisplay()
    {
        if (nameText != null)
            nameText.text = itemName;

        bool owned = IsOwned();

        if (priceText != null)
            priceText.text = owned ? "보유중" : $"{EffectivePrice()} G";

        if (buyButton != null)
            buyButton.interactable = !owned;
    }

    public void OnBuyButtonClicked()
    {
        if (IsOwned())
        {
            Debug.Log($"[구매 실패] {itemName}은(는) 이미 보유하고 있어 중복 구매할 수 없습니다.");
            return;
        }

        if (GoldManager.Instance == null)
        {
            Debug.LogWarning("GoldManager instance not found in scene.");
            return;
        }

        int effectivePrice = EffectivePrice();
        if (GoldManager.Instance.TrySpend(effectivePrice))
        {
            Debug.Log($"[구매 성공] {itemName} 구매 완료 (-{effectivePrice} G, 남은 골드: {GoldManager.Instance.CurrentGold} G)");

            if (isSkill)
                SkillManager.Instance?.TryAddSkill(itemName);
            else
                InventoryManager.Instance?.TryAddItem(itemName);

            RefreshDisplay();
        }
        else
        {
            Debug.Log($"[구매 실패] 골드가 부족합니다. (필요: {effectivePrice} G, 보유: {GoldManager.Instance.CurrentGold} G)");
        }
    }
}

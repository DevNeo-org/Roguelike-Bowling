using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 정비 화면 우측 상점 패널: GameDataManager의 상점 목록 전체를 그리드 형태의 작은 카드로 한 번만 생성한다.
// 카드에는 아이콘/이름/가격만 표시되고, 설명은 카드에 마우스를 올렸을 때 상단 공용 표시줄에 나타난다.
// 하단에는 장신구(스킬) 구매용 슬롯 3칸이 추가로 있다 - 정비 화면에 들어올 때마다 무료로 자동 추첨되고,
// 골드를 내고 리롤 버튼을 누르면 다시 추첨된다.
public class ShopAreaController : MonoBehaviour
{
    [SerializeField] private GameObject shopItemCardPrefab;
    [SerializeField] private RectTransform cardContainer;
    [SerializeField] private TextMeshProUGUI descriptionBar;

    [Header("Grid Layout")]
    [SerializeField] private int columns = 3;
    [SerializeField] private float cellWidth = 280f;
    [SerializeField] private float cellHeight = 165f;
    [SerializeField] private float columnGap = 15f;
    [SerializeField] private float rowGap = 12f;
    [SerializeField] private float startY = -130f;

    [Header("Accessory (장신구) Slots")]
    [SerializeField] private RectTransform accessoryContainer;
    [SerializeField] private Button rerollButton;
    [SerializeField] private int accessorySlotCount = 3;
    [SerializeField] private int rerollCost = 100;
    [SerializeField] private float accessoryCardWidth = 260f;
    [SerializeField] private float accessoryCardHeight = 90f;
    [SerializeField] private float accessoryColumnGap = 15f;
    [SerializeField] private float accessoryStartY = -700f;

    private const string DescriptionBarIdleText = "항목에 마우스를 올리면 설명이 표시됩니다.";
    private const string IconResourceFolder = "ShopIcons/";

    private readonly List<GameObject> accessoryCards = new List<GameObject>();

    private void Awake()
    {
        if (descriptionBar != null)
            descriptionBar.text = DescriptionBarIdleText;

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnRerollClicked);

        BuildShopItemGrid();
    }

    private void OnDestroy()
    {
        if (rerollButton != null)
            rerollButton.onClick.RemoveListener(OnRerollClicked);
    }

    // 정비 화면이 열릴 때마다(=이 오브젝트가 활성화될 때마다) 장신구 후보를 무료로 한 번 새로 뽑는다.
    private void OnEnable()
    {
        RerollAccessories();
    }

    private void BuildShopItemGrid()
    {
        if (shopItemCardPrefab == null || cardContainer == null)
            return;

        var items = GameDataManager.LoadShopItems();
        float gridWidth = columns * cellWidth + (columns - 1) * columnGap;
        float startX = -gridWidth / 2f + cellWidth / 2f;

        for (int i = 0; i < items.Count; i++)
        {
            int col = i % columns;
            int row = i / columns;

            var cardGO = Instantiate(shopItemCardPrefab, cardContainer);
            cardGO.name = "ShopItemCard_" + (i + 1);

            var rt = (RectTransform)cardGO.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(startX + col * (cellWidth + columnGap), startY - row * (cellHeight + rowGap));
            rt.localScale = Vector3.one;

            var shopItem = cardGO.GetComponent<ShopItem>();
            if (shopItem == null)
                continue;

            Sprite icon = Resources.Load<Sprite>(IconResourceFolder + items[i].icon);
            shopItem.Configure(items[i].name, items[i].description, items[i].price, icon);
            shopItem.SetDescriptionBar(descriptionBar, DescriptionBarIdleText);
        }
    }

    private void OnRerollClicked()
    {
        if (GoldManager.Instance != null && GoldManager.Instance.TrySpend(rerollCost))
        {
            Debug.Log($"[상점] 장신구 리롤 (-{rerollCost} G, 남은 골드: {GoldManager.Instance.CurrentGold} G)");
            RerollAccessories();
        }
        else
        {
            int current = GoldManager.Instance != null ? GoldManager.Instance.CurrentGold : 0;
            Debug.Log($"[상점] 장신구 리롤 실패. 골드가 부족합니다. (필요: {rerollCost} G, 보유: {current} G)");
        }
    }

    private void RerollAccessories()
    {
        if (shopItemCardPrefab == null || accessoryContainer == null)
            return;

        foreach (var card in accessoryCards)
        {
            if (card != null)
                Destroy(card);
        }
        accessoryCards.Clear();

        var available = GameDataManager.LoadSkills();
        if (SkillManager.Instance != null)
            available = available.FindAll(s => !SkillManager.Instance.IsOwned(s.name));

        var picked = GameDataManager.PickRandomDistinct(available, accessorySlotCount);

        float gridWidth = picked.Count * accessoryCardWidth + Mathf.Max(0, picked.Count - 1) * accessoryColumnGap;
        float startX = -gridWidth / 2f + accessoryCardWidth / 2f;

        for (int i = 0; i < picked.Count; i++)
        {
            var cardGO = Instantiate(shopItemCardPrefab, accessoryContainer);
            cardGO.name = "AccessoryCard_" + (i + 1);

            var rt = (RectTransform)cardGO.transform;
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(accessoryCardWidth, accessoryCardHeight);
            rt.anchoredPosition = new Vector2(startX + i * (accessoryCardWidth + accessoryColumnGap), accessoryStartY);
            rt.localScale = Vector3.one;

            var shopItem = cardGO.GetComponent<ShopItem>();
            if (shopItem == null)
                continue;

            shopItem.Configure(picked[i].name, picked[i].description, picked[i].price, null, newIsSkill: true);
            shopItem.SetDescriptionBar(descriptionBar, DescriptionBarIdleText);

            accessoryCards.Add(cardGO);
        }
    }
}

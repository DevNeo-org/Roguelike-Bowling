using TMPro;
using UnityEngine;

// 정비 화면 우측 상점 패널: GameDataManager의 상점 목록 전체를 그리드 형태의 작은 카드로 한 번만 생성한다.
// 카드에는 아이콘/이름/가격만 표시되고, 설명은 카드에 마우스를 올렸을 때 상단 공용 표시줄에 나타난다.
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

    private const string DescriptionBarIdleText = "항목에 마우스를 올리면 설명이 표시됩니다.";
    private const string IconResourceFolder = "ShopIcons/";

    private void Awake()
    {
        if (descriptionBar != null)
            descriptionBar.text = DescriptionBarIdleText;

        BuildShopItemGrid();
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
}

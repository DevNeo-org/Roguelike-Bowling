using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 투구 전 우측에 세로로 배치된 5개의 액티브 아이템 사용 버튼.
// 보유하지 않은 아이템은 버튼이 비활성화(interactable=false)되고, 보유 중인 아이템은 눌러서
// "다음 투구에 켜짐" 상태(ItemActivationManager)를 토글한다. 켜진 버튼은 발사 즉시(BallLauncher)
// 자동으로 다시 꺼진다.
public class ActiveItemSelector : MonoBehaviour
{
    [System.Serializable]
    public class ActiveItemOption
    {
        public string itemId;
        public string label = "아이템";
        public Color color = Color.white;
    }

    [SerializeField]
    private ActiveItemOption[] options = new ActiveItemOption[]
    {
        new ActiveItemOption { itemId = ItemIds.ObstaclePassBall,    label = "통과",  color = new Color(0.30f, 0.55f, 0.95f) },
        new ActiveItemOption { itemId = ItemIds.ObstacleBreakerBall, label = "파괴",  color = new Color(0.90f, 0.35f, 0.25f) },
        new ActiveItemOption { itemId = ItemIds.OldTowel,            label = "수건",  color = new Color(0.65f, 0.50f, 0.35f) },
        new ActiveItemOption { itemId = ItemIds.WaxTowel,            label = "왁스",  color = new Color(0.85f, 0.80f, 0.40f) },
        new ActiveItemOption { itemId = ItemIds.StraightShoes,       label = "직선",  color = new Color(0.55f, 0.55f, 0.60f) },
    };

    [Header("UI (options와 같은 순서/개수로 배치)")]
    [SerializeField] private Button[] buttons;
    [SerializeField] private Image[] buttonBackgrounds;
    [SerializeField] private Image[] buttonIcons;
    [SerializeField] private TextMeshProUGUI[] buttonLabels;
    [SerializeField] private GameObject[] activeHighlights;
    [SerializeField] private CanvasGroup[] ownedGroups; // 미보유 시 흐리게(alpha) 처리용

    // 상점(ShopItems.json)과 아이콘을 통일하기 위해 GameDataManager의 아이콘 파일명을 그대로 사용한다.
    private const string IconResourceFolder = "ShopIcons/";

    private void Awake()
    {
        var shopEntries = GameDataManager.LoadShopItems();

        for (int i = 0; i < buttons.Length && i < options.Length; i++)
        {
            int idx = i;
            if (buttons[i] != null)
                buttons[i].onClick.AddListener(() => ItemActivationManager.Toggle(options[idx].itemId));

            if (buttonBackgrounds != null && i < buttonBackgrounds.Length && buttonBackgrounds[i] != null)
                buttonBackgrounds[i].color = options[i].color;

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
                buttonLabels[i].text = options[i].label;

            if (buttonIcons != null && i < buttonIcons.Length && buttonIcons[i] != null)
            {
                Sprite icon = FindIconSprite(shopEntries, options[i].itemId);
                buttonIcons[i].sprite = icon;
                buttonIcons[i].enabled = icon != null;
            }
        }
    }

    private static Sprite FindIconSprite(System.Collections.Generic.List<ShopEntry> entries, string itemId)
    {
        if (entries == null) return null;
        foreach (var entry in entries)
        {
            if (entry.name == itemId)
                return Resources.Load<Sprite>(IconResourceFolder + entry.icon);
        }
        return null;
    }

    private void OnEnable()
    {
        ItemActivationManager.Changed += RefreshAll;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded += HandleInventoryChanged;
            InventoryManager.Instance.OnItemRemoved += HandleInventoryChanged;
        }
        RefreshAll();
    }

    private void OnDisable()
    {
        ItemActivationManager.Changed -= RefreshAll;
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnItemAdded -= HandleInventoryChanged;
            InventoryManager.Instance.OnItemRemoved -= HandleInventoryChanged;
        }
    }

    private void HandleInventoryChanged(string changedItemId) => RefreshAll();

    private void RefreshAll()
    {
        for (int i = 0; i < options.Length; i++)
        {
            bool owned = InventoryManager.Instance != null && InventoryManager.Instance.IsOwned(options[i].itemId);
            bool active = ItemActivationManager.IsActive(options[i].itemId);

            if (buttons != null && i < buttons.Length && buttons[i] != null)
                buttons[i].interactable = owned;

            if (activeHighlights != null && i < activeHighlights.Length && activeHighlights[i] != null)
                activeHighlights[i].SetActive(active);

            if (ownedGroups != null && i < ownedGroups.Length && ownedGroups[i] != null)
                ownedGroups[i].alpha = owned ? 1f : 0.4f;
        }
    }
}

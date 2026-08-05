using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 좌측 하단 버튼으로 여닫는 패널: 현재 보유 중인 아이템(InventoryManager)을
// GameDataManager의 이름/설명 데이터와 대조해서 보여준다.
public class InventorySkillUIController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private RectTransform inventoryListContainer;
    [SerializeField] private GameObject rowPrefab;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleOpen);

        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleOpen);
    }

    public void ToggleOpen()
    {
        if (panel == null)
            return;

        bool willOpen = !panel.activeSelf;
        panel.SetActive(willOpen);

        if (willOpen)
            Refresh();
    }

    private void Refresh()
    {
        ClearChildren(inventoryListContainer);

        bool hasItem = false;
        foreach (var item in GameDataManager.LoadShopItems())
        {
            if (InventoryManager.Instance == null || !InventoryManager.Instance.IsOwned(item.name))
                continue;

            CreateRow(inventoryListContainer, item.name, item.description);
            hasItem = true;
        }

        if (!hasItem)
            CreateRow(inventoryListContainer, "", "보유 중인 아이템이 없습니다.");
    }

    private void CreateRow(RectTransform container, string title, string description)
    {
        if (container == null || rowPrefab == null)
            return;

        GameObject row = Instantiate(rowPrefab, container);
        row.SetActive(true);

        var nameText = row.transform.Find("NameText")?.GetComponent<TextMeshProUGUI>();
        var descText = row.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();

        if (nameText != null)
        {
            nameText.text = title;
            nameText.gameObject.SetActive(!string.IsNullOrEmpty(title));
        }

        if (descText != null)
            descText.text = description;
    }

    private void ClearChildren(RectTransform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }
}

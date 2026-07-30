using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private Button toggleButton;

    private void Awake()
    {
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleShop);

        if (shopPanel != null)
            shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (toggleButton != null)
            toggleButton.onClick.RemoveListener(ToggleShop);
    }

    public void ToggleShop()
    {
        if (shopPanel == null)
            return;

        shopPanel.SetActive(!shopPanel.activeSelf);
    }
}

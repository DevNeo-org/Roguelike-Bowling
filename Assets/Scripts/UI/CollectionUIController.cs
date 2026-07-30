using UnityEngine;
using UnityEngine.UI;

public class CollectionUIController : MonoBehaviour
{
    [SerializeField] private GameObject returnScreen;
    [SerializeField] private Button backButton;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);
    }

    public void OnBackClicked()
    {
        Debug.Log("[도감] 뒤로가기");

        if (returnScreen != null)
            returnScreen.SetActive(true);

        gameObject.SetActive(false);
    }
}

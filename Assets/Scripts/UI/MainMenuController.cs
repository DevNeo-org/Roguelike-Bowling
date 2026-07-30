using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button loadGameButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button collectionButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject playScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private GameObject collectionScreen;
    [SerializeField] private StageManager stageManager;

    private void Awake()
    {
        if (newGameButton != null)
            newGameButton.onClick.AddListener(OnNewGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.AddListener(OnLoadGameClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (collectionButton != null)
            collectionButton.onClick.AddListener(OnCollectionClicked);

        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitClicked);

        RefreshLoadButtonState();
    }

    private void RefreshLoadButtonState()
    {
        if (loadGameButton != null)
            loadGameButton.interactable = SaveManager.HasSaveData();
    }

    private void OnDestroy()
    {
        if (newGameButton != null)
            newGameButton.onClick.RemoveListener(OnNewGameClicked);

        if (loadGameButton != null)
            loadGameButton.onClick.RemoveListener(OnLoadGameClicked);

        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (collectionButton != null)
            collectionButton.onClick.RemoveListener(OnCollectionClicked);

        if (quitButton != null)
            quitButton.onClick.RemoveListener(OnQuitClicked);
    }

public void OnNewGameClicked()
    {
        Debug.Log("[메인메뉴] 새 게임 시작");

        if (GoldManager.Instance != null)
            GoldManager.Instance.StartNewGame();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.StartNewGame();

        if (stageManager != null)
            stageManager.StartFromStageOne();
        if (BowlingScoreManager.Instance != null)
            BowlingScoreManager.Instance.StartNewGame();

        if (LaneStageManager.Instance != null)
            LaneStageManager.Instance.StartNewGame();

        if (SkillManager.Instance != null)
            SkillManager.Instance.StartNewGame();

        if (playScreen != null)
            playScreen.SetActive(true);

        gameObject.SetActive(false);
    }

    public void OnLoadGameClicked()
    {
        if (!SaveManager.HasSaveData())
        {
            Debug.Log("[메인메뉴] 불러오기 실패: 저장된 데이터가 없습니다.");
            return;
        }

        Debug.Log("[메인메뉴] 저장된 게임 불러오기");

        if (GoldManager.Instance != null)
            GoldManager.Instance.LoadFromSave();

        if (InventoryManager.Instance != null)
            InventoryManager.Instance.LoadFromSave();

        if (stageManager != null)
            stageManager.StartFromStageOne();

        // 스킬은 아직 저장/불러오기 대상이 아니므로 항상 빈 상태로 시작한다.
        if (SkillManager.Instance != null)
            SkillManager.Instance.StartNewGame();

        if (playScreen != null)
            playScreen.SetActive(true);

        gameObject.SetActive(false);
    }

    public void OnSettingsClicked()
    {
        Debug.Log("[메인메뉴] 설정 열기");

        if (settingsScreen != null)
        {
            var settingsController = settingsScreen.GetComponent<SettingsController>();
            if (settingsController != null)
                settingsController.SetReturnScreen(gameObject);

            settingsScreen.SetActive(true);
        }

        gameObject.SetActive(false);
    }

    public void OnCollectionClicked()
    {
        Debug.Log("[메인메뉴] 도감 열기");

        if (collectionScreen != null)
            collectionScreen.SetActive(true);

        gameObject.SetActive(false);
    }

    public void OnQuitClicked()
    {
        Debug.Log("[메인메뉴] 게임 종료");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}

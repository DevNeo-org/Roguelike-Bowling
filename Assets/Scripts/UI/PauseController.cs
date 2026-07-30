using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PauseController : MonoBehaviour
{
    [SerializeField] private GameObject pauseUI;
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject settingsScreen;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

    private bool isPaused;

    private void Awake()
    {
        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (resumeButton != null)
            resumeButton.onClick.AddListener(OnResumeClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
    }

    private void OnDestroy()
    {
        if (settingsButton != null)
            settingsButton.onClick.RemoveListener(OnSettingsClicked);

        if (resumeButton != null)
            resumeButton.onClick.RemoveListener(OnResumeClicked);

        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (isPaused)
                Resume();
            else
                Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;

        if (pauseUI != null)
            pauseUI.SetActive(true);

        Time.timeScale = 0f;
        Debug.Log("[일시정지] 일시정지");
    }

    private void Resume()
    {
        isPaused = false;

        if (pauseUI != null)
            pauseUI.SetActive(false);

        Time.timeScale = 1f;
        Debug.Log("[일시정지] 계속하기");
    }

    public void OnResumeClicked()
    {
        Resume();
    }

    public void OnSettingsClicked()
    {
        Debug.Log("[일시정지] 설정 열기");

        if (settingsScreen != null)
        {
            var settingsController = settingsScreen.GetComponent<SettingsController>();
            if (settingsController != null)
                settingsController.SetReturnScreen(pauseUI);

            settingsScreen.transform.SetAsLastSibling();
            settingsScreen.SetActive(true);
        }

        if (pauseUI != null)
            pauseUI.SetActive(false);
    }

    public void OnMainMenuClicked()
    {
        Debug.Log("[일시정지] 메인 메뉴로 이동");

        isPaused = false;
        Time.timeScale = 1f;

        if (pauseUI != null)
            pauseUI.SetActive(false);

        gameObject.SetActive(false);

        if (mainMenuScreen != null)
            mainMenuScreen.SetActive(true);
    }
}

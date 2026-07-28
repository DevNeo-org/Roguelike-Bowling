using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsController : MonoBehaviour
{
    [SerializeField] private GameObject returnScreen;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Dropdown screenModeDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private Slider volumeSlider;

    private Resolution[] availableResolutions;

    private void Awake()
    {
        if (backButton != null)
            backButton.onClick.AddListener(OnBackClicked);

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.AddListener(OnScreenModeChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnEnable()
    {
        SetupScreenModeDropdown();
        SetupResolutionDropdown();
        SetupVolumeSlider();
    }

    private void OnDestroy()
    {
        if (backButton != null)
            backButton.onClick.RemoveListener(OnBackClicked);

        if (screenModeDropdown != null)
            screenModeDropdown.onValueChanged.RemoveListener(OnScreenModeChanged);

        if (resolutionDropdown != null)
            resolutionDropdown.onValueChanged.RemoveListener(OnResolutionChanged);

        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
    }

    private void SetupScreenModeDropdown()
    {
        if (screenModeDropdown == null)
            return;

        screenModeDropdown.ClearOptions();
        screenModeDropdown.AddOptions(new List<string> { "전체 화면", "창 모드", "테두리 없는 창 모드" });

        int current;
        switch (Screen.fullScreenMode)
        {
            case FullScreenMode.Windowed:
                current = 1;
                break;
            case FullScreenMode.FullScreenWindow:
                current = 2;
                break;
            default:
                current = 0;
                break;
        }

        screenModeDropdown.SetValueWithoutNotify(current);
    }

    private void SetupResolutionDropdown()
    {
        if (resolutionDropdown == null)
            return;

        availableResolutions = Screen.resolutions;
        resolutionDropdown.ClearOptions();

        var options = new List<string>();
        int currentIndex = 0;

        for (int i = 0; i < availableResolutions.Length; i++)
        {
            var r = availableResolutions[i];
            options.Add($"{r.width} x {r.height}");

            if (r.width == Screen.currentResolution.width && r.height == Screen.currentResolution.height)
                currentIndex = i;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.SetValueWithoutNotify(currentIndex);
    }

    private void SetupVolumeSlider()
    {
        if (volumeSlider == null)
            return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.SetValueWithoutNotify(AudioListener.volume);
    }

    public void OnScreenModeChanged(int index)
    {
        FullScreenMode mode;
        switch (index)
        {
            case 1:
                mode = FullScreenMode.Windowed;
                break;
            case 2:
                mode = FullScreenMode.FullScreenWindow;
                break;
            default:
                mode = FullScreenMode.ExclusiveFullScreen;
                break;
        }

        Screen.fullScreenMode = mode;
        Debug.Log($"[설정] 화면 모드 변경: {mode}");
    }

    public void OnResolutionChanged(int index)
    {
        if (availableResolutions == null || index < 0 || index >= availableResolutions.Length)
            return;

        var r = availableResolutions[index];
        Screen.SetResolution(r.width, r.height, Screen.fullScreenMode);
        Debug.Log($"[설정] 해상도 변경: {r.width} x {r.height}");
    }

    public void OnVolumeChanged(float value)
    {
        AudioListener.volume = value;
        Debug.Log($"[설정] 사운드 볼륨 변경: {value:F2}");
    }

    public void OnBackClicked()
    {
        Debug.Log("[설정] 뒤로가기");

        if (returnScreen != null)
            returnScreen.SetActive(true);

        gameObject.SetActive(false);
    }

    public void SetReturnScreen(GameObject screen)
    {
        returnScreen = screen;
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 투구 전 우측 버튼으로 4가지 무게(6/9/12/15 파운드)의 공을 선택한다.
// BallSpawner가 새 공을 스폰할 때 이 선택을 읽어 질량과 색을 적용한다.
public class BallWeightSelector : MonoBehaviour
{
    public static BallWeightSelector Instance { get; private set; }

    [System.Serializable]
    public class WeightOption
    {
        public string label = "9";
        public float pounds = 9f;
        public Color ballColor = Color.blue;
    }

    [Tooltip("기존 공 프리팹의 기본 질량(9파운드 기준)에 맞춰 무게를 환산할 때 쓰는 기준 파운드")]
    [SerializeField] private float baselinePounds = 9f;
    [SerializeField] private float baselineMass = 7f;

    [SerializeField]
    private WeightOption[] weightOptions = new WeightOption[]
    {
        new WeightOption { label = "6",  pounds = 6f,  ballColor = new Color(0.55f, 0.85f, 0.35f) },
        new WeightOption { label = "9",  pounds = 9f,  ballColor = new Color(0.25f, 0.45f, 0.95f) },
        new WeightOption { label = "12", pounds = 12f, ballColor = new Color(0.55f, 0.20f, 0.75f) },
        new WeightOption { label = "15", pounds = 15f, ballColor = new Color(0.12f, 0.12f, 0.14f) },
    };

    [SerializeField] private int defaultIndex = 1;

    [Header("UI (선택)")]
    [Tooltip("weightOptions와 같은 순서/개수로 배치")]
    [SerializeField] private Button[] weightButtons;
    [SerializeField] private Image[] buttonBackgrounds;
    [SerializeField] private TextMeshProUGUI[] buttonLabels;
    [SerializeField] private GameObject[] selectedHighlights;

    public int SelectedIndex { get; private set; }
    public event System.Action<int> OnWeightChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        SelectedIndex = Mathf.Clamp(defaultIndex, 0, weightOptions.Length - 1);

        for (int i = 0; i < weightButtons.Length && i < weightOptions.Length; i++)
        {
            int idx = i;
            if (weightButtons[i] != null)
                weightButtons[i].onClick.AddListener(() => SelectWeight(idx));

            if (buttonBackgrounds != null && i < buttonBackgrounds.Length && buttonBackgrounds[i] != null)
                buttonBackgrounds[i].color = weightOptions[i].ballColor;

            if (buttonLabels != null && i < buttonLabels.Length && buttonLabels[i] != null)
                buttonLabels[i].text = weightOptions[i].label;
        }

        RefreshSelectionVisuals();
    }

    public int OptionCount => weightOptions.Length;

    public WeightOption GetOption(int index) => weightOptions[Mathf.Clamp(index, 0, weightOptions.Length - 1)];

    public void SelectWeight(int index)
    {
        if (index < 0 || index >= weightOptions.Length) return;

        SelectedIndex = index;
        RefreshSelectionVisuals();
        OnWeightChanged?.Invoke(SelectedIndex);

        Debug.Log($"[BallWeightSelector] {weightOptions[index].label}파운드 공 선택");
    }

    private void RefreshSelectionVisuals()
    {
        if (selectedHighlights == null) return;

        for (int i = 0; i < selectedHighlights.Length; i++)
        {
            if (selectedHighlights[i] != null)
                selectedHighlights[i].SetActive(i == SelectedIndex);
        }
    }

    // 기존 공 프리팹의 질량(baselineMass)이 baselinePounds에 대응한다고 보고 비례 환산한다.
    public float GetMassForSelected()
    {
        var opt = weightOptions[SelectedIndex];
        return baselineMass * (opt.pounds / baselinePounds);
    }

    public Color GetColorForSelected() => weightOptions[SelectedIndex].ballColor;
}

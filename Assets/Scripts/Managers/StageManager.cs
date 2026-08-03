using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    public static StageManager Instance { get; private set; }

    [Header("Balance")]
    [SerializeField] private int baseTargetScore = 40;
    [SerializeField] private int targetScoreIncreasePerStage = 10;
    private const int FramesPerStage = 10;

    [Header("Stage Play UI")]
    [SerializeField] private GameObject stagePlayUI;
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private TextMeshProUGUI resultText;
    [SerializeField] private ThrowResultPopupController resultPopup;
    [SerializeField] private TextMeshProUGUI[] throwSlot1;
    [SerializeField] private TextMeshProUGUI[] throwSlot2;
    [SerializeField] private TextMeshProUGUI throwSlot3Tenth;

    [Header("Maintenance UI")]
    [SerializeField] private GameObject maintenanceUI;
    [SerializeField] private TextMeshProUGUI maintenanceSummaryText;
    [SerializeField] private Button nextStageButton;

    [Header("Failed UI")]
    [SerializeField] private GameObject failedUI;
    [SerializeField] private TextMeshProUGUI failedSummaryText;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button failedMainMenuButton;

    [Header("Cross References")]
    [SerializeField] private GameObject shopToggleButton;
    [SerializeField] private GameObject mainMenuScreen;
    [SerializeField] private GameObject playScreen;

    private int currentStage = 1;
    private int currentFrame = 1;
    private int stageScore;
    private int targetScore;
    private bool isPlaying;

    public int CurrentFrameNumber => currentFrame;
    public bool IsPlaying => isPlaying;

    // 프레임별로 던진 공의 핀 개수를 순서대로 기록 (10프레임만 보너스 투구로 최대 3개까지 가능).
    private List<int>[] frameThrows;
    private int pinsStanding;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (nextStageButton != null)
            nextStageButton.onClick.AddListener(OnNextStageClicked);

        if (retryButton != null)
            retryButton.onClick.AddListener(OnRetryClicked);

        if (failedMainMenuButton != null)
            failedMainMenuButton.onClick.AddListener(OnFailedMainMenuClicked);
    }

    // 디버그 치트키: K를 누르면 현재 스테이지를 즉시 클리어 상태로 만들고 정비 화면으로 넘어간다.
    private void Update()
    {
        if (!isPlaying)
            return;

        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            Debug.Log($"[치트] K키 입력: {currentStage}스테이지 강제 클리어");
            stageScore = Mathf.Max(stageScore, targetScore);
            RefreshInfoText();
            isPlaying = false;
            StartCoroutine(ShowClearPopupThenEnterMaintenance());
        }
    }

    private void OnDestroy()
    {
        if (nextStageButton != null)
            nextStageButton.onClick.RemoveListener(OnNextStageClicked);

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (failedMainMenuButton != null)
            failedMainMenuButton.onClick.RemoveListener(OnFailedMainMenuClicked);
    }

    public void StartFromStageOne()
    {
        StartStage(1);
    }

    // "불러오기"에서 사용 - 마지막으로 저장된 스테이지의 1프레임부터 다시 시작한다
    // (프레임 중간 물리 상태까지는 저장하지 않는다).
    public void StartFromSavedStage()
    {
        StartStage(SaveManager.LoadStage(1));
    }

    private void StartStage(int stage)
    {
        currentStage = stage;
        SaveManager.SaveStage(stage);
        currentFrame = 1;
        stageScore = 0;
        targetScore = baseTargetScore + (stage - 1) * targetScoreIncreasePerStage;
        isPlaying = true;

        pinsStanding = 10;
        frameThrows = new List<int>[FramesPerStage];
        for (int i = 0; i < FramesPerStage; i++)
            frameThrows[i] = new List<int>();

        if (throwSlot1 != null)
        {
            for (int i = 0; i < throwSlot1.Length; i++)
            {
                if (throwSlot1[i] != null)
                    throwSlot1[i].text = "";
            }
        }

        if (throwSlot2 != null)
        {
            for (int i = 0; i < throwSlot2.Length; i++)
            {
                if (throwSlot2[i] != null)
                    throwSlot2[i].text = "";
            }
        }

        if (throwSlot3Tenth != null)
            throwSlot3Tenth.text = "";

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.ResetPins();

        if (resultText != null)
            resultText.text = "Space 키로 공을 던져 투구하세요.";

        if (stagePlayUI != null)
            stagePlayUI.SetActive(true);

        if (maintenanceUI != null)
            maintenanceUI.SetActive(false);

        if (failedUI != null)
            failedUI.SetActive(false);

        if (shopToggleButton != null)
            shopToggleButton.SetActive(false);

        RefreshInfoText();

        Debug.Log($"[스테이지] {currentStage}스테이지 시작 (목표 점수: {targetScore})");
    }

    // 실제 물리 투구(Map 씬 방식) 한 번의 결과를 기록한다. pinsKnocked는 이번 투구에서
    // 실제로 새로 쓰러진 핀 개수(PinDeckManager가 계산)이며, PinDeckManager가 공이 멈출 때마다 호출한다.
    // 10프레임을 제외하면 스트라이크가 아닐 때만 두 번째 투구가 진행되고,
    // 10프레임은 전통적인 볼링 규칙대로 스트라이크/스페어 시 보너스 투구(최대 3구)가 추가된다.
    public void RecordThrow(int pinsKnocked)
    {
        if (!isPlaying || currentFrame > FramesPerStage)
            return;

        int frameIdx = currentFrame - 1;
        bool isTenth = currentFrame == FramesPerStage;
        int pinsBeforeThisThrow = pinsStanding;
        int pins = Mathf.Clamp(pinsKnocked, 0, pinsBeforeThisThrow);

        // 스트라이크는 "이 투구가 프레임/랙의 첫 투구"일 때만 성립한다.
        // (예: 1구가 거터라 핀이 그대로 10개 남아있는 상태에서 2구로 그 10개를 마저 넘어뜨리는 것은
        //  스트라이크가 아니라 스페어다 - 10프레임의 2/3구는 직전 투구가 스트라이크/스페어였을 때만 새 랙이다.)
        int throwsSoFarInFrame = frameThrows[frameIdx].Count;
        bool isFreshRack;
        if (throwsSoFarInFrame == 0)
            isFreshRack = true;
        else if (isTenth && throwsSoFarInFrame == 1)
            isFreshRack = frameThrows[frameIdx][0] == 10;
        else if (isTenth && throwsSoFarInFrame == 2)
            isFreshRack = true; // 3구가 존재한다는 것 자체가 이미 스트라이크/스페어를 의미
        else
            isFreshRack = false;

        frameThrows[frameIdx].Add(pins);

        string symbol = pins == 10 && isFreshRack ? "X"
            : pins == 0 ? "-"
            : (pins == pinsBeforeThisThrow ? "/" : pins.ToString());

        int throwSlotIndex = frameThrows[frameIdx].Count;
        TextMeshProUGUI targetSlot = null;
        if (throwSlotIndex == 1 && throwSlot1 != null && frameIdx < throwSlot1.Length)
            targetSlot = throwSlot1[frameIdx];
        else if (throwSlotIndex == 2 && throwSlot2 != null && frameIdx < throwSlot2.Length)
            targetSlot = throwSlot2[frameIdx];
        else if (throwSlotIndex == 3)
            targetSlot = throwSlot3Tenth;

        if (targetSlot != null)
            targetSlot.text = symbol;

        bool frameComplete;
        if (!isTenth)
        {
            pinsStanding -= pins;
            frameComplete = pins == 10 || frameThrows[frameIdx].Count == 2;
        }
        else
        {
            frameComplete = HandleTenthFrameThrow(frameIdx, pins);
        }

        string throwLabel = pins == 10 && isFreshRack ? "스트라이크"
            : pins == 0 ? "거터"
            : (pins == pinsBeforeThisThrow ? "스페어" : $"{pins}점");

        if (resultText != null)
            resultText.text = $"{currentFrame}프레임 {throwSlotIndex}투: {throwLabel}";

        if (resultPopup != null)
            resultPopup.Show(throwLabel);

        Debug.Log($"[스테이지] {currentFrame}프레임 {throwSlotIndex}투 결과: {throwLabel}");

        if (frameComplete)
            FinishFrame();
        else
            RefreshInfoText();
    }

    // 10프레임 전용 처리: 첫 투구가 스트라이크거나 두 투구 합이 스페어면 보너스 투구가 주어진다.
    private bool HandleTenthFrameThrow(int frameIdx, int pins)
    {
        var throwsInFrame = frameThrows[frameIdx];

        if (throwsInFrame.Count == 1)
        {
            pinsStanding = pins == 10 ? 10 : 10 - pins;
            return false;
        }

        if (throwsInFrame.Count == 2)
        {
            int first = throwsInFrame[0];

            if (first == 10)
            {
                pinsStanding = pins == 10 ? 10 : 10 - pins;
                return false; // 첫 투구 스트라이크는 항상 3구째 보너스를 받는다.
            }

            if (first + pins == 10)
            {
                pinsStanding = 10;
                return false; // 스페어는 3구째 보너스를 받는다.
            }

            return true; // 오픈 프레임은 두 투구로 종료.
        }

        return true; // 세 번째 투구는 항상 프레임 종료.
    }

    private void FinishFrame()
    {
        stageScore = ComputeConfirmedScore();
        RefreshInfoText();

        Debug.Log($"[스테이지] {currentFrame}프레임 종료 - 확정 점수 {stageScore}점 (목표 {targetScore}점)");

        // 스트라이크/스페어 보너스가 아직 다음 프레임에서 정산 안 됐어도, 그 보너스가
        // 최악의 경우(전부 거터)로 나오더라도 이미 목표를 넘긴다면 나머지 보너스 투구를
        // 기다릴 필요 없이 바로 클리어 처리한다.
        int guaranteedScore = ComputeMinimumGuaranteedScore();

        if (stageScore >= targetScore || guaranteedScore >= targetScore)
        {
            stageScore = Mathf.Max(stageScore, guaranteedScore);
            RefreshInfoText();
            isPlaying = false;
            StartCoroutine(ShowClearPopupThenEnterMaintenance());
            return;
        }

        if (currentFrame >= FramesPerStage)
        {
            isPlaying = false;
            EnterFailed();
            return;
        }

        currentFrame++;
        pinsStanding = 10;

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.ResetPins();

        RefreshInfoText();
    }

    // "스테이지 통과!" 팝업을 잠깐 띄운 뒤 정비 화면으로 넘어간다.
    private IEnumerator ShowClearPopupThenEnterMaintenance()
    {
        float delay = 1f;

        if (resultPopup != null)
        {
            resultPopup.Show("스테이지 통과!");
            delay = resultPopup.DisplayDuration;
        }

        yield return new WaitForSeconds(delay);

        EnterMaintenance();
    }

    // ComputeConfirmedScore와 동일하되, 스트라이크/스페어의 보너스 투구가 아직 안 나왔으면
    // 포기(break)하는 대신 그 보너스를 최악의 경우인 0점으로 가정하고 계속 더한다.
    // "지금 상태로 최악의 결과가 나와도 확보되는 점수"를 구하기 위한 것 - 이 값만으로도
    // 목표를 넘기면 나머지 보너스 투구 결과를 기다리지 않고 바로 스테이지를 클리어시킨다.
    private int ComputeMinimumGuaranteedScore()
    {
        int total = 0;

        for (int f = 0; f < FramesPerStage; f++)
        {
            var t = frameThrows[f];
            if (t.Count == 0)
                break;

            if (f < FramesPerStage - 1)
            {
                if (t[0] == 10)
                {
                    List<int> bonus = GetThrowsFrom(f + 1, 2);
                    int b0 = bonus.Count > 0 ? bonus[0] : 0;
                    int b1 = bonus.Count > 1 ? bonus[1] : 0;
                    total += 10 + b0 + b1;
                }
                else if (t.Count >= 2 && t[0] + t[1] == 10)
                {
                    List<int> bonus = GetThrowsFrom(f + 1, 1);
                    int b0 = bonus.Count > 0 ? bonus[0] : 0;
                    total += 10 + b0;
                }
                else if (t.Count >= 2)
                {
                    total += t[0] + t[1];
                }
                else
                {
                    // 아직 프레임이 진행 중(첫 투구만 있음, 스트라이크 아님) - 이 투구는 앞
                    // 프레임의 보너스로 이미 반영됐을 수 있으므로 별도로 더하지 않고 멈춘다.
                    break;
                }
            }
            else
            {
                int sum = 0;
                foreach (var p in t)
                    sum += p;
                total += sum;
            }
        }

        return total;
    }

    // 전통적인 볼링 점수 계산: 스트라이크는 다음 두 투구, 스페어는 다음 한 투구를 보너스로 더한다.
    // 보너스에 필요한 미래 투구가 아직 없으면 그 프레임(과 그 이후)은 집계하지 않고 멈춘다.
    private int ComputeConfirmedScore()
    {
        int total = 0;

        for (int f = 0; f < FramesPerStage; f++)
        {
            var t = frameThrows[f];
            if (t.Count == 0)
                break;

            if (f < FramesPerStage - 1)
            {
                if (t[0] == 10)
                {
                    List<int> bonus = GetThrowsFrom(f + 1, 2);
                    if (bonus.Count < 2)
                        break;

                    total += 10 + bonus[0] + bonus[1];
                }
                else if (t.Count >= 2 && t[0] + t[1] == 10)
                {
                    List<int> bonus = GetThrowsFrom(f + 1, 1);
                    if (bonus.Count < 1)
                        break;

                    total += 10 + bonus[0];
                }
                else if (t.Count >= 2)
                {
                    total += t[0] + t[1];
                }
                else
                {
                    break; // 아직 프레임이 진행 중 (첫 투구만 있음, 스트라이크 아님)
                }
            }
            else
            {
                bool finished = t.Count == 3 || (t.Count == 2 && t[0] != 10 && t[0] + t[1] != 10);
                if (!finished)
                    break;

                int sum = 0;
                foreach (var p in t)
                    sum += p;

                total += sum;
            }
        }

        return total;
    }

    private List<int> GetThrowsFrom(int startFrame, int count)
    {
        var result = new List<int>();

        for (int f = startFrame; f < FramesPerStage && result.Count < count; f++)
        {
            foreach (var p in frameThrows[f])
            {
                result.Add(p);
                if (result.Count == count)
                    break;
            }
        }

        return result;
    }

    private void RefreshInfoText()
    {
        if (infoText == null)
            return;

        int displayFrame = Mathf.Min(currentFrame, FramesPerStage);
        infoText.text = $"스테이지 {currentStage}\n프레임 {displayFrame} / {FramesPerStage}\n점수 {stageScore} / 목표 {targetScore}";
    }

    private void EnterMaintenance()
    {
        Debug.Log($"[스테이지] {currentStage}스테이지 클리어! (점수 {stageScore} / 목표 {targetScore})");

        if (stagePlayUI != null)
            stagePlayUI.SetActive(false);

        // 상점 UI가 이제 정비 화면 우측 패널에 항상 표시되므로 별도 토글 버튼은 더 이상 사용하지 않는다.

        if (maintenanceSummaryText != null)
            maintenanceSummaryText.text = $"스테이지 {currentStage} 클리어!\n점수 {stageScore} / 목표 {targetScore}";

        if (maintenanceUI != null)
            maintenanceUI.SetActive(true);
    }

    private void EnterFailed()
    {
        Debug.Log($"[스테이지] {currentStage}스테이지 실패 (점수 {stageScore} / 목표 {targetScore})");

        if (stagePlayUI != null)
            stagePlayUI.SetActive(false);

        if (failedSummaryText != null)
            failedSummaryText.text = $"스테이지 {currentStage} 실패\n점수 {stageScore} / 목표 {targetScore}";

        if (failedUI != null)
            failedUI.SetActive(true);
    }

    public void OnNextStageClicked()
    {
        Debug.Log("[정비 타임] 다음 스테이지로 진행");

        if (maintenanceUI != null)
            maintenanceUI.SetActive(false);

        StartStage(currentStage + 1);
    }

    public void OnRetryClicked()
    {
        Debug.Log("[스테이지 실패] 같은 스테이지 다시 도전");

        if (failedUI != null)
            failedUI.SetActive(false);

        StartStage(currentStage);
    }

    public void OnFailedMainMenuClicked()
    {
        Debug.Log("[스테이지 실패] 메인 메뉴로 이동");

        if (failedUI != null)
            failedUI.SetActive(false);

        if (playScreen != null)
            playScreen.SetActive(false);

        if (mainMenuScreen != null)
            mainMenuScreen.SetActive(true);
    }
}

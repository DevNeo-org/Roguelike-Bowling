using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// 모든 튜토리얼 단계를 하나의 스크립트로 관리.
//
// [설정 방법]
//  1. References 필드에 필요한 컴포넌트 연결
//  2. Steps 배열에 TutorialStepData 에셋과 씬 오브젝트(waitVisual)를 등록
//  3. Next 버튼 onClick → TutorialController.Next() 하나만 연결
public class TutorialController : MonoBehaviour
{
    // ── 단계 엔트리 (ScriptableObject 데이터 + 씬 참조) ────────────────────────

    [Serializable]
    public class TutorialStepEntry
    {
        [Tooltip("Project 창에서 생성한 TutorialStepData 에셋")]
        public TutorialStepData data;

        [Tooltip("조건 대기 중 표시할 씬 오브젝트 (화살표 이미지 등). 없으면 비워두세요.")]
        public GameObject waitVisual;
    }

    // ── 인스펙터 필드 ──────────────────────────────────────────────────────────

    [Header("References")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private ThrowInputHandler throwInputHandler;
    [SerializeField] private BallLauncher ballLauncher;

    [Header("Positioning Detection")]
    [Tooltip("시작 위치 기준 왼쪽·오른쪽 각각 이 픽셀 이상 이동해야 완료.")]
    [SerializeField] private float requiredMovementPx = 80f;

    [Header("Steps")]
    [SerializeField] private TutorialStepEntry[] steps;

    [Space]
    [SerializeField] private UnityEvent onTutorialComplete;

    // ── 내부 상태 ──────────────────────────────────────────────────────────────

    private enum StepPhase { Lines, Waiting, CompletionLines }

    private int _stepIndex;
    private int _lineIndex;
    private StepPhase _stepPhase;

    private float _minMouseX;
    private float _maxMouseX;

    // OnSpin 추적
    private bool _spinThrowInProgress; // 스핀 없이 던진 후 공 리셋 대기 중

    // ── 생명주기 ──────────────────────────────────────────────────────────────

    private void Start()
    {
        if (steps == null || steps.Length == 0) return;
        StartStep(0);
    }

    // ── 외부 호출 ──────────────────────────────────────────────────────────────

    /// <summary>Next 버튼 onClick에 연결하세요.</summary>
    public void Next()
    {
        if (_stepPhase == StepPhase.Lines)
            AdvanceLines();
        else if (_stepPhase == StepPhase.CompletionLines)
            AdvanceCompletionLines();
        // Waiting 중에는 무시
    }

    // ── 단계 진행 ──────────────────────────────────────────────────────────────

    private void StartStep(int index)
    {
        _stepIndex = index;
        _lineIndex = 0;
        _stepPhase = StepPhase.Lines;

        var entry = steps[index];
        if (entry.waitVisual != null) entry.waitVisual.SetActive(false);

        SetThrowEnabled(false);

        if (entry.data == null || entry.data.lines == null || entry.data.lines.Length == 0)
        {
            Debug.LogError($"[TutorialController] Steps[{index}].data가 비어 있습니다.");
            return;
        }

        ShowLine(entry.data.lines[0]);
    }

    private void AdvanceLines()
    {
        var entry = steps[_stepIndex];
        _lineIndex++;

        if (_lineIndex >= entry.data.lines.Length)
        {
            if (entry.data.condition == TutorialCompletionCondition.OnNextClick)
                FinishWaiting(entry);
            else
                BeginWaiting(entry);
            return;
        }

        ShowLine(entry.data.lines[_lineIndex]);
    }

    private void BeginWaiting(TutorialStepEntry entry)
    {
        _stepPhase = StepPhase.Waiting;

        if (entry.waitVisual != null) entry.waitVisual.SetActive(true);
        if (dialogueText != null) dialogueText.text = string.Empty;

        switch (entry.data.condition)
        {
            case TutorialCompletionCondition.OnPositioning:
                _minMouseX = float.MaxValue;
                _maxMouseX = float.MinValue;
                if (ballLauncher != null) ballLauncher.enabled = true;
                if (throwInputHandler != null) throwInputHandler.enabled = true;
                break;

            case TutorialCompletionCondition.OnThrow:
                SetThrowEnabled(true);
                break;

            case TutorialCompletionCondition.OnSpin:
                _spinThrowInProgress = false;
                SetThrowEnabled(true);
                break;
        }
    }

    private void Update()
    {
        if (_stepPhase != StepPhase.Waiting || steps == null) return;

        var entry = steps[_stepIndex];

        switch (entry.data.condition)
        {
            case TutorialCompletionCondition.OnPositioning:
                UpdatePositioningDetection(entry);
                break;

            case TutorialCompletionCondition.OnThrow:
                if (throwInputHandler != null &&
                    throwInputHandler.State == ThrowInputHandler.ThrowState.Done)
                    FinishWaiting(entry);
                break;

            case TutorialCompletionCondition.OnSpin:
                UpdateSpinDetection(entry);
                break;
        }
    }

    private void UpdateSpinDetection(TutorialStepEntry entry)
    {
        if (throwInputHandler == null) return;

        var state = throwInputHandler.State;

        if (state == ThrowInputHandler.ThrowState.Done && !_spinThrowInProgress)
        {
            _spinThrowInProgress = true;
            if (throwInputHandler.IsCurvedDrag)
                FinishWaiting(entry);
            // 스핀 없음: BallSpawner가 공을 리셋할 때까지 대기
        }

        // BallSpawner가 ThrowInputHandler를 Idle로 리셋하면 다음 시도 허용
        if (_spinThrowInProgress && state == ThrowInputHandler.ThrowState.Idle)
            _spinThrowInProgress = false;
    }

    private void UpdatePositioningDetection(TutorialStepEntry entry)
    {
        if (throwInputHandler == null) return;

        var state = throwInputHandler.State;

        if (state == ThrowInputHandler.ThrowState.Oscillating ||
            state == ThrowInputHandler.ThrowState.ForwardDrag  ||
            state == ThrowInputHandler.ThrowState.Done)
        {
            throwInputHandler.Reset();
            return;
        }

        if (state == ThrowInputHandler.ThrowState.Positioning)
        {
            float x = throwInputHandler.CurrentMousePos.x;
            if (x < _minMouseX) _minMouseX = x;
            if (x > _maxMouseX) _maxMouseX = x;
        }

        if (_maxMouseX - _minMouseX >= requiredMovementPx)
            FinishWaiting(entry);
    }

    private void FinishWaiting(TutorialStepEntry entry)
    {
        if (entry.waitVisual != null) entry.waitVisual.SetActive(false);
        SetThrowEnabled(false);
        throwInputHandler?.Reset();

        bool hasCompletionLines = entry.data.completionLines != null &&
                                  entry.data.completionLines.Length > 0;
        if (hasCompletionLines)
        {
            _stepPhase = StepPhase.CompletionLines;
            _lineIndex = 0;
            ShowLine(entry.data.completionLines[0]);
        }
        else
        {
            MoveToNextStep();
        }
    }

    private void AdvanceCompletionLines()
    {
        var entry = steps[_stepIndex];
        _lineIndex++;

        if (_lineIndex >= entry.data.completionLines.Length)
        {
            MoveToNextStep();
            return;
        }

        ShowLine(entry.data.completionLines[_lineIndex]);
    }

    private void MoveToNextStep()
    {
        int next = _stepIndex + 1;
        if (next >= steps.Length)
        {
            SetThrowEnabled(true);
            onTutorialComplete?.Invoke();
            gameObject.SetActive(false);
            return;
        }

        StartStep(next);
    }

    // ── 유틸 ──────────────────────────────────────────────────────────────────

    private void SetThrowEnabled(bool enabled)
    {
        if (throwInputHandler != null) throwInputHandler.enabled = enabled;
        if (ballLauncher != null) ballLauncher.enabled = enabled;
    }

    private void ShowLine(string line)
    {
        if (dialogueText != null)
            dialogueText.text = line;
    }
}

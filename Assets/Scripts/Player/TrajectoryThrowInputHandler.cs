using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// ThrowInputHandler와 별개로 동작하는 입력 처리기.
// Positioning/Oscillating(좌우 위치잡기, 파워 게이지)은 동일한 방식을 쓰지만,
// 앞 드래그(ForwardDrag)에서는 궤적을 각도/곡률 스칼라 하나로 압축하지 않고
// 샘플링한 좌표 전체(ForwardDragPoints)를 그대로 외부에 공개한다.
// TrajectoryBallLauncher가 이 궤적 원본을 읽어 AddForce로 그대로 재현한다.
//
// [흐름]
//  Idle
//   → LMB 누름 → Positioning  (좌우 드래그로 공 위치 이동)
//   → 아래로 드래그 → Oscillating  (파워 PingPong 진동)
//   → 앞으로 드래그 → ForwardDrag  (궤적 좌표를 그대로 기록)
//   → LMB 릴리즈 → Done
public class TrajectoryThrowInputHandler : MonoBehaviour
{
    [Header("Backswing")]
    [Tooltip("이 픽셀 이상 아래로 드래그하면 파워 진동 시작")]
    [SerializeField] private float backswingThresholdPx = 40f;

    [Header("Oscillation")]
    [Tooltip("파워 진동 속도 (값이 클수록 빠름)")]
    [SerializeField] private float oscillationSpeed = 0.6f;
    [Tooltip("실제 파워(ThrowPowerNormalized)가 최대가 되는 스윙 지점(0~1). 0.5면 스윙 정중앙에서 최대.")]
    [SerializeField, Range(0.1f, 0.9f)] private float powerPeakPosition = 0.5f;
    [Tooltip("최대 파워 구간의 폭. 0.1이면 0.4~0.6 구간 전체가 최대 파워.")]
    [SerializeField, Range(0.01f, 0.4f)] private float powerPeakHalfWidth = 0.1f;

    [Header("Forward Drag")]
    [Tooltip("진동 중 가장 아래 지점에서 이 픽셀 이상 위로 올라오면 앞 드래그 시작")]
    [SerializeField] private float forwardThresholdPx = 20f;
    [Tooltip("앞 드래그 중 포인트 샘플링 최소 거리 (px)")]
    [SerializeField] private float sampleThreshold = 6f;

    // ── 공개 프로퍼티 ──────────────────────────────────────────────────────────

    public enum ThrowState { Idle, Positioning, Oscillating, ForwardDrag, Done }

    public ThrowState State { get; private set; } = ThrowState.Idle;
    public bool HasResult => State == ThrowState.Done;

    /// <summary>실제 투구 파워 [0,1]. Oscillating에서 확정.</summary>
    public float ThrowPowerNormalized { get; private set; }

    /// <summary>스윙 진행도 [0,1]. Oscillating 중 PingPong 원본 값. 게이지 UI 표시용.</summary>
    public float SwingPhaseNormalized { get; private set; }

    /// <summary>현재 마우스 스크린 좌표. TrajectoryBallLauncher가 Positioning 중 공 이동에 사용.</summary>
    public Vector2 CurrentMousePos { get; private set; }

    /// <summary>
    /// 앞 드래그 중 샘플링된 스크린 좌표 궤적 원본(시간순). 곡률/각도로 압축하지 않고 그대로 노출한다.
    /// TrajectoryBallLauncher가 Done 시점에 이 리스트를 읽어 진행률별 좌우 편차 테이블을 만든다.
    /// </summary>
    public IReadOnlyList<Vector2> ForwardDragPoints => _forwardPoints;

    // ── 내부 상태 ──────────────────────────────────────────────────────────────

    private Vector2 _dragStartPos;
    private float _lowestY;
    private float _oscillationTimer;

    private readonly List<Vector2> _forwardPoints = new List<Vector2>();

#if UNITY_EDITOR
    private GUIStyle _labelStyle;
    private static Texture2D _lineTex;
#endif

    // ── 외부 호출 ──────────────────────────────────────────────────────────────

    /// <summary>공 삭제 시 스포너가 호출 — 상태 전체 초기화.</summary>
    public void Reset()
    {
        State = ThrowState.Idle;
        ThrowPowerNormalized = 0f;
        SwingPhaseNormalized = 0f;
        _oscillationTimer = 0f;
        _forwardPoints.Clear();
    }

    // ── 입력 처리 ──────────────────────────────────────────────────────────────

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        Vector2 pos = mouse.position.ReadValue();
        CurrentMousePos = pos;

        switch (State)
        {
            case ThrowState.Idle:
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    _dragStartPos = pos;
                    State = ThrowState.Positioning;
                }
                break;

            case ThrowState.Positioning:
                if (!mouse.leftButton.isPressed)
                {
                    State = ThrowState.Idle;
                    break;
                }
                if (_dragStartPos.y - pos.y >= backswingThresholdPx)
                {
                    _oscillationTimer = 0f;
                    _lowestY = pos.y;
                    State = ThrowState.Oscillating;
                }
                break;

            case ThrowState.Oscillating:
                if (!mouse.leftButton.isPressed)
                {
                    State = ThrowState.Idle;
                    break;
                }
                if (pos.y < _lowestY)
                    _lowestY = pos.y;

                _oscillationTimer += Time.deltaTime;
                SwingPhaseNormalized = Mathf.PingPong(_oscillationTimer * oscillationSpeed * 2f, 1f);
                ThrowPowerNormalized = ComputeSweetSpotPower(SwingPhaseNormalized);

                // 앞으로 밀기 시작 → ForwardDrag
                if (pos.y - _lowestY >= forwardThresholdPx)
                {
                    _forwardPoints.Clear();
                    _forwardPoints.Add(pos);
                    State = ThrowState.ForwardDrag;
                }
                break;

            case ThrowState.ForwardDrag:
                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    State = ThrowState.Done;
                    break;
                }
                if (!mouse.leftButton.isPressed)
                {
                    State = ThrowState.Idle;
                    break;
                }
                // 포인트 샘플링 (곡률 계산 없이 좌표 자체를 그대로 모은다)
                if (_forwardPoints.Count == 0 ||
                    Vector2.Distance(pos, _forwardPoints[_forwardPoints.Count - 1]) >= sampleThreshold)
                {
                    _forwardPoints.Add(pos);
                }
                break;

            case ThrowState.Done:
                break;
        }
    }

    /// <summary>
    /// 스윙 진행도(swingPhase, 0~1)를 powerPeakPosition 기준 파워 [0,1]로 변환한다.
    /// peakPosition에서 1이 되고, 양 끝(0 또는 1)까지 대칭으로 0에 떨어진다.
    /// </summary>
    private float ComputeSweetSpotPower(float swingPhase)
    {
        float dist = Mathf.Abs(swingPhase - powerPeakPosition);
        if (dist <= powerPeakHalfWidth) return 1f;

        float maxDist = swingPhase < powerPeakPosition ? powerPeakPosition : 1f - powerPeakPosition;
        return 1f - Mathf.InverseLerp(powerPeakHalfWidth, maxDist, dist);
    }

    // ── 시각화 (에디터 전용) ────────────────────────────────────────────────────

#if UNITY_EDITOR
    private void OnGUI()
    {
        if (State == ThrowState.Idle) return;

        EnsureStyles();

        float power = ThrowPowerNormalized;

        // ForwardDrag 궤적 표시
        if (State == ThrowState.ForwardDrag && _forwardPoints.Count >= 2)
        {
            for (int i = 0; i < _forwardPoints.Count - 1; i++)
                DrawLine(ToGUI(_forwardPoints[i]), ToGUI(_forwardPoints[i + 1]), Color.yellow, 3f);
        }

        string label;
        switch (State)
        {
            case ThrowState.Positioning:
                label = "[궤적모드] 좌우로 위치 조정 후, 뒤로 당기세요";
                break;
            case ThrowState.Oscillating:
                label = $"[궤적모드] 파워: {power * 100f:F0}%  |  앞으로 밀며 궤적을 그리세요";
                break;
            case ThrowState.ForwardDrag:
                label = $"[궤적모드] 파워: {power * 100f:F0}%  |  그린 모양 그대로 공이 나아갑니다  |  놓으면 발사";
                break;
            default: // Done
                label = $"[궤적모드] 파워: {power * 100f:F0}%  |  발사됨";
                break;
        }
        _labelStyle.normal.textColor = Color.yellow;
        const float labelWidth = 420f;
        GUI.Label(new Rect(Screen.width - labelWidth - 10f, 70f, labelWidth, 40), label, _labelStyle);
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null) return;
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 14,
            alignment = TextAnchor.UpperRight
        };
    }

    private static Vector2 ToGUI(Vector2 screenPos)
        => new Vector2(screenPos.x, Screen.height - screenPos.y);

    private static void DrawLine(Vector2 from, Vector2 to, Color color, float width)
    {
        if (_lineTex == null)
        {
            _lineTex = new Texture2D(1, 1);
            _lineTex.SetPixel(0, 0, Color.white);
            _lineTex.Apply();
        }

        Color saved = GUI.color;
        GUI.color = color;
        Vector2 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, d.magnitude, width), _lineTex);
        GUI.matrix = savedMatrix;
        GUI.color = saved;
    }
#endif
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 볼링 투구 입력 처리
//
// [흐름]
//  Idle
//   → LMB 누름 → Positioning  (좌우 드래그로 공 위치 이동)
//   → 아래로 드래그 → Oscillating  (파워 PingPong 진동)
//   → 앞으로 드래그 → ForwardDrag  (궤적 곡률로 스핀 결정)
//   → LMB 릴리즈 → Done
//
// ThrowPowerNormalized     : [0,1] Oscillating에서 확정
// SpinNormalized           : [-1,1] ForwardDrag 궤적 곡률. 양수=반시계(왼쪽 휨), 음수=시계(오른쪽 휨)
public class ThrowInputHandler : MonoBehaviour
{
    [Header("UI Block")]
    [Tooltip("이 UI가 활성화되어 있으면 입력을 차단합니다 (예: MainMenuUI)")]
    [SerializeField] private GameObject _mainMenuUI;

    [Header("Backswing")]
    [Tooltip("이 픽셀 이상 아래로 드래그하면 파워 진동 시작")]
    [SerializeField] private float backswingThresholdPx = 40f;

    [Header("Oscillation")]
    [Tooltip("파워 진동 속도 (값이 클수록 빠름)")]
    [SerializeField] private float oscillationSpeed = 1.2f;

    [Header("Forward Drag")]
    [Tooltip("진동 중 가장 아래 지점에서 이 픽셀 이상 위로 올라오면 앞 드래그 시작")]
    [SerializeField] private float forwardThresholdPx = 20f;
    [Tooltip("앞 드래그 중 포인트 샘플링 최소 거리 (px)")]
    [SerializeField] private float sampleThreshold = 6f;

    [Header("Visualization")]
    [SerializeField] private float maxArrowLengthPx = 160f;

    // ── 공개 프로퍼티 ──────────────────────────────────────────────────────────

    public enum ThrowState { Idle, Positioning, Oscillating, ForwardDrag, Done }

    public ThrowState State { get; private set; } = ThrowState.Idle;
    public bool HasResult => State == ThrowState.Done;

    /// <summary>파워 [0,1]. Oscillating에서 확정.</summary>
    public float ThrowPowerNormalized { get; private set; }

    /// <summary>
    /// 스핀 [-1,1]. 앞 드래그 궤적의 곡률로 결정.
    /// 양수 = 반시계(왼쪽으로 휨), 음수 = 시계(오른쪽으로 휨).
    /// </summary>
    public float SpinNormalized { get; private set; }

    /// <summary>현재 마우스 스크린 좌표. BallLauncher가 Positioning 중 공 이동에 사용.</summary>
    public Vector2 CurrentMousePos { get; private set; }

    // ── 내부 상태 ──────────────────────────────────────────────────────────────

    private Vector2 _dragStartPos;
    private float _lowestY;
    private float _oscillationTimer;

    private readonly List<Vector2> _forwardPoints = new List<Vector2>();

    private GUIStyle _labelStyle;
    private static Texture2D _lineTex;

    // ── 외부 호출 ──────────────────────────────────────────────────────────────

    /// <summary>공 삭제 시 BallSpawner가 호출 — 상태 전체 초기화.</summary>
    public void Reset()
    {
        State = ThrowState.Idle;
        ThrowPowerNormalized = 0f;
        SpinNormalized = 0f;
        _oscillationTimer = 0f;
        _forwardPoints.Clear();
    }

    // ── 입력 처리 ──────────────────────────────────────────────────────────────

    private bool IsMenuActive => _mainMenuUI != null && _mainMenuUI.activeInHierarchy;

    private void Update()
    {
        if (IsMenuActive)
        {
            if (State != ThrowState.Idle && State != ThrowState.Done)
                State = ThrowState.Idle;
            return;
        }

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
                ThrowPowerNormalized = Mathf.PingPong(_oscillationTimer * oscillationSpeed * 2f, 1f);

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
                    SpinNormalized = ComputeSpin();
                    State = ThrowState.Done;
                    break;
                }
                if (!mouse.leftButton.isPressed)
                {
                    State = ThrowState.Idle;
                    break;
                }
                // 포인트 샘플링
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

    /// <summary>앞 드래그 궤적의 부호 있는 평균 곡률 [-1,1]을 반환한다.</summary>
    private float ComputeSpin()
    {
        if (_forwardPoints.Count < 3) return 0f;

        float crossSum = 0f;
        int count = 0;
        for (int i = 1; i < _forwardPoints.Count - 1; i++)
        {
            Vector2 a = _forwardPoints[i]     - _forwardPoints[i - 1];
            Vector2 b = _forwardPoints[i + 1] - _forwardPoints[i];
            float denom = a.magnitude * b.magnitude;
            if (denom < 0.0001f) continue;

            // 2D 외적: 양수 = 반시계 회전, 음수 = 시계 회전
            float cross = a.x * b.y - a.y * b.x;
            crossSum += cross / denom;
            count++;
        }

        return count == 0 ? 0f : Mathf.Clamp(crossSum / count, -1f, 1f);
    }

    // ── 시각화 ──────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (IsMenuActive || State == ThrowState.Idle) return;

        EnsureStyles();

        Vector2 origin = ToGUI(CurrentMousePos);
        float power = ThrowPowerNormalized;

        Color arrowColor = State == ThrowState.Positioning
            ? Color.white
            : Color.Lerp(Color.green, Color.red, power);

        // 화살표
        float arrowLen = State == ThrowState.Positioning
            ? maxArrowLengthPx * 0.25f
            : maxArrowLengthPx * power;

        var guiDir = new Vector2(0f, -1f).normalized;
        Vector2 tip = origin + guiDir * arrowLen;

        if (arrowLen > 1f)
        {
            DrawLine(origin, tip, arrowColor, 4f);
            if (arrowLen > 20f)
            {
                Vector2 headL = (Vector2)(Quaternion.Euler(0f, 0f,  25f) * -(Vector3)(guiDir * 14f));
                Vector2 headR = (Vector2)(Quaternion.Euler(0f, 0f, -25f) * -(Vector3)(guiDir * 14f));
                DrawLine(tip, tip + headL, arrowColor, 3f);
                DrawLine(tip, tip + headR, arrowColor, 3f);
            }
        }

        // ForwardDrag 궤적 표시
        if (State == ThrowState.ForwardDrag && _forwardPoints.Count >= 2)
        {
            for (int i = 0; i < _forwardPoints.Count - 1; i++)
                DrawLine(ToGUI(_forwardPoints[i]), ToGUI(_forwardPoints[i + 1]), Color.cyan, 3f);
        }

        // 상태 텍스트
        string label;
        Color labelColor;
        switch (State)
        {
            case ThrowState.Positioning:
                label = "좌우로 위치 조정 후, 뒤로 당기세요";
                labelColor = Color.white;
                break;
            case ThrowState.Oscillating:
                label = $"파워: {power * 100f:F0}%  |  앞으로 밀어 스핀 결정";
                labelColor = arrowColor;
                break;
            case ThrowState.ForwardDrag:
                label = $"파워: {power * 100f:F0}%  |  ( ) 모양으로 스핀 조절  |  놓으면 발사";
                labelColor = Color.cyan;
                break;
            default: // Done
                string spinText = Mathf.Abs(SpinNormalized) < 0.05f ? "없음"
                    : SpinNormalized > 0 ? $"← {SpinNormalized * 100f:F0}%"
                    : $"→ {-SpinNormalized * 100f:F0}%";
                label = $"파워: {power * 100f:F0}%  |  스핀: {spinText}";
                labelColor = arrowColor;
                break;
        }
        _labelStyle.normal.textColor = labelColor;
        GUI.Label(new Rect(10, 10, 600, 28), label, _labelStyle);
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null) return;
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 16
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
}

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
    [Tooltip("궤적 곡률에 곱하는 배율. 값이 클수록 같은 드래그로도 스핀이 더 크게 들어감.")]
    [SerializeField] private float spinSensitivity = 5f;
    [Tooltip("시작~끝을 잇는 직선 대비 부풀어진 정도(궤적 길이 대비 비율)가 이 값 미만이면 스핀 0으로 처리. 손떨림으로 대각선 직선 드래그에도 스핀이 살짝 들어가는 걸 방지.")]
    [SerializeField] private float straightnessDeadZone = 0.04f;

    [Header("Visualization")]
    [SerializeField] private float maxArrowLengthPx = 160f;

    [Header("Debug")]
    [Tooltip("테스트용: 파워를 항상 99%로 고정한다.")]
    [SerializeField] private bool debugForcePower99 = true;

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

    /// <summary>
    /// 앞으로 드래그를 시작한 첫 구간의 정규화된 방향 (화면 기준, x=오른쪽, y=위).
    /// 기본값 (0,1)=정면. BallLauncher가 이 방향을 카메라의 정면/오른쪽 축에 그대로 매핑해
    /// 드래그한 각도 그대로 발사되게 한다.
    /// </summary>
    public Vector2 InitialDragDirection { get; private set; } = Vector2.up;

    /// <summary>InitialDragDirection을 정면(위) 기준 부호 있는 각도(도)로 환산. 양수=왼쪽, 음수=오른쪽.</summary>
    public float InitialDragAngleDeg => Vector2.SignedAngle(Vector2.up, InitialDragDirection);

    /// <summary>Done 상태에서, 곡률이 감지되어 스핀이 걸렸으면 true(커브), 아니면 false(직선 대각선).</summary>
    public bool IsCurvedDrag => SpinNormalized != 0f;

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
        InitialDragDirection = Vector2.up;
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
                ThrowPowerNormalized = debugForcePower99 ? 0.99f : Mathf.PingPong(_oscillationTimer * oscillationSpeed * 2f, 1f);

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

                    // 곡률이 감지된 경우(커브)엔 초기 방향 반영은 취소하고 정면으로 발사한다.
                    // 커브는 스핀(BallMagnusEffect)이 담당 — 직선 드래그일 때만 초기 각도를 반영한다.
                    if (SpinNormalized != 0f)
                        InitialDragDirection = Vector2.up;

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

                    // 앞 드래그의 첫 구간 방향을 초기 발사 방향으로 한 번만 확정한다.
                    if (_forwardPoints.Count == 2)
                        InitialDragDirection = (_forwardPoints[1] - _forwardPoints[0]).normalized;
                }
                break;

            case ThrowState.Done:
                break;
        }
    }

    /// <summary>
    /// 앞 드래그 궤적이 시작~끝을 잇는 직선 대비 얼마나 옆으로 부풀었는지로 곡률 [-1,1]을 구한다.
    /// 완전한 대각선 직선 드래그(부풀음 없음)는 손떨림이 있어도 데드존 이하로 걸러져 스핀 0이 된다.
    /// </summary>
    private float ComputeSpin()
    {
        if (_forwardPoints.Count < 3) return 0f;

        Vector2 start = _forwardPoints[0];
        Vector2 end = _forwardPoints[_forwardPoints.Count - 1];
        Vector2 chord = end - start;
        float chordLen = chord.magnitude;
        if (chordLen < 0.0001f) return 0f;

        Vector2 chordDir = chord / chordLen;

        // 직선(코드)에서 가장 많이 벗어난 지점의 부호 있는 수직 거리를 찾는다.
        // 양수 = 반시계(왼쪽으로 부풂), 음수 = 시계(오른쪽으로 부풂).
        float maxSignedDeviation = 0f;
        foreach (var p in _forwardPoints)
        {
            Vector2 offset = p - start;
            float perp = chordDir.x * offset.y - chordDir.y * offset.x;
            if (Mathf.Abs(perp) > Mathf.Abs(maxSignedDeviation))
                maxSignedDeviation = perp;
        }

        float curvatureRatio = maxSignedDeviation / chordLen;
        if (Mathf.Abs(curvatureRatio) < straightnessDeadZone) return 0f;

        // 실제 공의 훅 방향(BallLauncher/BallMagnusEffect)과 부호를 맞추기 위해 반전한다.
        return Mathf.Clamp(-curvatureRatio * spinSensitivity, -1f, 1f);
    }

    // ── 시각화 ──────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (State == ThrowState.Idle) return;

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
                string shapeText = IsCurvedDrag ? "커브" : "직선(대각선)";
                string angleText = IsCurvedDrag ? "-" : $"{Mathf.Abs(InitialDragAngleDeg):F0}° {(InitialDragAngleDeg >= 0f ? "왼쪽" : "오른쪽")}";
                label = $"파워: {power * 100f:F0}%  |  스핀: {spinText}\n궤적 모양: {shapeText}  |  입력 각도: {angleText}";
                labelColor = arrowColor;
                break;
        }
        _labelStyle.normal.textColor = labelColor;
        const float labelWidth = 400f;
        GUI.Label(new Rect(Screen.width - labelWidth - 10f, 10f, labelWidth, 52), label, _labelStyle);
    }

    private void EnsureStyles()
    {
        if (_labelStyle != null) return;
        _labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize  = 16,
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
}

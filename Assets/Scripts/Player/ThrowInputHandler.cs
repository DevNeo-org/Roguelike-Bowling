using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Handles mouse drag input to compute bowling throw direction.
// Attach to Player_Test.
//
// LMB drag: record trajectory
// On release: compute throw angle via average recent direction + signed curvature offset (Plan A)
//
// Public result properties (ThrowDirectionScreen, ThrowAngleDeg) are read by
// the throw execution script in the next step.
public class ThrowInputHandler : MonoBehaviour
{
    [Header("Sampling")]
    [SerializeField] private float sampleThreshold = 8f;
    [SerializeField] private int maxPoints = 80;

    [Header("Direction — Plan A")]
    [Tooltip("릴리즈 직전 몇 개의 점으로 기본 방향을 구할지")]
    [SerializeField] private int recentPointCount = 10;
    [Tooltip("곡률 → 각도 오프셋 배율 (도 단위)")]
    [SerializeField, Range(0f, 90f)] private float curvatureScale = 30f;

    [Header("Visualization")]
    [SerializeField] private float arrowLength = 100f;
    [SerializeField] private Color trajectoryColor = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private Color directionColor = Color.yellow;

    /// <summary>릴리즈 후 true. 새 드래그를 시작하면 false로 초기화.</summary>
    public bool HasResult { get; private set; }

    /// <summary>
    /// 정규화된 투구 방향 (스크린 좌표 기준, Y축 위 = 양수).
    /// 다음 단계에서 월드 XZ 방향으로 변환해 사용.
    /// </summary>
    public Vector2 ThrowDirectionScreen { get; private set; }

    /// <summary>투구 각도 (도). 0 = 오른쪽, 90 = 화면 위쪽.</summary>
    public float ThrowAngleDeg { get; private set; }

    /// <summary>
    /// 릴리즈 직전 3점으로 산출한 부호 있는 곡률 프록시 [-1, 1].
    /// 양수 = 반시계(왼쪽으로 휨), 음수 = 시계(오른쪽으로 휨).
    /// BallLauncher가 스핀 적용에 사용.
    /// </summary>
    public float CurvatureValue { get; private set; }

    private readonly List<Vector2> _points = new List<Vector2>();
    private bool _isDragging;

    /// <summary>공 삭제 시 BallSpawner가 호출 — 드래그 궤적·결과 전부 초기화.</summary>
    public void Reset()
    {
        _points.Clear();
        _isDragging = false;
        HasResult = false;
        ThrowDirectionScreen = Vector2.zero;
        ThrowAngleDeg = 0f;
        CurvatureValue = 0f;
    }
    private static Texture2D _lineTex;
    private GUIStyle _labelStyle;

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
            BeginDrag(mouse.position.ReadValue());
        else if (_isDragging && mouse.leftButton.isPressed)
            ContinueDrag(mouse.position.ReadValue());
        else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            EndDrag(mouse.position.ReadValue());
    }

    private void BeginDrag(Vector2 pos)
    {
        _points.Clear();
        _points.Add(pos);
        _isDragging = true;
        HasResult = false;
    }

    private void ContinueDrag(Vector2 pos)
    {
        if (Vector2.Distance(pos, _points[_points.Count - 1]) < sampleThreshold)
            return;

        _points.Add(pos);

        if (_points.Count > maxPoints)
            _points.RemoveAt(0);
    }

    private void EndDrag(Vector2 pos)
    {
        ContinueDrag(pos);
        _isDragging = false;

        if (_points.Count < 2) return;

        CurvatureValue = 0f;  // ComputeThrowAngle 내부에서 갱신됨
        (ThrowDirectionScreen, ThrowAngleDeg) = ComputeThrowAngle();
        HasResult = true;
    }

    /// <summary>
    /// Plan A: 최근 N점 평균 방향 + 릴리즈 직전 3점 부호 곡률 오프셋
    /// </summary>
    private (Vector2 dir, float angleDeg) ComputeThrowAngle()
    {
        // 기본 방향: 최근 N 구간 벡터 합산
        int start = Mathf.Max(0, _points.Count - recentPointCount - 1);
        var sum = Vector2.zero;
        for (int i = start; i < _points.Count - 1; i++)
            sum += _points[i + 1] - _points[i];

        if (sum.sqrMagnitude < 0.0001f)
            return (Vector2.up, 90f);

        sum.Normalize();
        float baseAngle = Mathf.Atan2(sum.y, sum.x) * Mathf.Rad2Deg;

        // 곡률 보정: 마지막 3점의 부호 있는 곡률 프록시
        // cross > 0 → 반시계(왼쪽으로 휨) → 각도 증가
        // cross < 0 → 시계(오른쪽으로 휨) → 각도 감소
        float curvatureOffset = 0f;
        if (_points.Count >= 3)
        {
            Vector2 p0 = _points[_points.Count - 3];
            Vector2 p1 = _points[_points.Count - 2];
            Vector2 p2 = _points[_points.Count - 1];

            Vector2 a = p1 - p0;
            Vector2 b = p2 - p1;
            float cross = a.x * b.y - a.y * b.x;   // 2D 외적 (부호 있음)
            float denom = a.magnitude * b.magnitude;

            if (denom > 0.0001f)
            {
                CurvatureValue = cross / denom;          // 원시 곡률 프록시 [-1, 1] 저장
                curvatureOffset = CurvatureValue * curvatureScale;
            }
        }

        float finalAngle = baseAngle + curvatureOffset;
        var dir = new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        );

        return (dir, finalAngle);
    }

    // ────────────────────────────────────────────────────────────
    // 시각화 (테스트용 OnGUI)
    // ────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        if (_labelStyle == null)
        {
            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 14
            };
        }

        // 드래그 궤적
        if (_points.Count >= 2)
        {
            for (int i = 0; i < _points.Count - 1; i++)
                DrawLine(ToGUI(_points[i]), ToGUI(_points[i + 1]), trajectoryColor, 2f);
        }

        // 방향 화살표
        if (HasResult && _points.Count > 0)
        {
            Vector2 origin = ToGUI(_points[_points.Count - 1]);
            // OnGUI는 Y축 반전이므로 방향 벡터의 Y를 뒤집음
            var guiDir = new Vector2(ThrowDirectionScreen.x, -ThrowDirectionScreen.y);
            Vector2 tip = origin + guiDir * arrowLength;

            DrawLine(origin, tip, directionColor, 3f);

            // 화살촉
            Vector2 headL = (Vector2)(Quaternion.Euler(0f, 0f, 25f) * (-guiDir * 18f));
            Vector2 headR = (Vector2)(Quaternion.Euler(0f, 0f, -25f) * (-guiDir * 18f));
            DrawLine(tip, tip + headL, directionColor, 3f);
            DrawLine(tip, tip + headR, directionColor, 3f);

            _labelStyle.normal.textColor = directionColor;
            GUI.Label(
                new Rect(10, Screen.height - 50, 500, 30),
                $"투구 각도: {ThrowAngleDeg:F1}°  |  방향: ({ThrowDirectionScreen.x:F2}, {ThrowDirectionScreen.y:F2})",
                _labelStyle);
        }

        // 상태 안내
        string status = _isDragging
            ? "드래그 중..."
            : HasResult
                ? "릴리즈 완료 — 다시 드래그하면 초기화"
                : "마우스 왼쪽 버튼을 누른 채 드래그하세요";

        _labelStyle.normal.textColor = Color.white;
        GUI.Label(new Rect(10, 10, 500, 25), status, _labelStyle);
    }

    /// <summary>마우스 스크린 좌표(하단-좌측 원점) → OnGUI 좌표(상단-좌측 원점) 변환</summary>
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

        Color savedColor = GUI.color;
        GUI.color = color;

        Vector2 d = to - from;
        float angle = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        float length = d.magnitude;

        Matrix4x4 savedMatrix = GUI.matrix;
        GUIUtility.RotateAroundPivot(angle, from);
        GUI.DrawTexture(new Rect(from.x, from.y - width * 0.5f, length, width), _lineTex);
        GUI.matrix = savedMatrix;

        GUI.color = savedColor;
    }
}

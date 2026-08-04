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

    [Header("Speed")]
    [Tooltip("이 픽셀/초 이하면 최소 속도로 취급")]
    [SerializeField] private float minSpeedPx = 200f;
    [Tooltip("이 픽셀/초 이상이면 최대 속도로 취급")]
    [SerializeField] private float maxSpeedPx = 1500f;

    [Header("Direction — Plan A")]
    [Tooltip("릴리즈 직전 몇 개의 점으로 기본 방향을 구할지")]
    [SerializeField] private int recentPointCount = 10;
    [Tooltip("곡률 → 각도 오프셋 배율 (도 단위). 작을수록 초기 방향이 직선에 가까워짐")]
    [SerializeField, Range(0f, 30f)] private float curvatureScale = 7f;

    [Header("Visualization")]
    [SerializeField] private float arrowLength = 100f;
    [SerializeField] private Color trajectoryColor = new Color(0f, 1f, 1f, 0.8f);
    [SerializeField] private Color directionColor = Color.yellow;

    /// <summary>릴리즈 후 true. 새 드래그를 시작하면 false로 초기화.</summary>
    public bool HasResult { get; private set; }

    /// <summary>LMB를 누르고 있는 동안 true.</summary>
    public bool IsDragging => _isDragging;

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

    /// <summary>
    /// 릴리즈 시 마우스 이동 속도를 minSpeedPx~maxSpeedPx 범위로 정규화한 값 [0, 1].
    /// BallLauncher가 발사 속도 스케일링에 사용.
    /// </summary>
    public float ThrowSpeedNormalized { get; private set; }

    /// <summary>
    /// 드래그 중 마우스가 아래로 내려간 적 있으면 true (백스윙 판정).
    /// BallLauncher가 밀어내기/던지기 분기에 사용.
    /// </summary>
    public bool HasMovedBackward { get; private set; }

    [Header("Throw Style Detection")]
    [Tooltip("이 픽셀 이상 아래로 내려가면 백스윙으로 판정")]
    [SerializeField] private float backswingThresholdPx = 10f;

    [Header("Ball Hit Test")]
    [Tooltip("공을 눌러야 드래그가 시작된다 — 이 반경(픽셀) 안에서 누르면 공을 클릭한 것으로 인정")]
    [SerializeField] private float ballHitRadiusPx = 80f;

    private readonly List<Vector2> _points = new List<Vector2>();
    private readonly List<float> _timestamps = new List<float>();
    private bool _isDragging;
    private Rigidbody _ball;
    private Camera _camera;

    /// <summary>BallSpawner가 새 공 생성 후 호출 — 클릭 판정 대상(현재 공 위치)을 갱신한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
    }

    /// <summary>공 삭제 시 BallSpawner가 호출 — 드래그 궤적·결과 전부 초기화.</summary>
    public void Reset()
    {
        _points.Clear();
        _timestamps.Clear();
        _isDragging = false;
        HasResult = false;
        ThrowDirectionScreen = Vector2.zero;
        ThrowAngleDeg = 0f;
        CurvatureValue = 0f;
        ThrowSpeedNormalized = 0f;
        HasMovedBackward = false;
    }
    private static Texture2D _lineTex;
    private GUIStyle _labelStyle;

    private void Awake()
    {
        _camera = Camera.main;
    }

    private void Update()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            // 공을 눌렀을 때만 드래그 시작 — 화면 아무 데나 클릭해도 투구가 시작되던 문제 방지.
            if (IsPointerOnBall(mouse.position.ReadValue()))
                BeginDrag(mouse.position.ReadValue());
        }
        else if (_isDragging && mouse.leftButton.isPressed)
            ContinueDrag(mouse.position.ReadValue());
        else if (_isDragging && mouse.leftButton.wasReleasedThisFrame)
            EndDrag(mouse.position.ReadValue());
    }

    private bool IsPointerOnBall(Vector2 screenPos)
    {
        if (_ball == null || _camera == null) return false;

        Vector3 ballScreenPos = _camera.WorldToScreenPoint(_ball.position);
        if (ballScreenPos.z < 0f) return false; // 카메라 뒤에 있으면 클릭 대상 아님

        float dist = Vector2.Distance(screenPos, new Vector2(ballScreenPos.x, ballScreenPos.y));
        return dist <= ballHitRadiusPx;
    }

    private void BeginDrag(Vector2 pos)
    {
        _points.Clear();
        _timestamps.Clear();
        _points.Add(pos);
        _timestamps.Add(Time.unscaledTime);
        _isDragging = true;
        HasResult = false;
        HasMovedBackward = false;
    }

    private void ContinueDrag(Vector2 pos)
    {
        // 백스윙 감지: sampleThreshold 무관하게 매 프레임 체크
        float deltaY = pos.y - _points[_points.Count - 1].y;
        if (deltaY < -backswingThresholdPx)
            HasMovedBackward = true;

        if (Vector2.Distance(pos, _points[_points.Count - 1]) < sampleThreshold)
            return;

        _points.Add(pos);
        _timestamps.Add(Time.unscaledTime);

        if (_points.Count > maxPoints)
        {
            _points.RemoveAt(0);
            _timestamps.RemoveAt(0);
        }
    }

    private void EndDrag(Vector2 pos)
    {
        // 릴리즈 순간은 백스윙 감지 없이 포인트만 추가
        if (Vector2.Distance(pos, _points[_points.Count - 1]) >= sampleThreshold)
        {
            _points.Add(pos);
            _timestamps.Add(Time.unscaledTime);
            if (_points.Count > maxPoints)
            {
                _points.RemoveAt(0);
                _timestamps.RemoveAt(0);
            }
        }
        _isDragging = false;

        if (_points.Count < 2) return;

        CurvatureValue = 0f;  // ComputeThrowAngle 내부에서 갱신됨
        (ThrowDirectionScreen, ThrowAngleDeg) = ComputeThrowAngle();
        ThrowSpeedNormalized = ComputeThrowSpeedNormalized();
        HasResult = true;
    }

    /// <summary>
    /// 최근 N점 평균 방향 + 전체 궤적 평균 곡률 오프셋
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

        // 곡률 보정: 전체 궤적의 연속 3점마다 부호 있는 곡률을 구해 평균냄
        float curvatureOffset = 0f;
        if (_points.Count >= 3)
        {
            float crossSum = 0f;
            int count = 0;
            for (int i = 1; i < _points.Count - 1; i++)
            {
                Vector2 a = _points[i]     - _points[i - 1];
                Vector2 b = _points[i + 1] - _points[i];
                float denom = a.magnitude * b.magnitude;
                if (denom > 0.0001f)
                {
                    float cross = a.x * b.y - a.y * b.x;
                    crossSum += cross / denom;   // 정규화된 곡률 [-1, 1]
                    count++;
                }
            }

            if (count > 0)
            {
                CurvatureValue = crossSum / count;       // 전체 평균 곡률
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

    /// <summary>
    /// 최근 recentPointCount 구간의 픽셀/초 평균 속도를 [0,1]로 정규화해 반환.
    /// </summary>
    private float ComputeThrowSpeedNormalized()
    {
        int start = Mathf.Max(0, _points.Count - recentPointCount - 1);
        float totalDist = 0f;
        for (int i = start; i < _points.Count - 1; i++)
            totalDist += Vector2.Distance(_points[i], _points[i + 1]);

        float timeSpan = _timestamps[_timestamps.Count - 1] - _timestamps[start];
        if (timeSpan < 0.0001f) return 0f;

        float rawSpeed = totalDist / timeSpan; // px/s
        return Mathf.Clamp01(Mathf.InverseLerp(minSpeedPx, maxSpeedPx, rawSpeed));
    }

    // ────────────────────────────────────────────────────────────
    // 시각화 (테스트용 OnGUI)
    // ────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        // 메인메뉴/설정/도감/정비 화면 등 실제 투구 중이 아닐 때는 이 디버그 표시가 필요 없다.
        if (StageManager.Instance == null || !StageManager.Instance.IsPlaying)
            return;

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
                new Rect(10, Screen.height - 50, 700, 30),
                $"투구 각도: {ThrowAngleDeg:F1}°  |  방향: ({ThrowDirectionScreen.x:F2}, {ThrowDirectionScreen.y:F2})  |  속도: {ThrowSpeedNormalized:F2}",
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

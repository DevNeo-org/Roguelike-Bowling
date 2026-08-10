using System.Collections.Generic;
using UnityEngine;

// BallLauncher와 별개로 동작하는 대체 발사기.
// BallLauncher는 발사 순간 linearVelocity/angularVelocity를 한 번에 세팅하지만,
// 이 스크립트는 TrajectoryThrowInputHandler가 기록한 앞 드래그 궤적 좌표를
// "진행률(0~1) → 그 지점에서의 진행 방향(코드 대비 좌/우로 꺾인 정도)" 테이블로 변환해두고,
// 발사 후 공이 굴러가는 동안 매 물리 틱마다 AddForce로 전진시키면서 그 방향을 참고한
// 완만한 조향 힘을 준다. 궤적의 정확한 좌표를 그대로 따라가도록 강제하지는 않는다 —
// "이 구간에서는 대략 이 방향으로 꺾여있었다" 정도만 반영해 부드럽게 휘어지게 한다.
//
// - 전진 거리: 뒤로 당길 때 확정한 파워(ThrowPowerNormalized)로 결정된다
//   (minLaunchDistance~maxLaunchDistance 사이를 보간).
// - 좌우 방향: 드래그 궤적의 국소적인 꺾임 방향을 부드럽게(지수 스무딩) 따라간다.
//
// TrajectoryThrowInputHandler와 함께 Player 오브젝트에 부착하세요.
// _ball은 런타임에 SetBall()로 설정됩니다.
[RequireComponent(typeof(TrajectoryThrowInputHandler))]
public class TrajectoryBallLauncher : MonoBehaviour
{
    [Header("Distance (Power 기반)")]
    [Tooltip("파워 0일 때 공이 나아가는 목표 거리 (m)")]
    [SerializeField] private float minLaunchDistance = 4f;
    [Tooltip("파워 1일 때 공이 나아가는 목표 거리 (m)")]
    [SerializeField] private float maxLaunchDistance = 16f;

    [Header("Propulsion")]
    [Tooltip("목표 거리에 도달하기 전까지 매 틱 가하는 전진 힘. 공 무게에 따라 가속도가 달라진다(F=ma).")]
    [SerializeField] private float forwardForce = 55f;

    [Header("Trajectory Follow (궤적 추종)")]
    [Tooltip("궤적의 국소 방향(코드 대비 좌/우로 꺾인 정도, -1~1)에 곱하는 조향 힘 배율. 궤적 좌표를 정확히 따라가진 않고, 대략적인 굽음 방향만 반영한다.")]
    [SerializeField] private float steerForceScale = 15f;
    [Tooltip("조향 힘이 목표 방향으로 수렴하는 속도. 클수록 방향 전환이 빠르고, 작을수록 완만하게 이어진다.")]
    [SerializeField] private float steerSmoothing = 2f;
    [Tooltip("조향 힘이 100% 세기로 걸리기 시작하는 기준 속도(m/s). 공의 현재 속도가 이보다 느리면 " +
        "그 비율만큼 조향 힘도 줄어든다 — 정지 직전처럼 전진 속도가 거의 0인데 조향 힘만 일정하게 " +
        "걸리면, 방향(heading)이 급격히 꺾이거나 제자리에서 도는 것처럼 보이는 문제를 막기 위함.")]
    [SerializeField] private float steerReferenceSpeed = 2f;
    [Tooltip("궤적 진행률(progress) 계산에 쓰는 거리를 목표 거리(_targetDistance)의 몇 배로 늘릴지. " +
        "전진력은 목표 거리에서 끊기지만 공은 관성으로 그 뒤에도 계속 구르기 때문에, 진행률 기준을 " +
        "그대로 목표 거리로 잡으면 궤적이 일찍 다 소진돼 그 이후엔 방향이 고정값으로 얼어붙어 갑자기 " +
        "직진하는 것처럼 보인다. 이 배율만큼 궤적을 더 늘려 펴서 관성 구간까지 자연스럽게 이어지게 한다.")]
    [SerializeField] private float curveDistanceMultiplier = 2.5f;

    [Header("Positioning")]
    [Tooltip("좌우 이동 가능 최대 범위 (m)")]
    [SerializeField] private float armRadius = 1.5f;

    private struct TrajSample { public float progress; public float bias; }
    private readonly List<TrajSample> _table = new List<TrajSample>();

    private Rigidbody _ball;
    private BallResetter _ballResetter;
    private Vector3 _spawnPosition;
    private Camera _camera;
    private TrajectoryThrowInputHandler _input;
    private bool _canLaunch = true;

    private bool _isLaunched;
    private Vector3 _launchPos;
    private Vector3 _laneForward;
    private Vector3 _laneRight;
    private float _targetDistance;
    private float _curveDistance;
    private float _smoothedBias;

    private void Awake()
    {
        _input = GetComponent<TrajectoryThrowInputHandler>();
        _camera = Camera.main;
    }

    private void Update()
    {
        if (!_canLaunch) return;

        if (_input.State == TrajectoryThrowInputHandler.ThrowState.Positioning)
            MoveBallHorizontally();
        else if (_input.HasResult)
            Launch();
    }

    private void FixedUpdate()
    {
        if (!_isLaunched || _ball == null) return;

        float traveled = Vector3.Dot(_ball.position - _launchPos, _laneForward);

        // 목표 거리에 도달하기 전까지만 전진 힘을 준다 — 이후엔 레인 마찰(LaneFrictionZone)에 맡긴다.
        if (traveled < _targetDistance)
            _ball.AddForce(_laneForward * forwardForce, ForceMode.Force);

        float progress = _curveDistance > 0.0001f ? Mathf.Clamp01(traveled / _curveDistance) : 1f;
        float targetBias = SampleTrajectory(progress);

        // 목표 방향으로 지수적으로 수렴시켜 급격한 방향 전환 없이 부드럽게 이어지게 한다.
        float smoothT = 1f - Mathf.Exp(-steerSmoothing * Time.fixedDeltaTime);
        _smoothedBias = Mathf.Lerp(_smoothedBias, targetBias, smoothT);

        // 조향 힘을 현재 속도에 비례해서 줄인다 — 그렇지 않으면 정지 직전처럼 전진 속도가
        // 거의 0인 상태에서 일정한 조향 힘만 계속 걸려 방향이 급격히 꺾이거나 제자리에서
        // 도는 것처럼 보인다(BallMagnusEffect가 linearVelocity에 비례하는 것과 같은 이유).
        float speedScale = Mathf.Clamp01(_ball.linearVelocity.magnitude / steerReferenceSpeed);
        _ball.AddForce(_laneRight * (_smoothedBias * steerForceScale * speedScale), ForceMode.Force);
    }

    /// <summary>Positioning 상태에서 마우스 좌우에 따라 공을 레인 위에서 이동.</summary>
    private void MoveBallHorizontally()
    {
        if (_ball == null) return;

        Vector3 camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
        Vector3 laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        var plane = new Plane(laneForward, _spawnPosition);
        Ray ray = _camera.ScreenPointToRay(_input.CurrentMousePos);
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 hit = ray.GetPoint(dist);

        float lateral = Vector3.Dot(hit - _spawnPosition, camRight);
        lateral = Mathf.Clamp(lateral, -armRadius, armRadius);

        _ball.MovePosition(_spawnPosition + camRight * lateral);
    }

    private void Launch()
    {
        if (_ball == null) return;

        _canLaunch = false;
        _ball.isKinematic = false;
        _ball.linearVelocity = Vector3.zero;
        _ball.angularVelocity = Vector3.zero;

        _laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
        _laneRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
        _launchPos = _ball.position;
        _targetDistance = Mathf.Lerp(minLaunchDistance, maxLaunchDistance, _input.ThrowPowerNormalized);
        _curveDistance = _targetDistance * curveDistanceMultiplier;

        BuildTrajectoryTable(_input.ForwardDragPoints);
        _smoothedBias = SampleTrajectory(0f);

        _isLaunched = true;
        _ballResetter?.SetLaunched();
    }

    /// <summary>
    /// 드래그 궤적(스크린 좌표)을 "진행률(0~1) → 그 지점에서의 국소 방향(초기 드래그 방향 대비
    /// 좌/우로 꺾인 정도, 정규화)" 테이블로 변환한다. 기준 방향은 "시작~끝을 잇는 전체 직선(코드)"이
    /// 아니라 "드래그를 시작한 첫 구간의 방향" 하나로 고정한다 — 코드를 기준으로 삼으면 ㄱ자처럼
    /// 도중에 꺾이는 궤적에서 코드 자체가 이미 꺾인 방향으로 기울어버려, 앞부분의 직진 구간이
    /// 반대쪽으로 밀린 것처럼 잘못 해석되어 불필요하게 돌아가는 움직임이 생기기 때문이다.
    /// </summary>
    private void BuildTrajectoryTable(IReadOnlyList<Vector2> points)
    {
        _table.Clear();

        if (points == null || points.Count < 2)
        {
            _table.Add(new TrajSample { progress = 0f, bias = 0f });
            _table.Add(new TrajSample { progress = 1f, bias = 0f });
            return;
        }

        Vector2 initialSeg = points[1] - points[0];
        Vector2 refDir = initialSeg.sqrMagnitude > 0.0001f ? initialSeg.normalized : Vector2.up;

        float totalLen = 0f;
        for (int i = 1; i < points.Count; i++)
            totalLen += Vector2.Distance(points[i - 1], points[i]);

        float cum = 0f;
        float lastBias = 0f;
        for (int i = 0; i < points.Count; i++)
        {
            if (i > 0) cum += Vector2.Distance(points[i - 1], points[i]);
            float progress = totalLen > 0.0001f ? cum / totalLen : 0f;

            float bias;
            if (i < points.Count - 1)
            {
                Vector2 seg = points[i + 1] - points[i];
                Vector2 tangent = seg.sqrMagnitude > 0.0001f ? seg.normalized : refDir;
                float cross = refDir.x * tangent.y - refDir.y * tangent.x; // sin(각도), 양수=왼쪽으로 꺾임

                // BallLauncher의 dragDir.x(양수=오른쪽) 관례와 맞추기 위해 부호를 반전한다.
                bias = -cross;
                lastBias = bias;
            }
            else
            {
                bias = lastBias; // 마지막 점은 직전 구간의 방향을 그대로 이어받는다.
            }

            _table.Add(new TrajSample { progress = progress, bias = bias });
        }
    }

    /// <summary>진행률 t(0~1)에 해당하는 국소 방향(정규화)을 테이블에서 보간해 읽는다.</summary>
    private float SampleTrajectory(float t)
    {
        if (_table.Count == 0) return 0f;
        if (t <= _table[0].progress) return _table[0].bias;

        for (int i = 1; i < _table.Count; i++)
        {
            if (t <= _table[i].progress)
            {
                TrajSample a = _table[i - 1];
                TrajSample b = _table[i];
                float span = b.progress - a.progress;
                float local = span > 0.0001f ? (t - a.progress) / span : 0f;
                return Mathf.Lerp(a.bias, b.bias, local);
            }
        }
        return _table[_table.Count - 1].bias;
    }

    /// <summary>스포너가 새 공 생성 후 호출 — 투구 대상 Rigidbody를 교체한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
        _ballResetter = rb != null ? rb.GetComponent<BallResetter>() : null;
        if (rb != null)
            _spawnPosition = rb.position;
        _isLaunched = false;
    }

    /// <summary>스포너가 공 생성 완료 후 호출 — 다음 투구를 허용한다.</summary>
    public void ResetLaunch()
    {
        _canLaunch = true;
        _isLaunched = false;
    }
}

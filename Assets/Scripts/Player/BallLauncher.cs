using UnityEngine;

// ThrowInputHandler의 파워/방향 결과를 읽어 공을 발사합니다.
// ThrowInputHandler와 함께 Player 오브젝트에 부착하세요.
// _ball은 런타임에 BallSpawner.SetBall()로 설정됩니다.
[RequireComponent(typeof(ThrowInputHandler))]
public class BallLauncher : MonoBehaviour
{
    [Header("Launch Speed")]
    [Tooltip("파워 0일 때 발사 속도 (m/s)")]
    [SerializeField] private float minLaunchSpeed = 2f;
    [Tooltip("파워 1일 때 발사 속도 (m/s)")]
    [SerializeField] private float maxLaunchSpeed = 8f;

    [Header("Positioning")]
    [Tooltip("좌우 이동 가능 최대 범위 (m)")]
    [SerializeField] private float armRadius = 1.5f;

    [Header("Spin")]
    [Tooltip("SpinNormalized(±1)에 곱해 angularVelocity(rad/s)로 변환. 값이 클수록 더 크게 휨.")]
    [SerializeField] private float spinScale = 10f;

    [Header("Weight Effect")]
    [Tooltip("발사 속도 보정 기준 질량(기본 9파운드 공의 질량).")]
    [SerializeField] private float referenceMass = 7f;
    [Tooltip("기준 질량 대비 무게 편차 비율 1.0(=100% 초과)당 속도를 얼마나 줄이거나 늘릴지. 작게 유지해 무게 때문에 너무 느려지거나 빨라지지 않게 한다.")]
    [SerializeField] private float weightSpeedInfluence = 0.1f;
    [Tooltip("무게 보정으로 속도가 변할 수 있는 최대/최소 배율 범위.")]
    [SerializeField] private Vector2 weightSpeedFactorRange = new Vector2(0.85f, 1.15f);

    [Header("UI Block")]
    [Tooltip("이 UI가 활성화되어 있으면 조작을 차단합니다 (예: MainMenuUI)")]
    [SerializeField] private GameObject _mainMenuUI;

    private Rigidbody _ball;
    private BallResetter _ballResetter;
    private Vector3 _spawnPosition;
    private Camera _camera;
    private ThrowInputHandler _input;
    private bool _canLaunch = true;

    private void Awake()
    {
        _input = GetComponent<ThrowInputHandler>();
        _camera = Camera.main;
    }

    private void Update()
    {
        if (!_canLaunch) return;
        if (_mainMenuUI != null && _mainMenuUI.activeInHierarchy) return;

        if (_input.State == ThrowInputHandler.ThrowState.Positioning)
            MoveBallHorizontally();
        else if (_input.HasResult)
            Launch();
    }

    /// <summary>Positioning 상태에서 마우스 좌우에 따라 공을 레인 위에서 이동.</summary>
    private void MoveBallHorizontally()
    {
        if (_ball == null) return;

        Vector3 camRight = Vector3.ProjectOnPlane(_camera.transform.right, Vector3.up).normalized;
        Vector3 laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        // 레인 방향을 법선으로 하는 평면에 레이캐스트
        var plane = new Plane(laneForward, _spawnPosition);
        Ray ray = _camera.ScreenPointToRay(_input.CurrentMousePos);
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 hit = ray.GetPoint(dist);

        // 좌우 성분만 추출하여 스폰 위치 기준으로 클램프
        float lateral = Vector3.Dot(hit - _spawnPosition, camRight);
        lateral = Mathf.Clamp(lateral, -armRadius, armRadius);

        _ball.MovePosition(_spawnPosition + camRight * lateral);
    }

    private void Launch()
    {
        if (_ball == null) return;

        _canLaunch = false;
        _ball.isKinematic = false;

        float speed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, _input.ThrowPowerNormalized);

        // 무게에 따라 속도를 살짝만 보정한다 (무거우면 약간 느리게, 가벼우면 약간 빠르게).
        // 체감이 크지 않도록 weightSpeedInfluence/range로 폭을 좁게 제한한다.
        float massDeviation = (_ball.mass - referenceMass) / referenceMass;
        float speedFactor = Mathf.Clamp(1f - massDeviation * weightSpeedInfluence,
            weightSpeedFactorRange.x, weightSpeedFactorRange.y);
        speed *= speedFactor;

        Vector3 worldDir = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        _ball.linearVelocity  = worldDir * speed;
        _ball.angularVelocity = Vector3.up * _input.SpinNormalized * spinScale;


        _ballResetter?.SetLaunched();
    }

    /// <summary>BallSpawner가 새 공 생성 후 호출 — 투구 대상 Rigidbody를 교체한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
        _ballResetter = rb != null ? rb.GetComponent<BallResetter>() : null;
        if (rb != null)
            _spawnPosition = rb.position;
    }

    /// <summary>BallSpawner가 공 생성 완료 후 호출 — 다음 투구를 허용한다.</summary>
    public void ResetLaunch()
    {
        _canLaunch = true;
    }
}

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

        Vector3 worldDir = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        _ball.linearVelocity  = worldDir * speed;
        _ball.angularVelocity = Vector3.up * _input.SpinNormalized * spinScale;

        Debug.Log($"[투구] 파워: {_input.ThrowPowerNormalized:F2} | 속도: {speed:F1} m/s | 스핀: {_input.SpinNormalized:F3} | angularVel: {_ball.angularVelocity}");

        _ballResetter?.SetLaunched();
    }

    /// <summary>BallSpawner가 새 공 생성 후 호출 — 투구 대상 Rigidbody를 교체한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
        _ballResetter = rb != null ? rb.GetComponent<BallResetter>() : null;
        if (rb != null)
        {
            _spawnPosition = rb.position;
            rb.isKinematic = true; // 발사 전까지 중력/물리 정지
        }
    }

    /// <summary>BallSpawner가 공 생성 완료 후 호출 — 다음 투구를 허용한다.</summary>
    public void ResetLaunch()
    {
        _canLaunch = true;
        Debug.Log("[투구] 재투구 대기 중");
    }
}

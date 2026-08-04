using UnityEngine;
using UnityEngine.InputSystem;

// Reads the computed throw direction from ThrowInputHandler and launches the ball.
// Attach to Player_Test alongside ThrowInputHandler.
// _ball is set at runtime by BallSpawner.SetBall().
[RequireComponent(typeof(ThrowInputHandler))]
public class BallLauncher : MonoBehaviour
{
    [Tooltip("마우스를 가장 느리게 움직였을 때 발사 속도 (m/s)")]
    [SerializeField] private float minLaunchSpeed = 1f;
    [Tooltip("마우스를 가장 빠르게 움직였을 때 발사 속도 (m/s)")]
    [SerializeField] private float maxLaunchSpeed = 4f;
    [Tooltip("곡률값([-1,1])에 곱해 Y축 각속도(rad/s)로 변환")]
    [SerializeField] private float spinScale = 15f;

    [Header("Arm Range")]
    [Tooltip("팔 가동 범위 반경 (m)")]
    [SerializeField] private float armRadius = 1.5f;

    [Header("Throw Style")]
    [Tooltip("밀어내기 발사 시작 높이 (Y)")]
    [SerializeField] private float pushStartHeight = 0.5f;

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

        if (_input.IsDragging)
        {
            MoveBallToMouse();
            return;
        }

        if (_input.HasResult)
            Launch();
    }

    private void MoveBallToMouse()
    {
        if (_ball == null) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        _ball.isKinematic = true;

        // 레인 방향에 수직인 평면 — 공이 앞뒤(레인 방향)로 이동하지 않음
        Vector3 laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
        var plane = new Plane(laneForward, _spawnPosition);

        Ray ray = _camera.ScreenPointToRay(mouse.position.ReadValue());
        if (!plane.Raycast(ray, out float dist)) return;

        Vector3 target = ray.GetPoint(dist);

        // 스폰 위치 기준 팔 가동 범위 반경 제한
        Vector3 offset = target - _spawnPosition;
        if (offset.magnitude > armRadius)
            target = _spawnPosition + offset.normalized * armRadius;

        _ball.MovePosition(target);
    }

    private void Launch()
    {
        _canLaunch = false;
        _ball.isKinematic = false;

        float launchSpeed = Mathf.Lerp(minLaunchSpeed, maxLaunchSpeed, _input.ThrowSpeedNormalized);

        // 드래그 중 뒤로 내려간 적 있으면 밀어내기, 앞으로만 이동했으면 던지기
        if (_input.HasMovedBackward)
            ApplyPushThrow(launchSpeed);
        else
            ApplySwingThrow(launchSpeed);

        _ballResetter?.SetLaunched();
    }

    /// <summary>스윙 던지기 — 릴리즈 시 공의 실제 위치(좌우 오프셋)로 발사 방향 결정 + 곡률 스핀.</summary>
    private void ApplySwingThrow(float launchSpeed)
    {
        Vector3 laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;
        Vector3 camRight    = Vector3.ProjectOnPlane(_camera.transform.right,   Vector3.up).normalized;

        // 공의 좌우 오프셋을 발사 방향에 반영
        float lateralAmount = Vector3.Dot(_ball.position - _spawnPosition, camRight);
        float lateralRatio  = Mathf.Clamp(lateralAmount / armRadius, -1f, 1f);
        Vector3 worldDir    = (laneForward + camRight * lateralRatio).normalized;

        // 밀어내기(ApplyPushThrow)와 마찬가지로 레인 높이로 보정한다 - 안 그러면 조준 중
        // 들고 있던 높이(spawnPosition.y)에서 그대로 자유낙하해 레인에 강하게 부딪히며 크게 튕겨오른다.
        Vector3 pos = _ball.position;
        pos.y = pushStartHeight;
        _ball.position = pos;

        _ball.linearVelocity  = worldDir * launchSpeed;
        _ball.angularVelocity = Vector3.up * _input.CurvatureValue * spinScale;

        Debug.Log($"[스윙 던지기] 방향: {worldDir:F2} | 속도: {launchSpeed:F1} m/s | 스핀: {_ball.angularVelocity.y:F2} rad/s");
    }

    /// <summary>밀어내기 — 레인 정방향으로 스핀 없이 직진.</summary>
    private void ApplyPushThrow(float launchSpeed)
    {
        Vector3 laneForward = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up).normalized;

        Vector3 pos = _ball.position;
        pos.y = pushStartHeight;
        _ball.position = pos;

        _ball.linearVelocity  = laneForward * launchSpeed;
        _ball.angularVelocity = Vector3.up * _input.CurvatureValue * spinScale;

        Debug.Log($"[밀어내기] 속도: {launchSpeed:F1} m/s | 스핀: {_ball.angularVelocity.y:F2} rad/s");
    }

    /// <summary>BallSpawner가 새 공 생성 후 호출 — 투구 대상 Rigidbody를 교체한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
        _ballResetter = rb != null ? rb.GetComponent<BallResetter>() : null;
        if (rb != null) _spawnPosition = rb.position;
    }

    /// <summary>BallSpawner가 공 생성 완료 후 호출 — 다음 투구를 허용한다.</summary>
    public void ResetLaunch()
    {
        _canLaunch = true;
        Debug.Log("[투구] 재투구 대기 중");
    }
}

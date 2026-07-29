using UnityEngine;

// Reads the computed throw direction from ThrowInputHandler and launches the ball.
// Attach to Player_Test alongside ThrowInputHandler.
// _ball is set at runtime by BallSpawner.SetBall().
[RequireComponent(typeof(ThrowInputHandler))]
public class BallLauncher : MonoBehaviour
{
    [SerializeField] private float launchSpeed = 8f;
    [Tooltip("곡률값([-1,1])에 곱해 Y축 각속도(rad/s)로 변환")]
    [SerializeField] private float spinScale = 15f;

    private Rigidbody _ball;

    private ThrowInputHandler _input;
    private bool _canLaunch = true;

    private void Awake()
    {
        _input = GetComponent<ThrowInputHandler>();
    }

    private void Update()
    {
        if (!_canLaunch || !_input.HasResult) return;

        Launch(_input.ThrowDirectionScreen);
    }

    private void Launch(Vector2 screenDir)
    {
        _canLaunch = false;

        // 카메라 수평 방향 기준으로 스크린 XY → 월드 XZ 투영
        Camera cam = Camera.main;
        Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;
        Vector3 worldDir   = (camForward * screenDir.y + camRight * screenDir.x).normalized;

        _ball.linearVelocity  = worldDir * launchSpeed;
        _ball.angularVelocity = Vector3.up * _input.CurvatureValue * spinScale;

        Debug.Log($"[투구] 방향: {worldDir:F2} | 속도: {launchSpeed} m/s | 스핀(Y): {_ball.angularVelocity.y:F2} rad/s");
    }

    /// <summary>BallSpawner가 새 공 생성 후 호출 — 투구 대상 Rigidbody를 교체한다.</summary>
    public void SetBall(Rigidbody rb)
    {
        _ball = rb;
    }

    /// <summary>BallSpawner가 공 생성 완료 후 호출 — 다음 투구를 허용한다.</summary>
    public void ResetLaunch()
    {
        _canLaunch = true;
        Debug.Log("[투구] 재투구 대기 중");
    }
}

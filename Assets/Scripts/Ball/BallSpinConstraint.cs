using UnityEngine;

// 게임플레이에 쓰이는 스핀은 Y축(BallLauncher, BallMagnusEffect 참고) 하나뿐이다.
// 거터 벽 등 설계에 없는 충돌에서 PhysX가 Y축 회전을 임의로 튀게 만들면
// 회전이 갑자기 끊기거나 비정상적으로 튀는 현상이 생긴다.
// X/Z축은 건드리지 않는다 — 굴러가며 자연스럽게 생기는 구름 회전(PhysX가
// 미끄러짐을 없애려고 붙이는 회전)까지 강제로 지워버리면 공이 항상
// "미끄러지는 중"으로 취급돼 마찰 감속이 비정상적으로 세지기 때문이다.
//
// Y축 감쇠는 상시로 걸지 않는다 — 상시로 걸면 발사 시 넣어준 정상적인 훅
// 스핀까지 착지 후 굴러가는 동안 계속 깎여서, 커브를 그리다가 다시 직진으로
// 수렴해버리는 부작용이 있었다. 대신 한 틱 사이에 Y축 회전이 비정상적으로
// 급변하는 순간(=이상 충돌)만 감지해서 그 직후 짧게만 강한 감쇠를 걸고,
// 평소에는 아주 약한 기본 감쇠만 유지해 정상 커브가 오래 살아있게 한다.
// 레인(또는 무엇이든)에 처음 닿기 전까지는 아예 적용하지 않는다 — 공중에
// 떠 있는 동안 BallLauncher가 넣어준 의도된 발사 스핀까지 깎이면 안 되기 때문.
//
// Ball prefab에 부착.
[RequireComponent(typeof(Rigidbody))]
public class BallSpinConstraint : MonoBehaviour
{
    [Tooltip("평소(이상 충돌이 없을 때) 걸리는 기본 Y축 감쇠율. 0에 가까울수록 정상 커브가 오래 유지된다.")]
    [SerializeField] private float baselineDamping = 0.1f;

    [Tooltip("한 틱 사이에 Y축 각속도가 이 값(rad/s) 이상 급변하면 '이상 충돌'로 간주한다.")]
    [SerializeField] private float spikeThreshold = 3f;
    [Tooltip("이상 충돌 감지 직후 짧게 적용할 강한 Y축 감쇠율.")]
    [SerializeField] private float spikeDamping = 6f;
    [Tooltip("이상 충돌 감지 후 강한 감쇠가 유지되는 시간 (초).")]
    [SerializeField] private float spikeDampingDuration = 0.5f;

    private Rigidbody _rb;
    private bool _hasLanded;
    private float _prevAngularY;
    private float _spikeTimer;
    private bool _wasKinematic = true;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        _hasLanded = true;
    }

    private void FixedUpdate()
    {
        if (_rb.isKinematic)
        {
            _wasKinematic = true;
            return;
        }

        // kinematic으로 레인 위에 놓여있는 동안 이미 접촉해 _hasLanded가 먼저 켜져 있을 수 있다.
        // 그 상태에서 발사 순간 kinematic이 풀리며 BallLauncher가 큰 훅 스핀을 한 번에 넣어주면,
        // _prevAngularY(0)와의 차이가 스파이크 임계값을 넘어 "이상 충돌"로 오판되어 발사 스핀
        // 대부분이 첫 0.5초 안에 강하게 깎여나간다. kinematic에서 풀린 첫 틱은 기준값만
        // 다시 맞추고 스파이크 판정은 건너뛰어 이 오탐을 막는다.
        if (_wasKinematic || !_hasLanded)
        {
            _wasKinematic = false;
            _prevAngularY = _rb.angularVelocity.y;
            return;
        }

        Vector3 av = _rb.angularVelocity;

        // 직전 틱 이후 Y축 회전이 급변했으면(=물리 엔진이 이번 충돌로 스핀을 확 바꿔놨으면)
        // 이상 충돌로 보고 짧게 강한 감쇠 구간을 시작한다.
        if (Mathf.Abs(av.y - _prevAngularY) >= spikeThreshold)
            _spikeTimer = spikeDampingDuration;

        float damping = _spikeTimer > 0f ? spikeDamping : baselineDamping;
        av.y *= Mathf.Clamp01(1f - damping * Time.fixedDeltaTime);
        _rb.angularVelocity = av;

        _prevAngularY = av.y;
        if (_spikeTimer > 0f)
            _spikeTimer -= Time.fixedDeltaTime;
    }
}

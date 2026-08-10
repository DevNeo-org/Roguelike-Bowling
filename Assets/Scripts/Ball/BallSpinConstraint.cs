using UnityEngine;

// 게임플레이에 쓰이는 스핀은 Y축(BallLauncher, BallMagnusEffect 참고) 하나뿐이다.
// 거터 벽 등 설계에 없는 충돌에서 PhysX가 Y축 회전을 임의로 튀게 만들면
// 회전이 갑자기 끊기거나 비정상적으로 튀는 현상이 생긴다.
// X/Z축은 건드리지 않는다 — 굴러가며 자연스럽게 생기는 구름 회전(PhysX가
// 미끄러짐을 없애려고 붙이는 회전)까지 강제로 지워버리면 공이 항상
// "미끄러지는 중"으로 취급돼 마찰 감속이 비정상적으로 세지기 때문이다.
// 대신 Y축 회전에만 완만한 감쇠를 줘서 이상 스핀이 서서히 가라앉게 한다.
// 레인(또는 무엇이든)에 처음 닿기 전까지는 감쇠를 적용하지 않는다 — 공중에
// 떠 있는 동안 BallLauncher가 넣어준 의도된 발사 스핀까지 깎이면 안 되기 때문.
//
// Ball prefab에 부착.
[RequireComponent(typeof(Rigidbody))]
public class BallSpinConstraint : MonoBehaviour
{
    [Tooltip("Y축(스핀) 회전 감쇠율. 초당 이 비율만큼 회전 속도가 줄어든다 (0=감쇠 없음).")]
    [SerializeField] private float yAxisDamping = 2f;

    private Rigidbody _rb;
    private bool _hasLanded;

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
        if (_rb.isKinematic || !_hasLanded) return;

        Vector3 av = _rb.angularVelocity;
        av.y *= Mathf.Clamp01(1f - yAxisDamping * Time.fixedDeltaTime);
        _rb.angularVelocity = av;
    }
}

using UnityEngine;

// 공의 스핀(angularVelocity)에 의한 Magnus 횡력을 매 물리 프레임마다 적용한다.
// 결과: 스핀 방향에 따라 공의 궤적이 서서히 휜다 (볼링 훅 샷 효과).
//
// F_magnus = magnusCoeff × (ω × v)
//   ω = angularVelocity (Y축 스핀이 주요 성분)
//   v = linearVelocity
//
// Ball prefab에 부착.
[RequireComponent(typeof(Rigidbody))]
public class BallMagnusEffect : MonoBehaviour
{
    [Tooltip("Magnus 횡력 배율. 값이 클수록 궤적이 더 크게 휜다.")]
    [SerializeField] private float magnusCoeff = 0.3f;
    [Tooltip("횡력 방향이 반대로 느껴질 경우 체크.")]
    [SerializeField] private bool invertSpin = false;

    private Rigidbody _rb;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    // LaneFrictionZone과 동일하게 OnCollisionStay 사용:
    // 레인과 접촉 중일 때만 힘을 적용해 공중 낙하 시 간섭하지 않는다.
    private void OnCollisionStay(Collision collision)
    {
        // ω × v : Y축 스핀 + 전진 → 측면 힘 발생
        Vector3 force = Vector3.Cross(_rb.angularVelocity, _rb.linearVelocity) * magnusCoeff;

        if (invertSpin)
            force = -force;

        _rb.AddForce(force, ForceMode.Force);
    }
}

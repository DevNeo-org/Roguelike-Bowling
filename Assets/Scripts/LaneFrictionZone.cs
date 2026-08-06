using UnityEngine;

// Attach to a lane surface. While a ball (or any Rigidbody) stays in contact,
// applies an extra deceleration force to simulate rolling resistance that
// plain PhysicMaterial friction can't represent once the ball stops slipping.
[ExecuteAlways]
public class LaneFrictionZone : MonoBehaviour
{
    [Tooltip("Extra deceleration strength. Higher = slows moving objects down more over time, even after they start rolling without slipping.")]
    public float rollingResistanceCoefficient = 1.0f;

    [Tooltip("저항력 계산 기준 질량(기본 9파운드 공의 질량). ForceMode.Force는 최종 감속도가 질량에 반비례하므로, 이 기준 질량으로 힘을 고정해두면 더 무거운 공은 관성이 커서 덜 밀리고(잘 굴러감), 가벼운 공은 더 빨리 감속한다.")]
    public float referenceMass = 7f;

private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        if (horizontalVel.sqrMagnitude < 0.0001f) return;

        Vector3 decelForce = -horizontalVel.normalized * rollingResistanceCoefficient * referenceMass;
        rb.AddForce(decelForce, ForceMode.Force);
    }
}

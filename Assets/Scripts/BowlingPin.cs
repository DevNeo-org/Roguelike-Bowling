using UnityEngine;

// Attached to each bowling pin. IsDown reflects the pin's CURRENT state
// (angle from upright, or how far it's been shoved from its spot) rather
// than latching permanently the instant it ever crosses a threshold.
// PinDeckManager only reads IsDown after waiting for the pin to come to
// rest, so this checks the final settled state, not a momentary spike
// while the pin is still tumbling/wobbling from an impact.
public class BowlingPin : MonoBehaviour
{
    [SerializeField] private float fallAngleThreshold = 40f;
    [SerializeField] private float displacementThreshold = 0.08f;
    [SerializeField] private float gracePeriod = 0.3f;

    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private Rigidbody rb;
    private float graceTimer;

    public bool IsDown
    {
        get
        {
            float angle = Vector3.Angle(transform.up, Vector3.up);
            bool tippedOver = angle > fallAngleThreshold;

            bool shoveAway = false;
            if (graceTimer <= 0f)
            {
                float horizontalDist = Vector2.Distance(
                    new Vector2(transform.position.x, transform.position.z),
                    new Vector2(initialPosition.x, initialPosition.z));
                shoveAway = horizontalDist > displacementThreshold;
            }

            return tippedOver || shoveAway;
        }
    }

    private void Awake()
    {
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        graceTimer = gracePeriod;

        // 무게중심을 바닥 쪽으로 너무 낮추면(예전 -0.3, -0.15 모두) 밑동이 힌지처럼 고정된 채
        // 머리 쪽만 위로 들린 상태로 쓰러져 버린다 - 기하학적 중심에 가깝게 되돌려서
        // 쓰러졌을 때 몸통 전체가 바닥에 눕도록 했다.
        if (rb != null)
        {
            rb.centerOfMass = new Vector3(0f, 0f, 0f);

            // MeshCollider(convex)의 평평한 밑면이 레인 바닥의 평평한 면과 맞닿으면 접촉점이
            // 여러 개 잡혀서 PhysX가 매 프레임 미세하게 다르게 풀어내며 계속 떨리는 현상이 생긴다.
            // 이 값들은 프리팹에 저장되지 않는 런타임 전용 API라 여기서 매번 직접 설정해야 한다.
            rb.solverIterations = 14;
            rb.solverVelocityIterations = 6;
            rb.sleepThreshold = 0.02f;
        }
    }

    private void Update()
    {
        if (graceTimer > 0f)
        {
            graceTimer -= Time.deltaTime;

            if (graceTimer <= 0f && rb != null)
                rb.isKinematic = false;
        }
    }

    // 핀이 리셋되는 순간, 마침 근처에 남아있는 공(또는 아직 리셋 전인 다른 핀)과 겹치는 경우가
    // 있다 - 이때 물리 엔진이 다음 스텝에 겹침을 강제로 풀어내며(depenetration) 방금 세운 핀을
    // 그대로 다시 쓰러뜨려버린다. gracePeriod 동안 잠깐 kinematic으로 만들어 물리 개입 자체를
    // 막고, Update()에서 grace가 끝나면 다시 일반 다이나믹 상태로 되돌린다.
    // 스킬(리무브 등)이 서 있는 핀 하나를 즉시 쓰러뜨릴 때 사용.
    public void ForceKnockDown()
    {
        if (rb == null) return;

        rb.isKinematic = false;
        Vector3 randomDir = new Vector3(Random.Range(-0.5f, 0.5f), 0f, 1f).normalized;
        rb.AddForceAtPosition(randomDir * 6f, transform.position + Vector3.up * 0.3f, ForceMode.Impulse);
    }

    public void ResetPin()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        graceTimer = gracePeriod;

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }
    }
}

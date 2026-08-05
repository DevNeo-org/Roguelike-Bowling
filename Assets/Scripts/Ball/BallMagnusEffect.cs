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
    [Tooltip("커브 세기를 정규화하는 기준 질량(기본 9파운드 공의 질량). 공이 이보다 무거우면 관성이 커서 덜 휘고, 가벼우면 더 잘 휜다.")]
    [SerializeField] private float referenceMass = 7f;
    [Tooltip("횡력 방향이 반대로 느껴질 경우 체크.")]
    [SerializeField] private bool invertSpin = false;

    [Header("Debug Arrow")]
    [Tooltip("Game 뷰에서 공의 정면 방향 화살표 표시")]
    [SerializeField] private bool showForwardArrow = true;
    [SerializeField] private float arrowLength = 0.5f;
    [SerializeField] private Color arrowColor = Color.red;

    private Rigidbody _rb;
    private LineRenderer _line;

    private Material _lineMaterial;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _lineMaterial = new Material(Shader.Find("Sprites/Default"));

        _line = gameObject.AddComponent<LineRenderer>();
        _line.positionCount = 2;
        _line.startWidth = 0.05f;
        _line.endWidth = 0.05f;
        _line.useWorldSpace = true;
        _line.material = _lineMaterial;
        _line.startColor = arrowColor;
        _line.endColor   = arrowColor;
        _line.enabled = showForwardArrow;
    }

    private void OnDestroy()
    {
        if (_lineMaterial != null)
            Destroy(_lineMaterial);
    }

    // LaneFrictionZone과 동일하게 OnCollisionStay 사용:
    // 레인과 접촉 중일 때만 힘을 적용해 공중 낙하 시 간섭하지 않는다.
    private void OnCollisionStay(Collision collision)
    {
        // 공이 굴러가는 동안 자연스럽게 생기는 X축 회전(구름 스핀)까지 angularVelocity에
        // 섞여 있어서, 전체 벡터로 외적을 구하면 "구름 스핀 × 전진속도"가 의도치 않은
        // 수직(Y축) 힘을 만들어내 공이 계속 튀어 오르는 문제가 있었다.
        // 커브(훅)에 실제로 의미가 있는 건 Y축(좌우 회전) 스핀뿐이므로 그 성분만 분리해서 쓴다.
        float curveSpin = _rb.angularVelocity.y;

        // 공이 무거울수록 같은 스핀이라도 궤적을 덜 휘게(관성이 크므로) 만든다.
        // referenceMass(기본 9파운드)를 기준으로 정규화 - 기준 질량인 공은 기존과 동일하게 동작한다.
        float massRatio = referenceMass / Mathf.Max(_rb.mass, 0.01f);
        Vector3 force = Vector3.Cross(Vector3.up * curveSpin, _rb.linearVelocity) * magnusCoeff * massRatio;

        if (invertSpin)
            force = -force;

        _rb.AddForce(force, ForceMode.Acceleration);
    }

    private void Update()
    {
        if (_line == null) return;
        _line.enabled = showForwardArrow;
        if (!showForwardArrow) return;

        Vector3 origin = transform.position;
        _line.SetPosition(0, origin);
        _line.SetPosition(1, origin + transform.forward * arrowLength);
    }
}

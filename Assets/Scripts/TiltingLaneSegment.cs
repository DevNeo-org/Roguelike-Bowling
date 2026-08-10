using UnityEngine;

// 레인 형태 변형 장애물: 레인의 일부 구간이 주기적으로 좌우로 기울어져서,
// 공의 굴러가는 경로 자체가 휘어지게 만든다. 장애물을 얹는 게 아니라
// 레인 표면 자체의 형태(기울기)가 바뀐다는 점에서 다른 장애물들과 다르다.
public class TiltingLaneSegment : MonoBehaviour
{
    public float maxTiltAngle = 12f;
    public float tiltSpeed = 0.8f;

    private float phase;

    private void Awake()
    {
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        phase += tiltSpeed * Time.deltaTime;
        float angle = Mathf.Sin(phase) * maxTiltAngle;
        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}

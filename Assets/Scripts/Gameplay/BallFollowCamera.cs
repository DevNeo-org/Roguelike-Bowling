using UnityEngine;

// 투구 후 카메라가 볼링공을 Z축으로 그대로 따라가되(보간 없이 즉시 반응),
// 공이 카메라 화면에 보이도록 followOffsetZ만큼 뒤에서 따라간다.
// 볼링 핀 앞쪽(pinFrontZ)을 넘어서지는 않는다 - 결과(핀이 쓰러지는 모습)를 보여줘야 하기 때문.
// 투구가 판정되면 다음 투구를 위해 즉시 원래 자리로 되돌아간다.
public class BallFollowCamera : MonoBehaviour
{
    [SerializeField] private string ballTag = "Ball";
    [SerializeField] private float pinFrontZ = 16f;
    [SerializeField] private float followOffsetZ = 3f;

    private float startZ;
    private Transform trackedBall;
    private int judgedBallId = int.MinValue; // 이미 판정된 공은 (아직 씬에 남아있어도) 다시 추적하지 않는다

    private void Start()
    {
        startZ = transform.position.z;

        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged += HandleThrowJudged;
    }

    private void OnDestroy()
    {
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnThrowJudged -= HandleThrowJudged;
    }

    // 물리적으로 여러 레인을 순서대로 이동하는 씬(예: MountainLaneProgress)이 레인 전환 후 호출해서
    // 기준 Z(원위치)와 핀 앞 한계선을 새 레인 위치로 갱신한다.
    public void SetLaneOrigin(float newStartZ, float newPinFrontZ)
    {
        startZ = newStartZ;
        pinFrontZ = newPinFrontZ;
    }

    private void HandleThrowJudged()
    {
        // 판정된 공은 (파괴되기 전까지 잠시 씬에 남아있어도) 다시 쫓아가지 않도록 기억해둔다.
        if (trackedBall != null)
            judgedBallId = trackedBall.gameObject.GetInstanceID();

        trackedBall = null;

        // 다음 투구가 항상 같은 위치에서 정확히 시작하도록, 판정 즉시 원위치로 스냅한다.
        Vector3 pos = transform.position;
        pos.z = startZ;
        transform.position = pos;
    }

    private void LateUpdate()
    {
        if (trackedBall == null)
        {
            // 판정된 이전 공이 아직 파괴되지 않고 씬에 남아있을 수 있으므로(같은 태그를 공유),
            // 태그를 가진 오브젝트를 전부 훑어서 그 공이 아닌 것(=새로 던진 공)을 찾는다.
            GameObject[] candidates = GameObject.FindGameObjectsWithTag(ballTag);
            foreach (var candidate in candidates)
            {
                if (candidate.GetInstanceID() == judgedBallId)
                    continue;

                trackedBall = candidate.transform;
                break;
            }
        }

        if (trackedBall == null)
            return;

        // 보간 없이 공을 그대로 따라가되, 공이 화면에 보이도록 followOffsetZ만큼 뒤에서 따라가고,
        // 핀 앞쪽을 넘지 않도록 고정한다.
        Vector3 pos = transform.position;
        pos.z = Mathf.Clamp(trackedBall.position.z - followOffsetZ, startZ, pinFrontZ);
        transform.position = pos;
    }
}

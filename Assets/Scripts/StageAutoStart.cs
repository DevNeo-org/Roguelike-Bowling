using UnityEngine;

// 각 맵 씬(Field/Mountain/Desert/Ice/Volcano)에 붙여둔다. 씬이 로드되자마자
// (1) 이 맵의 1번 레인 핀 키를 PinDeckManager에 지정하고 - 안 하면 산 1레인
// 전용 키("Basic")로 고정돼있어서 다른 맵에서는 핀을 아예 못 찾는다 -
// (2) StageManager를 자동으로 시작시켜서 점수/프레임 UI가 항상 정상 작동하게 한다.
public class StageAutoStart : MonoBehaviour
{
    [Tooltip("이 맵의 실제 플레이 레인(보통 1번 레인) 핀 이름에 들어가는 키. 예: Basic, Classic1, Desert1, Ice1, Volcano1")]
    [SerializeField] private string laneKey = "Basic";

    private void Start()
    {
        if (PinDeckManager.Instance != null)
        {
            PinDeckManager.Instance.LaneOverrideKey = laneKey;
            Debug.Log($"[StageAutoStart] PinDeckManager 레인 키 설정: {laneKey}");
        }

        if (StageManager.Instance != null)
        {
            StageManager.Instance.StartFromStageOne();
            Debug.Log("[StageAutoStart] 씬 진입 시 스테이지 자동 시작");
        }
        else
        {
            Debug.LogWarning("[StageAutoStart] StageManager.Instance가 없어서 자동 시작 실패");
        }
    }
}

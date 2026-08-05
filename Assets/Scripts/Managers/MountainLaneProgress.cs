using UnityEngine;

// Scene_Stage_Mountain 전용: 산길을 따라 순서대로 놓인 5개의 물리적 레인을, 레인당 2프레임씩
// 자동으로 진행시킨다. BowlingScoreManager의 프레임이 2의 배수로 넘어갈 때마다 다음 레인으로 -
// 공 스폰 위치를 옮기고 카메라를 부드럽게 이동시키며, PinDeckManager가 다음 레인의 핀을 보게 한다.
public class MountainLaneProgress : MonoBehaviour
{
    [SerializeField] private int laneCount = 5;
    [SerializeField] private int framesPerLane = 2;
    [SerializeField] private float laneLength = 34.1f;
    [SerializeField] private float transitionDuration = 0.9f;

    [Header("Lane 1 기준 위치 (여기서부터 laneLength만큼씩 밀려서 계산됨)")]
    [SerializeField] private float lane1SpawnZ = 0.317f;
    [SerializeField] private float lane1CameraStartZ = -3f;
    [SerializeField] private float lane1PinFrontZ = 16f;

    [SerializeField] private Transform ballSpawner;
    [SerializeField] private BallFollowCamera followCamera;

    private int currentLane;
    private int lastHandledFrame = -1;

    private void Start()
    {
        currentLane = 0; // ActivateLane(1)에서 1로 올라가도록
        ActivateLane(1, instant: true);

        if (BowlingScoreManager.Instance != null)
            BowlingScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDestroy()
    {
        if (BowlingScoreManager.Instance != null)
            BowlingScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged()
    {
        int frame = BowlingScoreManager.Instance.CurrentFrame;
        if (frame == lastHandledFrame) return;
        lastHandledFrame = frame;

        if (frame > 0 && frame % framesPerLane == 0 && currentLane < laneCount)
            ActivateLane(currentLane + 1, instant: false);
    }

    private void ActivateLane(int laneNumber, bool instant)
    {
        currentLane = laneNumber;
        float offset = (laneNumber - 1) * laneLength;

        string key = laneNumber == 1 ? "Basic" : "MLane" + laneNumber;
        if (PinDeckManager.Instance != null)
        {
            PinDeckManager.Instance.LaneOverrideKey = key;
            PinDeckManager.Instance.ResetPins();
        }

        if (ballSpawner != null)
        {
            Vector3 pos = ballSpawner.position;
            pos.z = lane1SpawnZ + offset;
            ballSpawner.position = pos;
        }

        float targetCamZ = lane1CameraStartZ + offset;
        float targetPinFrontZ = lane1PinFrontZ + offset;

        if (followCamera == null) return;

        if (instant)
        {
            Vector3 camPos = followCamera.transform.position;
            camPos.z = targetCamZ;
            followCamera.transform.position = camPos;
            followCamera.SetLaneOrigin(targetCamZ, targetPinFrontZ);
        }
        else
        {
            StartCoroutine(MoveCameraRoutine(targetCamZ, targetPinFrontZ, transitionDuration));
        }

        Debug.Log($"[MountainLaneProgress] 레인 {laneNumber} 활성화 (핀 키: {key})");
    }

    private System.Collections.IEnumerator MoveCameraRoutine(float targetZ, float targetPinFrontZ, float duration)
    {
        followCamera.enabled = false;

        Transform camT = followCamera.transform;
        float fromZ = camT.position.z;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            Vector3 pos = camT.position;
            pos.z = Mathf.Lerp(fromZ, targetZ, t);
            camT.position = pos;
            yield return null;
        }

        Vector3 finalPos = camT.position;
        finalPos.z = targetZ;
        camT.position = finalPos;

        followCamera.SetLaneOrigin(targetZ, targetPinFrontZ);
        followCamera.enabled = true;
    }
}

using System.Collections.Generic;
using UnityEngine;

// Tracks the 10 pins belonging to whichever lane is currently active,
// counts newly knocked-down pins after each throw, and reports the
// result to BowlingScoreManager. Resets the pins whenever the score
// manager advances to a new frame.
public class PinDeckManager : MonoBehaviour
{
    public static PinDeckManager Instance { get; private set; }

    // 투구 하나의 판정(핀 집계 + 점수 반영)이 끝났을 때 발생. 다음 투구를 언제 허용해도
    // 되는지 알아야 하는 입력 컨트롤러(TestThrowController 등)가 구독한다.
    public event System.Action OnThrowJudged;

    [SerializeField] private string mapRootName = "Map";

    private int lastKnownDownCount;
    private int lastKnownFrame;
    private readonly HashSet<int> processedBallIds = new HashSet<int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (BowlingScoreManager.Instance != null)
        {
            BowlingScoreManager.Instance.OnScoreChanged += HandleScoreChanged;
            lastKnownFrame = BowlingScoreManager.Instance.CurrentFrame;
        }
    }

    private void OnDestroy()
    {
        if (BowlingScoreManager.Instance != null)
            BowlingScoreManager.Instance.OnScoreChanged -= HandleScoreChanged;
    }

    private void HandleScoreChanged()
    {
        int frame = BowlingScoreManager.Instance.CurrentFrame;
        if (frame != lastKnownFrame)
        {
            lastKnownFrame = frame;
            ResetPins();
        }
    }

    // Call this when a thrown ball has finished its run (e.g. reached the
    // end-of-lane trigger zone or fallen into a gutter). Waits for the
    // pins to actually settle before tallying, so pins still mid-fall
    // aren't missed.
    public void OnBallFinished(GameObject ball)
    {
        if (ball == null) return;

        int id = ball.GetInstanceID();
        if (processedBallIds.Contains(id))
        {
            Debug.Log("[PinDeckManager] 이미 처리된 공이라 무시: " + ball.name);
            return;
        }
        processedBallIds.Add(id);

        // Let the ball keep rolling/falling naturally (e.g. down the gutter)
        // instead of freezing it in place - it just gets cleaned up later.
        StartCoroutine(EvaluateAfterSettle(ball));
    }

    private System.Collections.IEnumerator EvaluateAfterSettle(GameObject ball)
    {
        List<BowlingPin> pins = GetActiveLanePins();

        float maxWait = 4f;
        float elapsed = 0f;
        float settleCheckInterval = 0.1f;
        float stillTimeRequired = 0.5f;
        float stillTimer = 0f;

        while (elapsed < maxWait)
        {
            bool allSettled = true;
            foreach (BowlingPin p in pins)
            {
                Rigidbody rb = p.GetComponent<Rigidbody>();
                if (rb != null && !rb.IsSleeping() &&
                    (rb.linearVelocity.magnitude > 0.05f || rb.angularVelocity.magnitude > 0.1f))
                {
                    allSettled = false;
                    break;
                }
            }

            if (allSettled)
            {
                stillTimer += settleCheckInterval;
                if (stillTimer >= stillTimeRequired) break;
            }
            else
            {
                stillTimer = 0f;
            }

            yield return new WaitForSeconds(settleCheckInterval);
            elapsed += settleCheckInterval;
        }

        int downCount = 0;
        string pinStates = "";
        foreach (BowlingPin p in pins)
        {
            if (p.IsDown) downCount++;
            pinStates += p.name + "=" + (p.IsDown ? "DOWN" : "up") + " ";
        }

        int newlyDown = Mathf.Max(0, downCount - lastKnownDownCount);
        lastKnownDownCount = downCount;

        bool frameChanged = false;

        // BowlingScoreManager (Map 씬 자체 채점) - 있으면 그대로 갱신.
        if (BowlingScoreManager.Instance != null)
        {
            int frameBefore = BowlingScoreManager.Instance.CurrentFrame;
            BowlingScoreManager.Instance.RecordRoll(newlyDown);
            int frameAfter = BowlingScoreManager.Instance.CurrentFrame;
            frameChanged = frameChanged || (frameAfter != frameBefore);
        }

        // StageManager (스테이지 목표 점수 진행) - 있으면 실제 투구 결과를 전달.
        if (StageManager.Instance != null)
        {
            int frameBefore = StageManager.Instance.CurrentFrameNumber;
            StageManager.Instance.RecordThrow(newlyDown);
            int frameAfter = StageManager.Instance.CurrentFrameNumber;
            frameChanged = frameChanged || (frameAfter != frameBefore);
        }

        Debug.Log("[PinDeckManager] 투구 종료: 이번에 " + newlyDown + "개 쓰러짐 (총 " + downCount + "개 다운, 대기 " + elapsed.ToString("F1") + "s)");
        Debug.Log("[PinDeckManager] 핀 상태: " + pinStates);

        OnThrowJudged?.Invoke();

        // If the frame is still in progress (no strike, more throws to come
        // in this frame), sweep away the fallen pins so only the standing
        // ones remain for the next throw - like a real pin sweep.
        if (!frameChanged)
        {
            SweepFallenPins(pins);
        }

        if (ball != null)
            StartCoroutine(DestroyWhenBallIsGone(ball));
    }

    // Scoring happens as soon as the pins settle, but the ball itself
    // (e.g. one that missed everything and is rolling down the gutter)
    // should keep visually rolling until it's actually off the play area
    // instead of vanishing the instant it's scored.
    private System.Collections.IEnumerator DestroyWhenBallIsGone(GameObject ball)
    {
        float maxExtraWait = 3f;
        float elapsed = 0f;

        while (ball != null && elapsed < maxExtraWait)
        {
            Rigidbody rb = ball.GetComponent<Rigidbody>();
            bool stopped = rb == null || (rb.linearVelocity.magnitude < 0.05f && rb.angularVelocity.magnitude < 0.1f);
            bool fellAway = ball.transform.position.y < -2f;

            if (stopped || fellAway)
                break;

            yield return null;
            elapsed += Time.deltaTime;
        }

        if (ball != null)
            Destroy(ball);
    }

    private void SweepFallenPins(List<BowlingPin> pins)
    {
        int swept = 0;
        foreach (BowlingPin p in pins)
        {
            if (p.IsDown && p.gameObject.activeSelf)
            {
                p.gameObject.SetActive(false);
                swept++;
            }
        }
        Debug.Log("[PinDeckManager] 넘어진 핀 " + swept + "개 정리 (남은 핀으로 2구 진행)");
    }

    public void ResetPins()
    {
        lastKnownDownCount = 0;
        foreach (BowlingPin p in GetActiveLanePins())
        {
            p.gameObject.SetActive(true);
            p.ResetPin();
        }
    }

    private List<BowlingPin> GetActiveLanePins()
    {
        List<BowlingPin> result = new List<BowlingPin>();

        string laneKey = LaneStageManager.Instance != null ? LaneStageManager.Instance.ActiveLaneKey : "Basic";
        GameObject mapRoot = GameObject.Find(mapRootName);
        if (mapRoot == null) return result;

        BowlingPin[] allPins = mapRoot.GetComponentsInChildren<BowlingPin>(true);
        foreach (BowlingPin p in allPins)
        {
            if (p.name.Contains("_" + laneKey))
                result.Add(p);
        }
        return result;
    }
}

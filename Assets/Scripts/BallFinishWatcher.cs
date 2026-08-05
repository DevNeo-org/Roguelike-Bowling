using UnityEngine;

// Fallback watcher attached to each thrown ball. Normally the throw is
// judged when the ball crosses ThrowEndZone, but if the ball stalls out
// on the lane (hits pins and stops, runs out of momentum, gets stuck,
// etc.) it may never reach that trigger. This watches the ball itself and
// forces the throw to be judged once it comes to rest or after a timeout,
// regardless of position. PinDeckManager's duplicate-guard makes it safe
// even if ThrowEndZone also fires.
public class BallFinishWatcher : MonoBehaviour
{
    // BallResetter.Init()에서 AddComponent로 추가되므로 인스펙터 설정 불가 — 상수로 관리
    private const float StopSpeedThreshold = 0.15f;
    private const float StopTimeRequired   = 0.6f;
    private const float MaxLifetime        = 6f;

    private Rigidbody rb;
    private float stopTimer;
    private float age;
    private bool finished;
    private bool hasLaunched;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    /// <summary>BallResetter.SetLaunched()에서 호출 — 발사 후부터 카운트 시작.</summary>
    public void SetLaunched()
    {
        hasLaunched = true;
    }

    private void Update()
    {
        if (finished || !hasLaunched) return;

        age += Time.deltaTime;

        if (rb != null && !rb.isKinematic)
        {
            float speed = rb.linearVelocity.magnitude + rb.angularVelocity.magnitude;
            if (speed < StopSpeedThreshold)
                stopTimer += Time.deltaTime;
            else
                stopTimer = 0f;
        }

        if (stopTimer >= StopTimeRequired || age >= MaxLifetime)
        {
            Finish();
        }
    }

    private void Finish()
    {
        finished = true;
        if (PinDeckManager.Instance != null)
            PinDeckManager.Instance.OnBallFinished(gameObject);
    }
}

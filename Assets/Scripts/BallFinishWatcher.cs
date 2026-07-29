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
    [SerializeField] private float stopSpeedThreshold = 0.15f;
    [SerializeField] private float stopTimeRequired = 0.6f;
    [SerializeField] private float maxLifetime = 6f;

    private Rigidbody rb;
    private float stopTimer;
    private float age;
    private bool finished;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if (finished) return;

        age += Time.deltaTime;

        if (rb != null && !rb.isKinematic)
        {
            float speed = rb.linearVelocity.magnitude + rb.angularVelocity.magnitude;
            if (speed < stopSpeedThreshold)
                stopTimer += Time.deltaTime;
            else
                stopTimer = 0f;
        }

        if (stopTimer >= stopTimeRequired || age >= maxLifetime)
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

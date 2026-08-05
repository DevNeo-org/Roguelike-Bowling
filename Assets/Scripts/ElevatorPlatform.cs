using UnityEngine;

// A platform that cycles between a bottom and top position, carrying
// anything resting on it (e.g. the bowling ball) between lane tiers.
public class ElevatorPlatform : MonoBehaviour
{
    public float bottomY = 0f;
    public float topY = 2f;
    public float travelTime = 1.5f;
    public float waitTime = 1.0f;

    private Rigidbody rb;
    private float timer;
    private enum State { WaitingAtBottom, MovingUp, WaitingAtTop, MovingDown }
    private State state = State.WaitingAtBottom;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
    }

    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        Vector3 pos = rb.position;

        switch (state)
        {
            case State.WaitingAtBottom:
                if (timer >= waitTime) { timer = 0f; state = State.MovingUp; }
                break;
            case State.MovingUp:
                {
                    float t = Mathf.Clamp01(timer / travelTime);
                    float y = Mathf.Lerp(bottomY, topY, t);
                    rb.MovePosition(new Vector3(pos.x, y, pos.z));
                    if (t >= 1f) { timer = 0f; state = State.WaitingAtTop; }
                    break;
                }
            case State.WaitingAtTop:
                if (timer >= waitTime) { timer = 0f; state = State.MovingDown; }
                break;
            case State.MovingDown:
                {
                    float t = Mathf.Clamp01(timer / travelTime);
                    float y = Mathf.Lerp(topY, bottomY, t);
                    rb.MovePosition(new Vector3(pos.x, y, pos.z));
                    if (t >= 1f) { timer = 0f; state = State.WaitingAtBottom; }
                    break;
                }
        }
    }
}

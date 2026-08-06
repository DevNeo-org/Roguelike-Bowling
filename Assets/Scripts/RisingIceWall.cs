using UnityEngine;

// Obstacle: 솟아오르는 얼음벽 (rising ice wall).
// Cycles between hidden (sunk below the lane, ball rolls right over) and
// risen (solid wall blocking the lane) - a timing gate rather than a
// push/slip/crack effect.
public class RisingIceWall : MonoBehaviour
{
    public float downY = -0.5f;
    public float upY = 0.4f;
    public float riseSpeed = 3f;
    public float waitDownTime = 1.5f;
    public float waitUpTime = 1.2f;

    private enum State { Down, Rising, Up, Sinking }
    private State state = State.Down;
    private float timer;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, downY, p.z);
    }

    private void FixedUpdate()
    {
        Vector3 pos = rb.position;
        timer += Time.fixedDeltaTime;

        switch (state)
        {
            case State.Down:
                if (timer >= waitDownTime) { timer = 0f; state = State.Rising; }
                break;

            case State.Rising:
                {
                    float newY = Mathf.MoveTowards(pos.y, upY, riseSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(new Vector3(pos.x, newY, pos.z));
                    if (Mathf.Approximately(newY, upY)) { timer = 0f; state = State.Up; }
                    break;
                }

            case State.Up:
                if (timer >= waitUpTime) { timer = 0f; state = State.Sinking; }
                break;

            case State.Sinking:
                {
                    float newY = Mathf.MoveTowards(pos.y, downY, riseSpeed * Time.fixedDeltaTime);
                    rb.MovePosition(new Vector3(pos.x, newY, pos.z));
                    if (Mathf.Approximately(newY, downY)) { timer = 0f; state = State.Down; }
                    break;
                }
        }
    }
}

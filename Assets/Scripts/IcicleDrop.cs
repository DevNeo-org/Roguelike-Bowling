using UnityEngine;

// Obstacle: 고드름 낙하 (falling icicle).
// Hangs above the lane and periodically drops straight down. Each cycle,
// after resetting to the ceiling, it also jumps to a random X/Z spot
// within the lane and waits a random amount of time before falling again -
// so both where and when it drops keeps changing.
public class IcicleDrop : MonoBehaviour
{
    public float ceilingY = 3.5f;
    public float floorY = 0.2f;
    public float fallSpeed = 6f;
    public float minWaitAtTopTime = 0.8f;
    public float maxWaitAtTopTime = 2.6f;
    public float waitAtBottomTime = 0.6f;
    public float impactForce = 4f;

    [Header("Random spot within the lane")]
    public float laneCenterX = -3f;
    public float xRange = 0.35f;
    public float zMin = 3f;
    public float zMax = 14f;

    private enum State { WaitingAtTop, Falling, WaitingAtBottom, Resetting }
    private State state = State.WaitingAtTop;
    private float timer;
    private float currentWaitAtTopTime;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        RelocateToRandomSpot();
        currentWaitAtTopTime = Random.Range(minWaitAtTopTime, maxWaitAtTopTime);
    }

    private void RelocateToRandomSpot()
    {
        float x = laneCenterX + Random.Range(-xRange, xRange);
        float z = Random.Range(zMin, zMax);
        rb.position = new Vector3(x, ceilingY, z);
        transform.position = new Vector3(x, ceilingY, z);
    }

    private void FixedUpdate()
    {
        Vector3 pos = rb.position;
        timer += Time.fixedDeltaTime;

        switch (state)
        {
            case State.WaitingAtTop:
                if (timer >= currentWaitAtTopTime) { timer = 0f; state = State.Falling; }
                break;

            case State.Falling:
                {
                    float newY = pos.y - fallSpeed * Time.fixedDeltaTime;
                    if (newY <= floorY)
                    {
                        newY = floorY;
                        state = State.WaitingAtBottom;
                        timer = 0f;
                    }
                    rb.MovePosition(new Vector3(pos.x, newY, pos.z));
                    break;
                }

            case State.WaitingAtBottom:
                if (timer >= waitAtBottomTime) { timer = 0f; state = State.Resetting; }
                break;

            case State.Resetting:
                RelocateToRandomSpot();
                currentWaitAtTopTime = Random.Range(minWaitAtTopTime, maxWaitAtTopTime);
                state = State.WaitingAtTop;
                timer = 0f;
                break;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (state != State.Falling) return;
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;

        Vector3 knock = new Vector3(Random.Range(-1f, 1f), 0.3f, Random.Range(-0.5f, 0.5f)).normalized * impactForce;
        ballRb.AddForce(knock, ForceMode.Impulse);

        Debug.Log("[IcicleDrop] " + collision.gameObject.name + " got hit by a falling icicle");
    }
}

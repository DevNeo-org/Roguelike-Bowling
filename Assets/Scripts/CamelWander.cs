using UnityEngine;

// Obstacle: 낙타 (camel).
// Wanders to random spots within the lane instead of following a fixed
// patrol path - picks a random point, walks there, pauses, then picks a
// new random point, forever. Pushes the ball hard on contact.
public class CamelWander : MonoBehaviour
{
    public float laneCenterX = -3f;
    public float xRange = 0.4f;
    public float zMin = 5f;
    public float zMax = 15f;
    public float moveSpeed = 1.5f;
    public float minWaitTime = 0.5f;
    public float maxWaitTime = 2f;
    public float pushForce = 6f;

    private Rigidbody rb;
    private Vector3 target;
    private bool waiting;
    private float waitTimer;
    private float currentWaitTime;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        PickNewTarget();
    }

    private void PickNewTarget()
    {
        float x = laneCenterX + Random.Range(-xRange, xRange);
        float z = Random.Range(zMin, zMax);
        target = new Vector3(x, rb.position.y, z);
    }

    private void FixedUpdate()
    {
        if (waiting)
        {
            waitTimer += Time.fixedDeltaTime;
            if (waitTimer >= currentWaitTime)
            {
                waiting = false;
                waitTimer = 0f;
                PickNewTarget();
            }
            return;
        }

        Vector3 pos = rb.position;
        Vector3 toTarget = target - pos;
        float step = moveSpeed * Time.fixedDeltaTime;

        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(target);
            waiting = true;
            currentWaitTime = Random.Range(minWaitTime, maxWaitTime);
        }
        else
        {
            rb.MovePosition(pos + toTarget.normalized * step);
            transform.rotation = Quaternion.LookRotation(new Vector3(toTarget.x, 0f, toTarget.z));
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;

        Vector3 push = new Vector3(Random.Range(-1f, 1f), 0.3f, Random.Range(-1f, 1f)).normalized * pushForce;
        ballRb.AddForce(push, ForceMode.Impulse);

        Debug.Log("[CamelWander] " + collision.gameObject.name + " bumped into a camel");
    }
}

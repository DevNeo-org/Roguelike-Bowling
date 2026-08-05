using UnityEngine;

// Obstacle: 제설차 (snowplow).
// Instead of a simple back-and-forth line, it patrols a rectangular loop
// around the lane (left edge -> forward -> right edge -> back -> repeat),
// so it feels like it's actually circling the lane rather than just
// sliding along one straight track.
public class SnowplowPatrol : MonoBehaviour
{
    public float laneCenterX = -3f;
    public float sideOffset = 0.45f; // how close to each gutter edge it swings
    public float zMin = 5f;
    public float zMax = 15f;
    public float moveSpeed = 2.5f;
    public float pushForce = 6f;

    private Rigidbody rb;
    private Vector3[] waypoints;
    private int currentWaypoint;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;

        float y = rb.position.y;
        waypoints = new Vector3[]
        {
            new Vector3(laneCenterX - sideOffset, y, zMin), // 왼쪽 뒤
            new Vector3(laneCenterX - sideOffset, y, zMax), // 왼쪽 앞
            new Vector3(laneCenterX + sideOffset, y, zMax), // 오른쪽 앞
            new Vector3(laneCenterX + sideOffset, y, zMin), // 오른쪽 뒤
        };

        // 가장 가까운 웨이포인트부터 시작하도록.
        currentWaypoint = 0;
        rb.position = waypoints[0];
    }

    private void FixedUpdate()
    {
        Vector3 target = waypoints[currentWaypoint];
        Vector3 pos = rb.position;
        Vector3 toTarget = target - pos;

        float step = moveSpeed * Time.fixedDeltaTime;
        if (toTarget.magnitude <= step)
        {
            rb.MovePosition(target);
            currentWaypoint = (currentWaypoint + 1) % waypoints.Length;
        }
        else
        {
            rb.MovePosition(pos + toTarget.normalized * step);
        }

        // 진행 방향을 바라보도록 회전시켜서 순환하는 느낌을 더 살린다.
        if (toTarget.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(new Vector3(toTarget.x, 0f, toTarget.z));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;

        Vector3 moveDir = (waypoints[currentWaypoint] - rb.position).normalized;
        Vector3 push = (moveDir + Vector3.up * 0.2f) * pushForce;
        ballRb.AddForce(push, ForceMode.Impulse);

        Debug.Log("[SnowplowPatrol] " + collision.gameObject.name + " got shoved by the snowplow");
    }
}

using UnityEngine;

// Obstacle: 돌부리 (protruding rock/root).
// A small solid bump. On collision, the ball keeps most of its forward
// momentum but gets popped upward - a light "hop" over the bump,
// rather than getting stopped dead by the collision solver.
// Single use: disappears after the first hit.
[RequireComponent(typeof(Collider))]
public class ObstacleRock : MonoBehaviour
{
    [Tooltip("Fraction of the incoming forward speed kept after the hop (0~1).")]
    [Range(0f, 1f)]
    public float forwardKeepRatio = 0.85f;

    [Tooltip("Upward hop speed given to the ball.")]
    public float popUpSpeed = 2.5f;

    [Tooltip("Minimum incoming speed required to trigger the hop.")]
    public float minSpeedToTrigger = 0.5f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        // relativeVelocity reflects the impact speed before the solver
        // has a chance to kill it via friction/restitution.
        Vector3 impactVel = collision.relativeVelocity;
        Vector3 horizontal = new Vector3(impactVel.x, 0f, impactVel.z);
        if (horizontal.magnitude < minSpeedToTrigger) return;

        Vector3 forwardDir = horizontal.normalized;
        float speed = horizontal.magnitude * forwardKeepRatio;

        // Directly set the velocity so the bounce feels consistent,
        // regardless of the default collider's friction/restitution.
        rb.linearVelocity = forwardDir * speed + Vector3.up * popUpSpeed;

        Debug.Log("[ObstacleRock] " + collision.gameObject.name + " hopped over the rock");

        // Obstacle is consumed after a single use.
        gameObject.SetActive(false);
    }
}

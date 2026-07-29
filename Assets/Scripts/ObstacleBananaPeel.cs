using UnityEngine;

// Obstacle: banana peel.
// When a ball rolls over this trigger zone, it gets a lateral impulse
// simulating a slip, plus a temporary friction reduction.
[RequireComponent(typeof(Collider))]
public class ObstacleBananaPeel : MonoBehaviour
{
    [Tooltip("Sideways force applied to the ball when it slips (random left/right).")]
    public float slipForce = 6f;

    [Tooltip("Minimum ball speed required to trigger the slip.")]
    public float minSpeedToTrigger = 1f;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

private void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        if (horizontalVel.magnitude < minSpeedToTrigger) return;

        float side = Random.value < 0.5f ? -1f : 1f;
        Vector3 slip = transform.right * side * slipForce;
        rb.AddForce(slip, ForceMode.Impulse);

        Debug.Log("[ObstacleBananaPeel] " + other.name + " slipped! side=" + side);

        // Obstacle is consumed after a single use.
        gameObject.SetActive(false);
    }
}

using UnityEngine;

// Obstacle: 오로라 자기장 (magnetic pull zone).
// Unlike every push-based obstacle so far, this one PULLS the ball
// sideways toward a fixed point while it's inside the zone - the
// opposite kind of force, so it reads as a distinct hazard.
public class MagneticPullZone : MonoBehaviour
{
    public float pullStrength = 4f;
    public Vector3 pullTargetOffset = new Vector3(0.5f, 0f, 0f);

private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 targetPoint = transform.position + pullTargetOffset;
        Vector3 toTarget = targetPoint - other.transform.position;
        toTarget.y = 0f;

        // Direct velocity nudge every physics step - a continuous Force was
        // too weak to feel against the ball's own momentum/mass.
        rb.linearVelocity += toTarget.normalized * pullStrength * Time.fixedDeltaTime * 10f;
    }
}

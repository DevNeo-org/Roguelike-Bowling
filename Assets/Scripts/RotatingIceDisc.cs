using UnityEngine;

// Obstacle: 회전하는 빙판 (rotating ice disc).
// A disc that spins in place on the lane. Any ball on top of it gets
// pushed sideways (tangential to the spin) instead of a simple straight
// push - it curves the ball's path rather than just knocking it around.
public class RotatingIceDisc : MonoBehaviour
{
    public float spinSpeed = 90f; // degrees per second, visual spin
    public float pushStrength = 3f;

    private void Update()
    {
        transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.Self);
    }

private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 toBall = other.transform.position - transform.position;
        toBall.y = 0f;

        Vector3 tangent = Vector3.Cross(Vector3.up, toBall).normalized;
        float spinDir = Mathf.Sign(spinSpeed);

        // Directly nudge velocity every physics step instead of a weak
        // continuous Force (which was too subtle to notice against the
        // ball's own momentum/mass).
        rb.linearVelocity += tangent * spinDir * pushStrength * Time.fixedDeltaTime * 10f;
    }
}

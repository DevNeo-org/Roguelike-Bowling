using UnityEngine;

// Lane gimmick: trampoline. Requires a minimum incoming speed to cross;
// balls that are too slow get bounced back instead of continuing.
[ExecuteAlways]
public class TrampolineGate : MonoBehaviour
{
    [Tooltip("Minimum forward speed (m/s) required to bounce across.")]
    public float requiredSpeed = 4f;

    [Tooltip("Extra upward+backward force applied when rejecting a slow ball.")]
    public float rejectForce = 6f;

    private void OnCollisionEnter(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);

        if (horizontalVel.magnitude < requiredSpeed)
        {
            Vector3 reject = (Vector3.up - transform.forward) * rejectForce;
            rb.AddForce(reject, ForceMode.Impulse);
            Debug.Log("[TrampolineGate] " + rb.name + " was too slow, bounced back!");
        }
    }
}

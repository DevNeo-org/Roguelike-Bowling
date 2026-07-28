using UnityEngine;

// Attach to a lane surface. While a ball (or any Rigidbody) stays in contact,
// applies an extra deceleration force to simulate rolling resistance that
// plain PhysicMaterial friction can't represent once the ball stops slipping.
[ExecuteAlways]
public class LaneFrictionZone : MonoBehaviour
{
    [Tooltip("Extra deceleration strength. Higher = slows moving objects down more over time, even after they start rolling without slipping.")]
    public float rollingResistanceCoefficient = 1.0f;

private void OnCollisionStay(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;

        Vector3 vel = rb.linearVelocity;
        Vector3 horizontalVel = new Vector3(vel.x, 0f, vel.z);
        if (horizontalVel.sqrMagnitude < 0.0001f) return;

        Vector3 decelForce = -horizontalVel.normalized * rollingResistanceCoefficient * rb.mass;
        rb.AddForce(decelForce, ForceMode.Force);
    }
}

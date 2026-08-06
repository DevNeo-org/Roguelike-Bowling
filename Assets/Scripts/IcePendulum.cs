using UnityEngine;

// Obstacle: 얼음 진자 (ice pendulum).
// A block that swings back and forth across the lane like a pendulum
// (arcing motion, not a straight patrol or a vertical rise) - knocks the
// ball hard if it's mid-swing when the ball passes through.
public class IcePendulum : MonoBehaviour
{
    public float laneCenterX = -3f;
    public float pivotHeight = 2.5f;
    public float armLength = 1.2f;
    public float swingAngleDegrees = 50f;
    public float swingSpeed = 1.5f;
    public float pushForce = 7f;

    private Rigidbody rb;
    private float phase;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.useGravity = false;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void FixedUpdate()
    {
        phase += swingSpeed * Time.fixedDeltaTime;
        float angleRad = Mathf.Sin(phase) * swingAngleDegrees * Mathf.Deg2Rad;

        float x = laneCenterX + Mathf.Sin(angleRad) * armLength;
        float y = pivotHeight - Mathf.Cos(angleRad) * armLength;

        Vector3 pos = rb.position;
        rb.MovePosition(new Vector3(x, y, pos.z));
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.CompareTag("Ball")) return;

        Rigidbody ballRb = collision.rigidbody;
        if (ballRb == null) return;

        Vector3 push = new Vector3(Random.Range(-0.5f, 0.5f), 0.3f, 1f).normalized * pushForce;
        ballRb.AddForce(push, ForceMode.Impulse);

        Debug.Log("[IcePendulum] " + collision.gameObject.name + " got clobbered by the swinging pendulum");
    }
}

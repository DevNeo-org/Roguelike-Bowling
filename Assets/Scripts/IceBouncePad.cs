using UnityEngine;

// Obstacle: 얼음 트램펄린 (bounce pad).
// A springy pad on the lane. When the ball touches it, it gets launched
// forward and upward in one sharp impulse - a "boost/launch" beat, unlike
// any push/crack/patrol/pendulum obstacle used elsewhere.
public class IceBouncePad : MonoBehaviour
{
    public float launchForce = 8f;
    public float upwardBias = 0.6f;
    public float cooldownTime = 1f;

    private bool onCooldown;

    private void OnTriggerEnter(Collider other)
    {
        if (onCooldown) return;
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        Vector3 forward = transform.forward;
        Vector3 launch = (forward + Vector3.up * upwardBias).normalized * launchForce;
        rb.AddForce(launch, ForceMode.Impulse);

        Debug.Log("[IceBouncePad] " + other.name + " got launched off the bounce pad");

        StartCoroutine(Cooldown());
    }

    private System.Collections.IEnumerator Cooldown()
    {
        onCooldown = true;
        yield return new WaitForSeconds(cooldownTime);
        onCooldown = false;
    }
}

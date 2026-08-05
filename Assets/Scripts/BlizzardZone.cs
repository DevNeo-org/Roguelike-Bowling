using UnityEngine;

// Obstacle: 눈보라 구간 (blizzard zone).
// While the ball is inside this trigger volume, a gusting side wind keeps
// pushing it sideways (strength oscillates a bit so it doesn't feel like
// a constant, fully-predictable drift).
public class BlizzardZone : MonoBehaviour
{
    public float windStrength = 3f;
    public float gustSpeed = 2f;

    private void OnTriggerStay(Collider other)
    {
        if (!other.CompareTag("Ball")) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null) return;

        float gust = Mathf.Sin(Time.time * gustSpeed) * windStrength;
        rb.AddForce(new Vector3(gust, 0f, 0f), ForceMode.Force);
    }
}
